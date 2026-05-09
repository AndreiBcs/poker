namespace Messaging.Abstractions
{
    public interface IMessageConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}