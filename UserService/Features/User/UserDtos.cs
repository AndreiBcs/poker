namespace UserService.Features.User
{
    public record UpdateUserDto(string? Username, string? Email, string? Password);
    public record UserResponseDto(Guid Id, string Username, string Email);

    public static class UserMappings
    {
        public static UserResponseDto ToUserResponse(this Domain.User user) =>
            new(user.Id, user.Username, user.Email);
            
    }
}