using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Interfaces;
using UserService.Features.Auth;
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
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }

        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddScoped<IValidator<SignUpDto>, SignUpValidator>();
            services.AddScoped<IValidator<UpdateUserDto>, UpdateUserValidator>();
            services.AddScoped<IValidator<LoginDto>, LoginValidator>();
            return services;
        }

        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IValidator<LoginDto>, LoginValidator>();
            services.AddScoped<IValidator<SignUpDto>, SignUpValidator>();

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

        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
               options.AddFixedWindowLimiter("auth", o =>{
                    o.Window                = TimeSpan.FromMinutes(1);
                    o.PermitLimit           = 10;
                    o.QueueLimit            = 0;
                    o.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
                }); 

                options.AddFixedWindowLimiter("api", o =>
                {
                    o.Window                = TimeSpan.FromMinutes(1);
                    o.PermitLimit           = 60;
                    o.QueueLimit            = 10;
                    o.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}