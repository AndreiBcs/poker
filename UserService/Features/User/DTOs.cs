using FluentValidation;

namespace UserService.Features.User
{
    public record UpdateUserDTO(string? Username);
    public record UserResponseDTO(Guid Id, string Username);


    public static class UserMappings
    {
        public static UserResponseDTO ToUserResponse(this Domain.User user) =>
            new(user.Id, user.Username);
            
    }

    public class UpdateUserValidator : AbstractValidator<UpdateUserDTO>
    {
        public UpdateUserValidator()
        {
            When(x => x.Username is not null, () =>
                RuleFor(x => x.Username!).MinimumLength(3).MaximumLength(14));
            
        }
    }
}
