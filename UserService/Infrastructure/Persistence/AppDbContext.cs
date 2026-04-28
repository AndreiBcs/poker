using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
    }
}