using UserService.Features.User;

namespace UserService.Domain.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDTO?> GetUserAsync(Guid id);
        Task<UserResponseDTO> CreateUserAsync(CreateUserDTO dto);
        Task<UserResponseDTO?> UpdateUserAsync(Guid id, UpdateUserDTO dto);
        Task<bool> DeleteUserAsync(Guid id);
    }
}