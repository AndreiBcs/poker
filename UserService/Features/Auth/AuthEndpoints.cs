using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using UserService.Domain.Interfaces;

namespace UserService.Features.Auth
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");
            

            group.MapPost("/login", async Task<Results<Ok<AuthResponseDto>, UnauthorizedHttpResult, ValidationProblem>>
            (LoginDto dto, IValidator<LoginDto> validator, IAuthService service) =>
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return TypedResults.ValidationProblem(validation.ToDictionary());

                var result = await service.Login(dto);
                return result is not null ? TypedResults.Ok(result) : TypedResults.Unauthorized();
            });


            group.MapPost("/sign-up", async Task<Results<Created<AuthResponseDto>, Conflict<string>, ValidationProblem>>
            (SignUpDto dto, IValidator<SignUpDto> validator, IAuthService service) =>
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return TypedResults.ValidationProblem(validation.ToDictionary());

                if(await service.UsernameConflict(dto.Username))
                    return TypedResults.Conflict("Username already taken.");

                if(await service.EmailConflict(dto.Email))
                    return TypedResults.Conflict("Email already in use.");

                var created = await service.SignUp(dto);
                return TypedResults.Created($"/api/users/{created.Id}", created);
            });
        }
    }
}