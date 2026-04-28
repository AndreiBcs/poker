using UserService.Domain.Interfaces;

namespace UserService.Features.Auth
{
    public class AuthService(IUserRepository repo, ITokenService tokenService) : IAuthService
    {
        public async Task<AuthResponseDto?> Login(LoginDto dto)
        {
            var user = await repo.GetUserByEmail(dto.Email);
            if(user is null) return null;

            if(!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) 
                return null;
            
            return user.ToAuthResponse(tokenService);
        }

        public async Task<AuthResponseDto> SignUp(SignUpDto dto)
        {
            var user = new Domain.User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                CreatedAt = DateTime.UtcNow
            };

            await repo.CreateUser(user);
            return user.ToAuthResponse(tokenService);
        }

        public async Task<bool> UsernameConflict(string username) =>
            await repo.GetUserByUsername(username) is not null;

        public async Task<bool> EmailConflict(string email) =>
            await repo.GetUserByEmail(email) is not null;
            
    }
}