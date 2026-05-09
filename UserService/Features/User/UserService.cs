using UserService.Domain.Interfaces;

namespace UserService.Features.User
{
    public class UserService(IUserRepository repo) : IUserService
    {

        public async Task<UserResponseDTO?> GetUserAsync(Guid id) =>
            (await repo.GetUserById(id))?.ToUserResponse();

        public async Task<UserResponseDTO> CreateUserAsync(CreateUserDTO dto)
        {
            var user = new Domain.User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                CreatedAt = DateTime.UtcNow
            };

            await repo.CreateUser(user);
            return user.ToUserResponse();
        }

        public async Task<UserResponseDTO?> UpdateUserAsync(Guid id, UpdateUserDTO dto)
        {
            var user = await repo.GetUserById(id);
            if(user is null) return null;

            if(dto.Username is not null) user.Username = dto.Username;

            return (await repo.UpdateUser(user)).ToUserResponse();
        }

        public async Task<bool> DeleteUserAsync(Guid id) => await repo.DeleteUser(id);
        
    }
}