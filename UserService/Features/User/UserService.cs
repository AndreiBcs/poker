using UserService.Domain.Interfaces;

namespace UserService.Features.User
{
    public class UserService(IUserRepository repo) : IUserService
    {

        public async Task<UserResponseDto?> GetUser(Guid id) =>
            (await repo.GetUserById(id))?.ToUserResponse();

        public async Task<UserResponseDto?> UpdateUser(Guid id, UpdateUserDto dto)
        {
            var user = await repo.GetUserById(id);
            if(user is null) return null;

            if(dto.Username is not null) user.Username = dto.Username;
            if(dto.Email is not null) user.Email = dto.Email;
            if(dto.Password is not null) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            return (await repo.UpdateUser(user)).ToUserResponse();
        }

        public async Task<bool> DeleteUser(Guid id) => await repo.DeleteUser(id);

        public async Task<bool> UsernameConflict(string? username)
        {
            return username is not null && await repo.GetUserByUsername(username) is not null;
        }

        public async Task<bool> EmailConflict(string? email)
        {
            return email is not null && await repo.GetUserByEmail(email) is not null;
        }
        
    }
}