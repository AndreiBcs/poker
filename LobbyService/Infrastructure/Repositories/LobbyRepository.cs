using LobbyService.Domain;
using LobbyService.Domain.Interfaces;
using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Repositories
{
    public class LobbyRepository(AppDbContext db) : ILobbyRepository
    {
        public async Task<Lobby> CreateAsync(Lobby lobby)
        {
            db.Lobbies.Add(lobby);
            await db.SaveChangesAsync();
            return lobby;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var lobby = await db.Lobbies.FindAsync(id);
            if (lobby is not null)
            {
                db.Lobbies.Remove(lobby);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }
        
        public async Task<List<Lobby>> GetAllOpenAsync() =>
            await db.Lobbies.ToListAsync();


        public async  Task<Lobby?> GetByIdAsync(Guid id) =>
            await db.Lobbies.FirstOrDefaultAsync(l => l.Id == id);


        public async Task UpdateAsync(Lobby lobby)
        {
            db.Lobbies.Update(lobby);
            await db.SaveChangesAsync();
        }
    }
}