namespace UserService.Features.Auth
{
    public record SignUpDto(string Username, string Email, string Password);
    public record LoginDto(string Email, string Password);
    public record AuthResponseDto(Guid Id, string Username, string Email, DateTime CreatedAt, string Token);

    public static class AuthMappings
    {
        public static AuthResponseDto ToAuthResponse(this Domain.User user, ITokenService tokenService) =>
            new(user.Id, user.Username, user.Email, user.CreatedAt, tokenService.GenerateToken(user));

    }
}
