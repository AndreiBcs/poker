using LobbyService.Domain;
using LobbyService.Domain.Interfaces;
using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Repositories
{
    public class PlayerRepository(AppDbContext db) : IPlayerRepository
    {
        public async Task AddPlayerAsync(Player player)
        {
            db.Players.Add(player);
            await db.SaveChangesAsync();
        }

        public async  Task<Player?> GetPlayerAsync(Guid id) =>
            await db.Players.FindAsync(id);


        public async Task RemovePlayerAsync(Guid id)
        {
            var player = await GetPlayerAsync(id);
            if (player is not null)
            {
                db.Players.Remove(player);
                await db.SaveChangesAsync();
            }
        }
    }
}