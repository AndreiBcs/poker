using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using UserService.Domain.Interfaces;

namespace UserService.Features.User
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/users");


            group.MapGet("/{id:guid}", async Task<Results<Ok<UserResponseDTO>, NotFound>> 
                (Guid id, IUserService service) =>
            {
                var user = await service.GetUserAsync(id);
                return user is not null
                    ? TypedResults.Ok(user)
                    : TypedResults.NotFound();
            });


            group.MapPut("/{id:guid}", async Task<Results<Ok<UserResponseDTO>, NotFound, Conflict<string>, ValidationProblem>> 
                (Guid id, UpdateUserDTO dto, IValidator<UpdateUserDTO> validator, IUserService service) =>
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return TypedResults.ValidationProblem(validation.ToDictionary());

                var updated = await service.UpdateUserAsync(id, dto);
                return updated is not null
                    ? TypedResults.Ok(updated)
                    : TypedResults.NotFound();
            });


            group.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> 
                (Guid id, IUserService service) =>
                await service.DeleteUserAsync(id)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound());
                    
        }
    }
}