using Microsoft.AspNetCore.Http.HttpResults;
using Source.Infrastructure.Authentication;

namespace Source.Features.Lobby
{
    public static class LobbyEndpoints
    {
        public static void MapLobbyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/lobbies").RequireAuthorization();

            group.MapGet("/{lobbyId:guid}", async Task<Results<Ok<LobbyDetailResponse>, NotFound>>
                (Guid lobbyId, LobbyService lobbyService) =>
            {
                var lobby = await lobbyService.GetLobbyByIdAsync(lobbyId, includePlayers: true);

                if (lobby is null)
                {
                    return TypedResults.NotFound();
                }

                var response = new LobbyDetailResponse(
                    lobby.Id,
                    lobby.Name,
                    lobby.MaxPlayers,
                    lobby.SmallBlind,
                    lobby.BigBlind,
                    lobby.Players.Select(player => new LobbyPlayerResponse(player.Id, player.Username)).ToList());

                return TypedResults.Ok(response);
            });

            group.MapGet("/", async Task<Ok<List<LobbyListItemResponse>>>
                (LobbyService lobbyService) =>
            {
                var lobbies = await lobbyService.GetAllLobbiesAsync(includePlayers: true);
                var response = lobbies.Select(lobby => new LobbyListItemResponse(
                    lobby.Id,
                    lobby.Name,
                    lobby.Players.Count,
                    lobby.MaxPlayers,
                    lobby.SmallBlind,
                    lobby.BigBlind)).ToList();

                return TypedResults.Ok(response);
            });

            group.MapPost("/{lobbyId:guid}/join", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, Conflict, BadRequest>>
                (Guid lobbyId, LobbyService lobbyService, HttpContext httpContext) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId is null)
                {
                    return TypedResults.Unauthorized();
                }

                var result = await lobbyService.JoinLobbyAsync(lobbyId, userId.Value);
                return result switch
                {
                    LobbyJoinResult.Success => TypedResults.NoContent(),
                    LobbyJoinResult.LobbyFull => TypedResults.Conflict(),
                    LobbyJoinResult.LobbyNotFound => TypedResults.NotFound(),
                    LobbyJoinResult.UserNotFound => TypedResults.BadRequest(),
                    _ => TypedResults.BadRequest()
                };
            });

            group.MapPost("/{lobbyId:guid}/leave", async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, BadRequest>>
                (Guid lobbyId, LobbyService lobbyService, HttpContext httpContext) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId is null)
                {
                    return TypedResults.Unauthorized();
                }

                var result = await lobbyService.LeaveLobbyAsync(lobbyId, userId.Value);
                return result switch
                {
                    LobbyLeaveResult.Success => TypedResults.NoContent(),
                    LobbyLeaveResult.LobbyNotFound => TypedResults.NotFound(),
                    LobbyLeaveResult.UserNotFound => TypedResults.BadRequest(),
                    LobbyLeaveResult.NotInLobby => TypedResults.BadRequest(),
                    _ => TypedResults.BadRequest()
                };
            });

            group.MapPost("/", async Task<Results<Created<LobbyDetailResponse>, BadRequest>>
                (LobbyCreateRequest dto, LobbyService lobbyService) =>
            {
                var lobby = await lobbyService.CreateLobbyAsync(dto);

                if (lobby is null)
                {
                    return TypedResults.BadRequest();
                }

                var response = new LobbyDetailResponse(
                    lobby.Id,
                    lobby.Name,
                    lobby.MaxPlayers,
                    lobby.SmallBlind,
                    lobby.BigBlind,
                    new List<LobbyPlayerResponse>());

                return TypedResults.Created($"/lobbies/{lobby.Id}", response);
            });

            group.MapPut("/{lobbyId:guid}", async Task<Results<NoContent, NotFound>>
                (Guid lobbyId, LobbyUpdateRequest dto, LobbyService lobbyService) =>
            {
                var lobby = await lobbyService.UpdateLobbyAsync(lobbyId, dto);

                return lobby is not null ? TypedResults.NoContent() : TypedResults.NotFound();
            });

            group.MapDelete("/{lobbyId:guid}", async Task<Results<NoContent, NotFound>>
                (Guid lobbyId, LobbyService lobbyService) =>
            {
                var success = await lobbyService.DeleteLobbyAsync(lobbyId);

                return success ? TypedResults.NoContent() : TypedResults.NotFound();
            });
        }
    }
}
