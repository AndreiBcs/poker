using Microsoft.AspNetCore.Http.HttpResults;

namespace Source.Features.User
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {

            var group = app.MapGroup("/users");



            group.MapGet("/{userId:guid}", async Task<Results<Ok<string>, NotFound>>(Guid userId, UserServices userService) =>
            {
                var user = await userService.GetUserByIdAsync(userId);

                return user is not null ? TypedResults.Ok(user.Username) : TypedResults.NotFound();
            });



            group.MapPost("/", async Task<Results<Created<string>, BadRequest>>(UserCreateRequest dto, UserServices userService) =>
            {
                var user = await userService.CreateUserAsync(dto);

                return user is not null ? TypedResults.Created($"/users/{user.Id}", user.Username) : TypedResults.BadRequest();
            });



            group.MapPost("/login", async Task<Results<Ok<string>, UnauthorizedHttpResult>>(UserLoginRequest request, UserServices userService) =>
            {
                var user = await userService.ValidateUserCredentialsAsync(request.Email, request.Password);

                return user is not null ? TypedResults.Ok(user.Username) : TypedResults.Unauthorized();
            });



            group.MapPut("/{userId:guid}/email", async (Guid userId, UserEmailUpdateRequest request, UserServices userService) =>
            {
                var success = await userService.UpdateUserEmailAsync(userId, request.NewEmail);
                return success ? Results.NoContent() : Results.NotFound();
            });



            group.MapPut("/{userId:guid}/password", async (Guid userId, UserPasswordUpdateRequest request, UserServices userService) =>
            {
                var success = await userService.UpdateUserPasswordAsync(userId, request.NewPassword);
                return success ? Results.NoContent() : Results.NotFound();
            });



            group.MapPut("/{userId:guid}/username", async (Guid userId, UserUsernameUpdateRequest request, UserServices userService) =>
            {
                var success = await userService.UpdateUsernameAsync(userId, request.NewUsername);
                return success ? Results.NoContent() : Results.NotFound();
            });



            group.MapDelete("/{userId:guid}", async Task<Results<NoContent, NotFound>>(Guid userId, UserServices userService) =>
            {
                var success = await userService.DeleteUserAsync(userId);
                if (success)
                {
                    return TypedResults.NoContent();
                }
                else
                {
                    return TypedResults.NotFound();
                }
            });
        }
    }
}