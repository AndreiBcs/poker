using Source.Domain.User;

namespace UserService.Features.User
{
    public record UpdateUserDto(string? Username, string? Email, string? Password);
    public record UserResponseDto(Gui8d Id, string Username, string Email);

    public static class UserMappings
    {
        public static UserResponseDto(this User user) =>
            new(user.Id, user.Username, user.Email);
            
    }
}