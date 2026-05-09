using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Interfaces;
using UserService.Features.User;
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

        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
        {

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = config["Jwt:Issuer"],
                        ValidAudience            = config["Jwt:Audience"],
                        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!))
                    };
                });

            services.AddAuthorization();
            return services;
        }
    }
}