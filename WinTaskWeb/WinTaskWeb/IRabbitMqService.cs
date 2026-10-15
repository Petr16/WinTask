namespace WinTaskWeb
{
    public interface IRabbitMqService
    {
        Task PublishTaskCreatedAsync(TaskItem task);
        Task PublishTaskCompletedAsync(TaskItem task);
    }
}
