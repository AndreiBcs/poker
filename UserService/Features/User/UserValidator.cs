using FluentValidation;

namespace UserService.Features.User
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserValidator()
        {
            When(x => x.Username is not null, () =>
                RuleFor(x => x.Username!).MinimumLength(3));

            When(x => x.Email is not null, () =>
                RuleFor(x => x.Email!).EmailAddress());

            When(x => x.Password is not null, () =>
                RuleFor(x => x.Password!).MinimumLength(8));

            RuleFor(x => x)
                .Must(x => x.Username is not null || x.Email is not null || x.Password is not null)
                .WithName("Body")
                .WithMessage("At least one field must be provided.");
            
        }
    }
}