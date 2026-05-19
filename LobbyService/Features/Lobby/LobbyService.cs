using LobbyService.Domain;
using LobbyService.Domain.Interfaces;

namespace LobbyService.Features.Lobby
{
    public class LobbyService(ILobbyRepository repo) : ILobbyService
    {
        public async Task AddPlayerToLobbyAsync(Player player)
        {
            throw new NotImplementedException();
        }

        public async Task<Domain.Lobby> CreateLobbyAsync(Domain.Lobby lobby)
        {
            return await repo.CreateAsync(lobby);
        }

        public async Task<bool> DeleteLobbyAsync(Guid id)
        {
            return await repo.DeleteAsync(id);
        }

        public async Task RemovePlayerFromLobbyAsync(Player player)
        {
            throw new NotImplementedException();
        }
    }
}