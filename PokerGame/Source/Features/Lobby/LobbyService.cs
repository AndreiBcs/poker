using Microsoft.EntityFrameworkCore;
using Source.Infrastructure.Persistence;

namespace Source.Features.Lobby
{
    public class LobbyService(AppDbContext db)
    {

        public async Task<Domain.Entities.Lobby?> GetLobbyByIdAsync(Guid lobbyId)
        {
            var lobby = await db.Lobbies.FindAsync(lobbyId);

            return lobby;
        }


        public async Task<List<Domain.Entities.Lobby>> GetAllLobbiesAsync()
        {
            var lobbies = await db.Lobbies.ToListAsync();

            return lobbies;
        }


        public async Task<bool> JoinLobbyAsync(Guid lobbyId, HttpContext httpContext)
        {
            var userId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var lobby = await db.Lobbies.Include(l => l.Players).FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby is null) return false;

            bool alreadyInLobby = lobby.Players.Any(p => p.Id.ToString() == userId);
            if (alreadyInLobby) return true;

            var user = await db.Users.FindAsync(Guid.Parse(userId));
            if (user is null) return false;

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                lobby.Players.Add(user);

                db.Lobbies.Update(lobby);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }


        public async Task<bool> LeaveLobbyAsync(Guid lobbyId, HttpContext httpContext)
        {
            var userId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var lobby = await db.Lobbies.Include(l => l.Players).FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby is null) return false;
           
            var user = await db.Users.FindAsync(Guid.Parse(userId));
            if (user is null) return false;

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                lobby.Players.Remove(user);

                db.Lobbies.Update(lobby);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }


        public async Task<Domain.Entities.Lobby?> CreateLobbyAsync(LobbyCreateRequest dto)
        {
            if(string.IsNullOrWhiteSpace(dto.Name) 
                || int.IsNegative(dto.MaxPlayers) 
                || int.IsNegative(dto.SmallBlind)
                || int.IsNegative(dto.BigBlind))
            {
                return null;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var lobby = new Domain.Entities.Lobby
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    MaxPlayers = dto.MaxPlayers,
                    SmallBlind = dto.SmallBlind,
                    BigBlind = dto.BigBlind
                };

                await db.Lobbies.AddAsync(lobby);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return lobby;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }


        public async Task<Domain.Entities.Lobby?> UpdateLobbyAsync(Guid lobbyId, LobbyUpdateRequest dto)
        {
            var lobby = await db.Lobbies.FindAsync(lobbyId);

            if (lobby is null) return null;

            if(string.IsNullOrWhiteSpace(dto.Name) 
                || int.IsNegative(dto.MaxPlayers) 
                || int.IsNegative(dto.SmallBlind)
                || int.IsNegative(dto.BigBlind))
            {
                return null;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                lobby.Name = dto.Name;
                lobby.MaxPlayers = dto.MaxPlayers;
                lobby.SmallBlind = dto.SmallBlind;
                lobby.BigBlind = dto.BigBlind;
                

                db.Lobbies.Update(lobby);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return lobby;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }


        public async Task<bool> DeleteLobbyAsync(Guid lobbyId)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var lobby = await db.Lobbies.FindAsync(lobbyId);

                if (lobby is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                db.Lobbies.Remove(lobby);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}