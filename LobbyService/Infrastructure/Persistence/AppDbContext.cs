using LobbyService.Domain;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Lobby> Lobbies => Set<Lobby>();
        public DbSet<Player> Players => Set<Player>();
    }
}