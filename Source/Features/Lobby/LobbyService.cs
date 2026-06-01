using Microsoft.EntityFrameworkCore;
using Source.Infrastructure.Persistence;

namespace Source.Features.Lobby
{
    public enum LobbyJoinResult
    {
        Success,
        LobbyNotFound,
        LobbyFull,
        UserNotFound,
        Failed
    }

    public enum LobbyLeaveResult
    {
        Success,
        LobbyNotFound,
        UserNotFound,
        NotInLobby,
        Failed
    }

    public class LobbyService(AppDbContext db)
    {

        public async Task<Domain.Entities.Lobby?> GetLobbyByIdAsync(Guid lobbyId, bool includePlayers = false)
        {
            var query = db.Lobbies.AsNoTracking();
            if (includePlayers)
            {
                query = query.Include(lobby => lobby.Players);
            }

            return await query.FirstOrDefaultAsync(lobby => lobby.Id == lobbyId);
        }


        public async Task<List<Domain.Entities.Lobby>> GetAllLobbiesAsync(bool includePlayers = false)
        {
            var query = db.Lobbies.AsNoTracking();
            if (includePlayers)
            {
                query = query.Include(lobby => lobby.Players);
            }

            var lobbies = await query.ToListAsync();

            return lobbies;
        }


        public async Task<LobbyJoinResult> JoinLobbyAsync(Guid lobbyId, Guid userId)
        {
            var lobby = await db.Lobbies.Include(l => l.Players).FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby is null) return LobbyJoinResult.LobbyNotFound;

            var user = await db.Users.FindAsync(userId);
            if (user is null) return LobbyJoinResult.UserNotFound;

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                if (user.LobbyId == lobby.Id)
                {
                    await transaction.CommitAsync();
                    return LobbyJoinResult.Success;
                }

                if (lobby.Players.Count >= lobby.MaxPlayers)
                {
                    await transaction.RollbackAsync();
                    return LobbyJoinResult.LobbyFull;
                }

                user.LobbyId = lobby.Id;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return LobbyJoinResult.Success;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return LobbyJoinResult.Failed;
            }
        }


        public async Task<LobbyLeaveResult> LeaveLobbyAsync(Guid lobbyId, Guid userId)
        {
            var lobbyExists = await db.Lobbies.AnyAsync(lobby => lobby.Id == lobbyId);
            if (!lobbyExists) return LobbyLeaveResult.LobbyNotFound;

            var user = await db.Users.FindAsync(userId);
            if (user is null) return LobbyLeaveResult.UserNotFound;

            if (user.LobbyId != lobbyId)
            {
                return LobbyLeaveResult.NotInLobby;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                user.LobbyId = null;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return LobbyLeaveResult.Success;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return LobbyLeaveResult.Failed;
            }
        }


        public async Task<Domain.Entities.Lobby?> CreateLobbyAsync(LobbyCreateRequest dto, Guid ownerUserId)
        {
            if(string.IsNullOrWhiteSpace(dto.Name) 
                || dto.MaxPlayers < 2 
                || dto.SmallBlind < 1
                || dto.BigBlind < 1
                || ownerUserId == Guid.Empty)
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
                    OwnerUserId = ownerUserId,
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
                || dto.MaxPlayers < 2 
                || dto.SmallBlind < 1
                || dto.BigBlind < 1)
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

                var lobbyUsers = await db.Users.Where(user => user.LobbyId == lobbyId).ToListAsync();
                foreach (var user in lobbyUsers)
                {
                    user.LobbyId = null;
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
