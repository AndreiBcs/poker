namespace Messaging.Abstractions
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class;
    }
}