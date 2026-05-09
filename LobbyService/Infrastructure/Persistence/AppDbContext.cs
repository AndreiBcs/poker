using LobbyService.Domain;
using Microsoft.EntityFrameworkCore;
namespace LobbyService.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Lobby> Lobbies => Set<Lobby>();
        public DbSet<Player> Players => Set<Player>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lobby>(e =>
            {
                e.HasKey(l => l.Id);
                e.Property(l => l.Name).IsRequired().HasMaxLength(20);
                e.Property(l => l.Status).HasConversion<string>();
            });

            modelBuilder.Entity<Player>(e =>
            {
                //e.HasKey(p => new { p.Id, p.UserId });
                e.Property(p => p.Username).IsRequired().HasMaxLength(50);
    
                // e.HasOne(p => p.Lobby)
                //     .WithMany(l => l.Players)
                //     .HasForeignKey(p => p.LobbyId)
                //     .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}