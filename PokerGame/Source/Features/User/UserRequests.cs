namespace Source.Features.User
{
    public record UserCreateRequest(string Username, string Email, string Password);
    public record UserLoginRequest(string Email, string Password);
    public record UserUpdateRequest(string Username, string Email, string Password);
    
}