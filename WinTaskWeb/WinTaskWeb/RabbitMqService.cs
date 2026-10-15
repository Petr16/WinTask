using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

//https://www.rabbitmq.com/tutorials/tutorial-one-dotnet

namespace WinTaskWeb
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "tasks_queue";

        public RabbitMqService(IConfiguration config)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).GetAwaiter().GetResult();
        }

        public async Task PublishTaskCreatedAsync(TaskItem task)
        {
            await SendEventAsync("task.created", task);
        }

        public async Task PublishTaskCompletedAsync(TaskItem task)
        {
            await SendEventAsync("task.completed", task);
        }

        private async Task SendEventAsync(string eventType, TaskItem task)
        {
            var payload = new
            {
                Event = eventType,
                TaskId = task.Id,
                Title = task.Title,
                Priority = task.Priority.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent //сохраняем на диск брокера
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }

        /// <summary>
        /// Автоматом закрываем канал и соединение при shutdown
        /// </summary>
        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();

            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
