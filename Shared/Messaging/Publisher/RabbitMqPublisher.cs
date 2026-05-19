using System.Text;
using System.Text.Json;
using Messaging.Abstractions;
using Messaging.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Messaging.Publisher
{

    public sealed class RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPublisher> logger) : IMessagePublisher, IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _initialized;


        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };


        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            var factory = BuildFactory();
            _connection  = await factory.CreateConnectionAsync();
            _channel     = await _connection.CreateChannelAsync();
            _initialized = true;
        }


        public async Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
        {
            await EnsureInitializedAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

            var props = new BasicProperties
            {
                Persistent  = true,
                ContentType = "application/json",
                Timestamp   = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(
                exchange:        exchange,
                routingKey:      routingKey,
                mandatory:       false,
                basicProperties: props,
                body:            body);

            logger.LogInformation("Published {RoutingKey} to {Exchange}", routingKey, exchange);
        }



        private ConnectionFactory BuildFactory() => new()
        {
            HostName = options.Value.Host,
            Port     = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password
        };


        public async ValueTask DisposeAsync()
        {
            if (_channel    is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}