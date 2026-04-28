using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using UserService.Domain.Interfaces;

namespace UserService.Features.User
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/users").RequireAuthorization().RequireRateLimiting("api");


            group.MapGet("/{id:guid}", async Task<Results<Ok<UserResponseDto>, NotFound>> 
                (Guid id, IUserService service) =>
            {
                var user = await service.GetUser(id);
                return user is not null
                    ? TypedResults.Ok(user)
                    : TypedResults.NotFound();
            });


            group.MapPut("/update-user/{id:guid}", async Task<Results<Ok<UserResponseDto>, NotFound, Conflict<string>, ValidationProblem>> 
                (Guid id, UpdateUserDto dto, IValidator<UpdateUserDto> validator, IUserService service) =>
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return TypedResults.ValidationProblem(validation.ToDictionary());

                if (await service.UsernameConflict(dto.Username))
                    return TypedResults.Conflict("Username already taken.");

                if (await service.EmailConflict(dto.Email))
                    return TypedResults.Conflict("Email already in use.");

                var updated = await service.UpdateUser(id, dto);
                return updated is not null
                    ? TypedResults.Ok(updated)
                    : TypedResults.NotFound();
            });


            group.MapDelete("/delete-user/{id:guid}", async Task<Results<NoContent, NotFound>> 
                (Guid id, IUserService service) =>
                await service.DeleteUser(id)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound());
                    
        }
    }
}