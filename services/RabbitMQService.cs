using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Comms.Data;
using Comms.Models;
using Microsoft.EntityFrameworkCore;

namespace Comms.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<RabbitMQService> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = Environment.ExpandEnvironmentVariables(_configuration["RabbitMQ:Host"] ?? "localhost"),
                Port = int.Parse(Environment.ExpandEnvironmentVariables(_configuration["RabbitMQ:Port"] ?? "5672")),
                UserName = Environment.ExpandEnvironmentVariables(_configuration["RabbitMQ:Username"] ?? "guest"),
                Password = Environment.ExpandEnvironmentVariables(_configuration["RabbitMQ:Password"] ?? "guest"),
                VirtualHost = Environment.ExpandEnvironmentVariables(_configuration["RabbitMQ:VirtualHost"] ?? "/")
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            SetupExchangesAndQueues();
        }

        private void SetupExchangesAndQueues()
        {
            // Declare exchanges
            var exchanges = _configuration.GetSection("RabbitMQ:Exchanges").Get<Dictionary<string, string>>() ?? new();
            foreach (var exchange in exchanges.Values)
            {
                _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
            }

            // Declare queues
            var queues = _configuration.GetSection("RabbitMQ:Queues").Get<Dictionary<string, string>>() ?? new();
            foreach (var queue in queues.Values)
            {
                _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            }

            // Bind queues to exchanges
            _channel.QueueBind(queues["UserEvents"], exchanges["UserExchange"], "user.*");
            _channel.QueueBind(queues["ChatEvents"], exchanges["ChatExchange"], "chat.*");
            _channel.QueueBind(queues["NotificationEvents"], exchanges["NotificationExchange"], "notification.*");
            _channel.QueueBind(queues["FileEvents"], exchanges["FileExchange"], "file.*");
        }

        public async Task PublishMessageAsync<T>(string exchange, string routingKey, T message)
        {
            try
            {
                var messageBody = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(messageBody);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(exchange, routingKey, properties, body);
                
                _logger.LogInformation($"Published message to {exchange} with routing key {routingKey}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing message to {exchange} with routing key {routingKey}");
                throw;
            }
        }

        public async Task PublishChatEventAsync(string eventType, object eventData)
        {
            var chatExchange = _configuration["RabbitMQ:Exchanges:ChatExchange"] ?? "chat.exchange";
            var routingKey = $"chat.{eventType}";
            
            var message = new
            {
                eventType = eventType,
                data = eventData,
                timestamp = DateTime.UtcNow,
                source = "communications-service"
            };

            await PublishMessageAsync(chatExchange, routingKey, message);
        }

        public async Task PublishUserEventAsync(string eventType, object eventData)
        {
            var userExchange = _configuration["RabbitMQ:Exchanges:UserExchange"] ?? "user.exchange";
            var routingKey = $"user.{eventType}";
            
            var message = new
            {
                eventType = eventType,
                data = eventData,
                timestamp = DateTime.UtcNow,
                source = "communications-service"
            };

            await PublishMessageAsync(userExchange, routingKey, message);
        }

        public async Task PublishNotificationEventAsync(string eventType, object eventData)
        {
            var notificationExchange = _configuration["RabbitMQ:Exchanges:NotificationExchange"] ?? "notification.exchange";
            var routingKey = $"notification.{eventType}";
            
            var message = new
            {
                eventType = eventType,
                data = eventData,
                timestamp = DateTime.UtcNow,
                source = "communications-service"
            };

            await PublishMessageAsync(notificationExchange, routingKey, message);
        }

        public void StartListening()
        {
            // Listen for user events
            ListenToQueue("user.events", HandleUserEvent);
            
            // Listen for file events
            ListenToQueue("file.events", HandleFileEvent);
            
            // Listen for notification events
            ListenToQueue("notification.events", HandleNotificationEvent);

            _logger.LogInformation("Started listening to RabbitMQ queues");
        }

        private void ListenToQueue(string queueName, Func<string, Task> messageHandler)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    await messageHandler(message);
                    
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing message from queue {queueName}");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        private async Task HandleUserEvent(string message)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<dynamic>(message);
                string eventType = eventData?.eventType ?? "unknown";

                _logger.LogInformation($"Received user event: {eventType}");

                // Handle different user events
                switch (eventType)
                {
                    case "user.created":
                        await HandleUserCreated(eventData);
                        break;
                    case "user.updated":
                        await HandleUserUpdated(eventData);
                        break;
                    case "user.deleted":
                        await HandleUserDeleted(eventData);
                        break;
                    default:
                        _logger.LogWarning($"Unknown user event type: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user event");
            }
        }

        private async Task HandleFileEvent(string message)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<dynamic>(message);
                string eventType = eventData?.eventType ?? "unknown";

                _logger.LogInformation($"Received file event: {eventType}");

                // Handle file events (update message media URLs, etc.)
                switch (eventType)
                {
                    case "file.uploaded":
                        await HandleFileUploaded(eventData);
                        break;
                    case "file.deleted":
                        await HandleFileDeleted(eventData);
                        break;
                    default:
                        _logger.LogWarning($"Unknown file event type: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling file event");
            }
        }

        private async Task HandleNotificationEvent(string message)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<dynamic>(message);
                string eventType = eventData?.eventType ?? "unknown";

                _logger.LogInformation($"Received notification event: {eventType}");

                // Handle notification events
                switch (eventType)
                {
                    case "notification.push_subscription":
                        await HandlePushSubscription(eventData);
                        break;
                    default:
                        _logger.LogWarning($"Unknown notification event type: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling notification event");
            }
        }

        private async Task HandleUserCreated(dynamic eventData)
        {
            // Handle user creation (no direct action needed in communications service)
            _logger.LogInformation($"User created: {eventData?.data?.userId}");
            await Task.CompletedTask;
        }

        private async Task HandleUserUpdated(dynamic eventData)
        {
            // Handle user updates (profile changes, etc.)
            _logger.LogInformation($"User updated: {eventData?.data?.userId}");
            await Task.CompletedTask;
        }

        private async Task HandleUserDeleted(dynamic eventData)
        {
            // Handle user deletion - cleanup user data
            string userIdStr = (string?)(eventData?.data?.userId?.ToString()) ?? "null";
        if (!Guid.TryParse(userIdStr, out Guid userId))
             {
            _logger.LogWarning("Invalid userId in user.deleted event: {UserId}", userIdStr);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();

        // ✅ Now EF Core sees a real Guid, not dynamic
        var participations = await context.ChatParticipants
            .Where(cp => cp.UserId == userId)
            .ToListAsync();

        context.ChatParticipants.RemoveRange(participations);
            
            // // Remove user's push subscriptions
            var subscriptions = await context.PushSubscriptions
                .Where(ps => ps.UserId == userId)
                .ToListAsync();
            
            context.PushSubscriptions.RemoveRange(subscriptions);
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Cleaned up data for deleted user: {userId}");
        }

        private async Task HandleFileUploaded(dynamic eventData)
        {
            // Handle file upload completion - update message with file URL
            var fileUrl = eventData?.data?.fileUrl?.ToString();
            string messageId = (string?)(eventData?.data?.messageId?.ToString()) ?? "null";
            
            if (!string.IsNullOrEmpty(fileUrl) && !string.IsNullOrEmpty(messageId))
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();

                var message = await context.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == Guid.Parse(messageId));

                if (message != null)
                {
                    message.FileUrl = fileUrl;
                    message.FileSize = eventData?.data?.fileSize;
                    message.FileType = eventData?.data?.fileType;
                    await context.SaveChangesAsync();

                    _logger.LogInformation($"Updated message {messageId} with file URL: {fileUrl}");
                }
            }
        }

        private async Task HandleFileDeleted(dynamic eventData)
        {
            // Handle file deletion - remove file references from messages
            string fileUrl = (string?)(eventData?.data?.fileUrl?.ToString()) ?? "null";

            
            if (!string.IsNullOrEmpty(fileUrl))
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();

                var messages = await context.Messages
                    .Where(m => m.FileUrl == fileUrl)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.FileUrl = null;
                    message.FileSize = null;
                    message.FileType = null;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation($"Removed file references for URL: {fileUrl}");
            }
        }

        private async Task HandlePushSubscription(dynamic eventData)
        {
            // Handle push subscription from other services
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
            
            var subscription = new PushSubscription
            {
                UserId = Guid.Parse(eventData?.data?.userId?.ToString() ?? ""),
                Endpoint = eventData?.data?.endpoint?.ToString() ?? "",
                P256dh = eventData?.data?.p256dh?.ToString(),
                Auth = eventData?.data?.auth?.ToString(),
                Platform = eventData?.data?.platform?.ToString() ?? "WEB"
            };
            
            context.PushSubscriptions.Add(subscription);
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Added push subscription for user: {subscription.UserId}");
        }

        public void StopListening()
        {
            _logger.LogInformation("Stopped listening to RabbitMQ queues");
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
} 