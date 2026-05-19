namespace LobbyService.Domain.Interfaces
{
    public interface ILobbyService
    {
        Task<Lobby> CreateLobbyAsync(Lobby lobby);
        Task<bool> DeleteLobbyAsync(Guid id);
        Task AddPlayerToLobbyAsync(Player player);
        Task RemovePlayerFromLobbyAsync(Player player); 
    }
}