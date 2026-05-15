namespace Source.Features.User
{
    public record UserCreateRequest(string Username, string Email, string Password);
    public record UserLoginRequest(string Email, string Password);
    public record UserEmailUpdateRequest(string NewEmail);
    public record UserPasswordUpdateRequest(string NewPassword);
    public record UserUsernameUpdateRequest(string NewUsername);
    
}