namespace Source.Features.User
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/users");

            group.MapGet("/{userId:guid}", async (Guid userId, UserServices userService) =>
            {
                var user = await userService.GetUserByIdAsync(userId);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            group.MapPost("/", async (UserCreateRequest request, UserServices userService) =>
            {
                var user = await userService.CreateUserAsync(request.Username, request.Email, request.Password);
                return Results.Created($"/users/{user.Id}", user);
            });

            group.MapPost("/login", async (UserLoginRequest request, UserServices userService) =>
            {
                var isValid = await userService.ValidateUserCredentialsAsync(request.Username, request.Password);
                return isValid ? Results.Ok() : Results.Unauthorized();
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

            group.MapDelete("/{userId:guid}", async (Guid userId, UserServices userService) =>
            {
                var success = await userService.DeleteUserAsync(userId);
                return success ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}