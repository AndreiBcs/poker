using UserService.Features.User;

namespace UserService.Domain.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto?> GetUser(Guid id);
        Task<UserResponseDto?> UpdateUser(Guid id, UpdateUserDto dto);
        Task<bool> DeleteUser(Guid id); 
        Task<bool> UsernameConflict(string? username);
        Task<bool> EmailConflict(string? username);
    }
}