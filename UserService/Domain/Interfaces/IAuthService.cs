namespace UserService.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignUp(SignUpDto dto);
        Task<AuthResponseDto?> Login(LoginDto dto);
    }
}