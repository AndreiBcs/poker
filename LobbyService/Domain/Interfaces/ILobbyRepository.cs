namespace LobbyService.Domain.Interfaces
{
    public interface ILobbyRepository
    {
        Task<Lobby?> GetByIdAsync(Guid id);
        Task<List<Lobby>> GetAllOpenAsync();
        Task<Lobby> CreateAsync(Lobby lobby);
        Task UpdateAsync(Lobby lobby);
        Task<bool> DeleteAsync(Guid id);
    }
}