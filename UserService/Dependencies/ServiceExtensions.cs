using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Interfaces;
using UserService.Features.User;
using Messaging.Extensions;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;

namespace UserService.Dependencies
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(config.GetConnectionString("DefaultConnection")));
            
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, Features.User.UserService>();
            return services;
        }

        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddScoped<IValidator<UpdateUserDTO>, UpdateUserValidator>();
            return services;
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration config)
        {
            services.AddRabbitMq(config);
            services.AddHostedService<UserRegisteredConsumer>();
            return services;
        }
    }
}