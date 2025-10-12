namespace Comms.Services
{
    public interface IRabbitMQService
    {
        Task PublishMessageAsync<T>(string exchange, string routingKey, T message);
        Task PublishChatEventAsync(string eventType, object eventData);
        Task PublishUserEventAsync(string eventType, object eventData);
        Task PublishNotificationEventAsync(string eventType, object eventData);
        void StartListening();
        void StopListening();
    }
} 