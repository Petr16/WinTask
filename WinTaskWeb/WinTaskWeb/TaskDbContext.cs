using Microsoft.EntityFrameworkCore;

namespace WinTaskWeb
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
    }
}