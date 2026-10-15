using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;//чтобы сваггер видел

namespace WinTaskWeb
{
    public static class TaskEndpoints
    {
        public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/tasks");

            group.MapGet("/", async ([FromServices] TaskDbContext db) =>
                await db.Tasks.AsNoTracking().ToListAsync());

            group.MapPost("/", async (
                [FromBody] CreateTaskDto dto,
                [FromServices] TaskDbContext db,
                [FromServices] IRabbitMqService mq) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return Results.BadRequest("Title не должен быть пустым");

                if (dto.Title.Length > 200)
                    return Results.BadRequest("Title больше 200 символов");

                var task = new TaskItem
                {
                    Title = dto.Title.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsCompleted = false,
                    CompletedAt = null,
                    Priority = dto.Priority
                };

                db.Tasks.Add(task);
                await db.SaveChangesAsync();

                await mq.PublishTaskCreatedAsync(task);

                return Results.Created($"/tasks/{task.Id}", task);
            });

            group.MapPut("/{id}/complete", async (
                [FromRoute] Guid id,
                [FromBody] CompleteTaskDto dto,
                [FromServices] TaskDbContext db,
                [FromServices] IRabbitMqService mq) =>
            {
                var task = await db.Tasks.FindAsync(id);
                if (task == null) return Results.NotFound();

                if (task.IsCompleted)
                    return Results.BadRequest("Задача уже завершена");

                if (task.Version != dto.Version)
                    return Results.Conflict("Задача изменена другим пользователем");

                task.IsCompleted = true;
                task.CompletedAt = DateTimeOffset.UtcNow;
                task.Version = Guid.NewGuid();

                try
                {
                    await db.SaveChangesAsync();
                    await mq.PublishTaskCompletedAsync(task);
                    return Results.Ok(task);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Results.Conflict("Конфликт");//Status409Conflict = 409
                }
            });

            group.MapDelete("/{id}", async (
                [FromRoute] Guid id,
                [FromServices] TaskDbContext db) =>
            {
                var task = await db.Tasks.FindAsync(id);
                if (task == null)
                    return Results.NotFound(); //это если нечего удалять

                db.Tasks.Remove(task);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }

    public record CreateTaskDto(string Title, TaskPriority Priority = TaskPriority.Medium);
    public record CompleteTaskDto(Guid Version);
}
