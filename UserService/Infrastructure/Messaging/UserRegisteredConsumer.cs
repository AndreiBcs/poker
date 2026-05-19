using System.Text.Json;
using Microsoft.Extensions.Options;
using Poker.Messaging.Configuration;
using Messaging.Consumer;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Messaging
{
    public class UserRegisteredConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<UserRegisteredConsumer> logger)
        : RabbitMqConsumerBase(options, logger)
    {
        protected override string Exchange   => "auth";
        protected override string Queue      => "user-service.user-registered";
        protected override string RoutingKey => "auth.user_registered";

        protected override async Task HandleAsync(string body, CancellationToken ct)
        {
            var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(body);
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IUserService>();
            await service.CreateUserAsync(@event!.UserId, @event.Username);
        }
    }
}