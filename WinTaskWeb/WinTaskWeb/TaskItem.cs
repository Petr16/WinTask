using System.ComponentModel.DataAnnotations;

namespace WinTaskWeb
{
    public class TaskItem
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } // не null, не пустой, максимум 200

        public bool IsCompleted { get; set; }

        /// <summary>
        /// Дата оздания
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Дата завершения
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; } // когда завершили

        public TaskPriority Priority { get; set; } = TaskPriority.Medium; // Low, Medium, High

        //https://metanit.com/sharp/efcore/2.11.php race condition
        /// <summary>
        /// Версия для проверки версионирования задачи, при комплите будем проверять актуальность, чтобы не было проблем
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();
    }
}