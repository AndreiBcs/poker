using Microsoft.AspNetCore.Http.HttpResults;
using Source.Infrastructure.Authentication;

namespace Source.Features.Game;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games").RequireAuthorization();

        group.MapPost("/{lobbyId:guid}/start", async Task<Results<Ok, BadRequest, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
            (Guid lobbyId, GameService gameService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null)
            {
                return TypedResults.Unauthorized();
            }

            var outcome = await gameService.StartGameAsync(lobbyId, userId.Value);
            return outcome.Status switch
            {
                GameStartStatus.LobbyNotFound => TypedResults.NotFound(),
                GameStartStatus.NotInLobby => TypedResults.Forbid(),
                GameStartStatus.NotLeader => TypedResults.Forbid(),
                GameStartStatus.NotEnoughPlayers => TypedResults.BadRequest(),
                GameStartStatus.AlreadyInProgress => TypedResults.BadRequest(),
                GameStartStatus.Started => TypedResults.Ok(),
                _ => TypedResults.BadRequest()
            };
        });

        group.MapGet("/{lobbyId:guid}/state", async Task<Results<Ok<GameStateDto>, NotFound, ForbidHttpResult, UnauthorizedHttpResult>>
            (Guid lobbyId, GameService gameService, HttpContext httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null)
            {
                return TypedResults.Unauthorized();
            }

            var isMember = await gameService.IsUserInLobbyAsync(lobbyId, userId.Value);
            if (!isMember)
            {
                return TypedResults.Forbid();
            }

            var state = await gameService.GetStateAsync(lobbyId, userId.Value);
            return state is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(state);
        });

        app.Map("/ws/game", async (HttpContext context, GameWebSocketHandler handler) =>
        {
            await handler.HandleAsync(context);
        }).RequireAuthorization();
    }
}
