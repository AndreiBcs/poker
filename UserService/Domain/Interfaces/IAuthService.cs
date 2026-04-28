using UserService.Features.Auth;

namespace UserService.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignUp(SignUpDto dto);
        Task<AuthResponseDto?> Login(LoginDto dto);
        Task<bool> UsernameConflict(string username);
        Task<bool> EmailConflict(string email);
    }
}