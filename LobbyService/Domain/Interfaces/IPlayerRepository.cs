namespace LobbyService.Domain.Interfaces
{
    public interface IPlayerRepository
    {
        Task<Player?> GetPlayerAsync(Guid id);
        Task AddPlayerAsync(Player player);
        Task RemovePlayerAsync(Guid lobbyId, Guid userId);
    }
}