using System.Text;
using Messaging.Abstractions;
using Messaging.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging.Consumer
{
    public abstract class RabbitMqConsumerBase(
        IOptions<RabbitMqOptions> options,
        ILogger logger) : IMessageConsumer, IHostedService, IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;


        // each service defines its own topology
        protected abstract string Exchange   { get; }
        protected abstract string Queue      { get; }
        protected abstract string RoutingKey { get; }

        // each service defines its own message handling
        protected abstract Task HandleAsync(string body, CancellationToken ct);


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

            await _channel.ExchangeDeclareAsync(
                exchange:          Exchange,
                type:              ExchangeType.Topic,
                durable:           true,
                cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue:             Queue,
                durable:           true,
                exclusive:         false,
                autoDelete:        false,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue:             Queue,
                exchange:          Exchange,
                routingKey:        RoutingKey,
                cancellationToken: cancellationToken);

            // one message at a time per consumer
            await _channel.BasicQosAsync(
                prefetchSize:  0,
                prefetchCount: 1,
                global:        false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue:             Queue,
                autoAck:           false,
                consumer:          consumer,
                cancellationToken: cancellationToken);

            logger.LogInformation("[{Consumer}] Listening on {Queue}", GetType().Name, Queue);
        }


        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e)
        {
            var body = Encoding.UTF8.GetString(e.Body.Span);

            try
            {
                await HandleAsync(body, CancellationToken.None);
                await _channel!.BasicAckAsync(e.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{Consumer}] Failed to handle message, requeueing", GetType().Name);
                await _channel!.BasicNackAsync(e.DeliveryTag, multiple: false, requeue: true);
            }
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel    is not null) await _channel.CloseAsync(cancellationToken);
            if (_connection is not null) await _connection.CloseAsync(cancellationToken);

            logger.LogInformation("[{Consumer}] Stopped", GetType().Name);
        }


        public async ValueTask DisposeAsync()
        {
            if (_channel    is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}