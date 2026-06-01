using Microsoft.AspNetCore.Http.HttpResults;
using Source.Infrastructure.Authentication;

namespace Source.Features.User
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/users");

            group.MapGet("/{userId:guid}", async Task<Results<Ok<UserResponse>, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
                (Guid userId, UserServices userService, HttpContext httpContext) =>
            {
                var currentUserId = httpContext.User.GetUserId();
                if (currentUserId is null)
                {
                    return TypedResults.Unauthorized();
                }

                if (currentUserId.Value != userId)
                {
                    return TypedResults.Forbid();
                }

                var user = await userService.GetUserByIdAsync(userId);

                return user is not null
                    ? TypedResults.Ok(new UserResponse(user.Id, user.Username, user.Email))
                    : TypedResults.NotFound();
            }).RequireAuthorization();

            group.MapPost("/", async Task<Results<Created<UserResponse>, BadRequest>>
                (UserCreateRequest dto, UserServices userService, UserSessionService sessionService, HttpContext httpContext) =>
            {
                var user = await userService.CreateUserAsync(dto);

                if (user is null)
                {
                    return TypedResults.BadRequest();
                }

                await sessionService.SignInAsync(httpContext, user);
                return TypedResults.Created($"/users/{user.Id}", new UserResponse(user.Id, user.Username, user.Email));
            });

            group.MapPost("/login", async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult>>
                (UserLoginRequest request, UserServices userService, UserSessionService sessionService, HttpContext httpContext) =>
            {
                var user = await userService.ValidateUserCredentialsAsync(request.Email, request.Password);

                if (user is null)
                {
                    return TypedResults.Unauthorized();
                }

                await sessionService.SignInAsync(httpContext, user);
                return TypedResults.Ok(new UserResponse(user.Id, user.Username, user.Email));
            });

            group.MapPost("/logout", async Task<Results<NoContent, UnauthorizedHttpResult>>
                (UserSessionService sessionService, HttpContext httpContext) =>
            {
                if (!httpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    return TypedResults.Unauthorized();
                }

                await sessionService.SignOutAsync(httpContext);
                return TypedResults.NoContent();
            }).RequireAuthorization();

            group.MapGet("/me", async Task<Results<Ok<UserResponse>, NotFound, UnauthorizedHttpResult>>
                (UserServices userService, HttpContext httpContext) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId is null)
                {
                    return TypedResults.Unauthorized();
                }

                var user = await userService.GetUserByIdAsync(userId.Value);
                return user is not null
                    ? TypedResults.Ok(new UserResponse(user.Id, user.Username, user.Email))
                    : TypedResults.NotFound();
            }).RequireAuthorization();

            group.MapPut("/{userId:guid}", async Task<Results<NoContent, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
                (Guid userId, UserUpdateRequest dto, UserServices userService, HttpContext httpContext) =>
            {
                var currentUserId = httpContext.User.GetUserId();
                if (currentUserId is null)
                {
                    return TypedResults.Unauthorized();
                }

                if (currentUserId.Value != userId)
                {
                    return TypedResults.Forbid();
                }

                var user = await userService.UpdateUserAsync(userId, dto);

                return user is not null ? TypedResults.NoContent() : TypedResults.NotFound();
            }).RequireAuthorization();

            group.MapDelete("/{userId:guid}", async Task<Results<NoContent, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
                (Guid userId, UserServices userService, UserSessionService sessionService, HttpContext httpContext) =>
            {
                var currentUserId = httpContext.User.GetUserId();
                if (currentUserId is null)
                {
                    return TypedResults.Unauthorized();
                }

                if (currentUserId.Value != userId)
                {
                    return TypedResults.Forbid();
                }

                var success = await userService.DeleteUserAsync(userId);

                if (!success)
                {
                    return TypedResults.NotFound();
                }

                await sessionService.SignOutAsync(httpContext);
                return TypedResults.NoContent();
            }).RequireAuthorization();
        }
    }
}
