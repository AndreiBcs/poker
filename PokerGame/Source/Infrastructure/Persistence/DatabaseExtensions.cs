using Microsoft.EntityFrameworkCore;

namespace Source.Infrastructure.Persistence
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
                
            return services;
        }
    }
}