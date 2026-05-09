using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Messaging;

public class RabbitMqPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisher> logger) : IMessagePublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _initialized;

    // lazy init — no need to connect at startup if we never publish
    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        var factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port     = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password
        };

        _connection  = await factory.CreateConnectionAsync();
        _channel     = await _connection.CreateChannelAsync();
        _initialized = true;
    }


    public async Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
    {
        await EnsureInitializedAsync();

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

        var props = new BasicProperties
        {
            Persistent    = true,          // survive broker restart
            ContentType   = "application/json",
            Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel!.BasicPublishAsync(
            exchange:   exchange,
            routingKey: routingKey,
            mandatory:  false,
            basicProperties: props,
            body:       body);

        logger.LogInformation("Published {RoutingKey} to exchange {Exchange}", routingKey, exchange);
    }


    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)    await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}