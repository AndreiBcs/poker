namespace UserService.Domain.Interfaces
{
    public interface IMessageConsumer
    {
        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
    }
}