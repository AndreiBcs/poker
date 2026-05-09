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

        public async Task DeleteAsync(Guid id)
        {
            var lobby = await db.Lobbies.FindAsync(id);
            if (lobby is not null)
            {
                db.Lobbies.Remove(lobby);
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id) =>
            await db.Lobbies.AnyAsync(l => l.Id == id);


        public async Task<IEnumerable<Lobby>> GetAllOpenAsync() =>
            await db.Lobbies
                .Include(l => l.Players)
                .Where(l => l.Status == LobbyStatus.Waiting)
                .AsAsyncEnumerable()
                .ToListAsync()
                .ContinueWith(t => t.Result.AsEnumerable());


        public async  Task<Lobby?> GetByIdAsync(Guid id) =>
            await db.Lobbies.Include(l => l.Players).FirstOrDefaultAsync(l => l.Id == id);


        public async Task UpdateAsync(Lobby lobby)
        {
            db.Lobbies.Update(lobby);
            await db.SaveChangesAsync();
        }
    }


    file static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source) list.Add(item);
            return list;
        }
    }
}