using Microsoft.EntityFrameworkCore;
using Source.Domain.Entities;

namespace Source.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Lobby> Lobbies => Set<Lobby>();
        public DbSet<Game> Games => Set<Game>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lobby>(entity =>
            {
                entity.ToTable("Lobby");
                entity.Property(lobby => lobby.SmallBlind).HasColumnName("SmallBling");
                entity.HasMany(lobby => lobby.Players)
                    .WithOne(user => user.Lobby)
                    .HasForeignKey(user => user.LobbyId);
            });
        }
    }
}
