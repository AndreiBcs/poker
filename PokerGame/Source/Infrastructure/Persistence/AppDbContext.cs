using Microsoft.EntityFrameworkCore;
using Source.Domain.Entities;

namespace Source.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Lobby> Lobby => Set<Lobby>();
        public DbSet<Game> Games => Set<Game>();
    }
}