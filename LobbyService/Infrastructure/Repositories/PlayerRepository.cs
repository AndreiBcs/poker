using LobbyService.Domain;
using LobbyService.Domain.Interfaces;
using LobbyService.Infrastructure.Persistence;

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
            await db.Players.FirstOrDefaultAsync(p => p.Id == id);


        public async Task RemovePlayerAsync(Guid lobbyId, Guid userId)
        {
            var player = await GetPlayerAsync(userId);
            if (player is not null)
            {
                db.Players.Remove(player);
                await db.SaveChangesAsync();
            }
        }
    }
}