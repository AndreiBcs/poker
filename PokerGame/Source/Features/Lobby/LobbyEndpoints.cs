using Microsoft.AspNetCore.Http.HttpResults;

namespace Source.Features.Lobby
{
    public static class LobbyEndpoints
    {
        public static void MapLobbyEndpoints(this WebApplication app)
        {
            
            var group = app.MapGroup("/lobbies").RequireAuthorization();



            group.MapGet("/{lobbyId:guid}", async Task<Results<Ok<Domain.Entities.Lobby>, NotFound>>
                (Guid lobbyId, LobbyService lobbyService) =>
            {
                var lobby = await lobbyService.GetLobbyByIdAsync(lobbyId);

                return lobby is not null ? TypedResults.Ok(lobby) : TypedResults.NotFound();
            });



            group.MapGet("/", async Task<Results<Ok<List<Domain.Entities.Lobby>>>>
                (LobbyService lobbyService) =>
            {
                var lobbies = await lobbyService.GetAllLobbiesAsync();

                return TypedResults.Ok(lobbies);
            });



            group.MapPost("/{lobbyId:guid}/join", async Task<Results<NoContent, NotFound, Unauthorized>>
                (Guid lobbyId, LobbyService lobbyService, HttpContext httpContext) =>
            {
                if(!httpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    return TypedResults.Unauthorized();
                }
                var success = await lobbyService.JoinLobbyAsync(lobbyId, httpContext);

                return success ? TypedResults.NoContent() : TypedResults.NotFound();
            });



            group.MapPost("/{lobbyId:guid}/leave", async Task<Results<NoContent, NotFound, Unauthorized>>
                (Guid lobbyId, LobbyService lobbyService, HttpContext httpContext) =>
            {
                if(!httpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    return TypedResults.Unauthorized();
                }
                var success = await lobbyService.LeaveLobbyAsync(lobbyId, httpContext);

                return success ? TypedResults.NoContent() : TypedResults.NotFound();
            });



            group.MapPost("/", async Task<Results<Created<Domain.Entities.Lobby>, BadRequest>>
                (LobbyCreateRequest dto, LobbyService lobbyService) =>
            {
                var lobby = await lobbyService.CreateLobbyAsync(dto);

                return lobby is not null ?
                            TypedResults.Created($"/users/{lobby.Id}", lobby) : 
                            TypedResults.BadRequest();
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