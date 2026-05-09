using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Messaging
{
    public class MessageConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<MessageConsumer> logger) : IMessageConsumer,
                                            IHostedService,
                                            IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private const string Exchange = "auth";
        private const string Queue = "user-service.user-registered";
        private const string RoutingKey = "auth.user_registered";


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = options.Value.Host,
                Port     = options.Value.Port,
                UserName = options.Value.Username,
                Password = options.Value.Password
            };
 
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel    = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    
            // durable exchange — survives broker restart
            await _channel.ExchangeDeclareAsync(
                exchange:    Exchange,
                type:        ExchangeType.Topic,
                durable:     true,
                cancellationToken: cancellationToken);
    
            // durable queue — messages survive broker restart
            await _channel.QueueDeclareAsync(
                queue:       Queue,
                durable:     true,
                exclusive:   false,
                autoDelete:  false,
                cancellationToken: cancellationToken);
    
            await _channel.QueueBindAsync(
                queue:       Queue,
                exchange:    Exchange,
                routingKey:  RoutingKey,
                cancellationToken: cancellationToken);
    
            // one message at a time — don't overwhelm the service
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
    
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += HandleMessageAsync;
    
            await _channel.BasicConsumeAsync(
                queue:       Queue,
                autoAck:     false, // manual ack — only remove from queue after successful processing
                consumer:    consumer,
                cancellationToken: cancellationToken);

            logger.LogInformation("RabbitMQ consumer started, listening on {Queue}", Queue);
        }

 
        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs e)
        {
            var body = Encoding.UTF8.GetString(e.Body.Span);
            logger.LogInformation("Received message on {RoutingKey}", e.RoutingKey);
    
            try
            {
                var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
                if (@event is null)
                {
                    logger.LogWarning("Received null or malformed event, discarding");
                    await _channel!.BasicNackAsync(e.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
    
                // scoped because IUserService is scoped (EF DbContext underneath)
                using var scope = scopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    
                await userService.CreateUserAsync(@event.UserId, @event.Username);
    
                await _channel!.BasicAckAsync(e.DeliveryTag, multiple: false);
                logger.LogInformation("User {UserId} created from broker event", @event.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process UserRegisteredEvent, requeueing");
                // requeue: true — retry on next available consumer
                await _channel!.BasicNackAsync(e.DeliveryTag, multiple: false, requeue: true);
            }
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
 
            if (_connection is not null)
                await _connection.CloseAsync(cancellationToken);
    
            logger.LogInformation("RabbitMQ consumer stopped");
        }
        

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)    await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}