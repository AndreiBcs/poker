using UserService.Features.User;
using UserService.Infrastructure.Messaging;

namespace UserService.Domain.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDTO?> GetUserAsync(Guid id);
        Task<UserResponseDTO> CreateUserAsync(Guid id, string username);
        Task<UserResponseDTO?> UpdateUserAsync(Guid id, UpdateUserDTO dto);
        Task<bool> DeleteUserAsync(Guid id);
    }
}