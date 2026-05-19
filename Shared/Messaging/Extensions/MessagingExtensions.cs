using Messaging.Abstractions;
using Messaging.Config;
using Messaging.Publisher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Extensions
{
    public static class MessagingExtensions
    {
        /// <summary>
        /// Registers RabbitMQ options and the publisher.
        /// Call AddHostedService{YourConsumer}() separately per service.
        /// </summary>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<RabbitMqOptions>(config.GetSection(RabbitMqOptions.Section));
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            return services;
        }
    }
}