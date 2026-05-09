namespace LobbyService.Domain.Interfaces
{
    public interface ILobbyRepository
    {
        Task<bool> ExistsAsync(Guid id);
        Task<Lobby?> GetByIdAsync(Guid id);
        Task<IEnumerable<Lobby>> GetAllOpenAsync();
        Task<Lobby> CreateAsync(Lobby lobby);
        Task UpdateAsync(Lobby lobby);
        Task DeleteAsync(Guid id);
    }
}