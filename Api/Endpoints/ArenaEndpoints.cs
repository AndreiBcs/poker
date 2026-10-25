using Api.Contracts;
using Api.Hubs;
using Api.Services;

namespace Api.Endpoints;

public static class ArenaEndpoints
{
    public static void MapArenaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", () => 
            new { status = "ok" });
        
        app.MapPost("/api/players", (RegisterPlayer request, Arena arena) => 
            Results.Ok(arena.Register(request)));
        
        app.MapGet("/api/players/{id:guid}", (Guid id, Arena arena) => 
            arena.GetPlayer(id));
        
        app.MapGet("/api/lobbies", (Arena arena) => 
            arena.List());
        
        app.MapPost("/api/lobbies", (CreateLobby request, HttpContext context, Arena arena) => 
            Results.Ok(new { id = arena.Create(PlayerId(context), request) }));
        
        app.MapGet("/api/lobbies/{id:guid}", (Guid id, HttpContext context, Arena arena) => 
            arena.State(id, PlayerId(context)));
        
        app.MapPost("/api/lobbies/{id:guid}/join",
            async (Guid id, JoinLobby request, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Join(id, PlayerId(context), request); await updates.Publish(id); return Results.NoContent();
            });
        
        app.MapPost("/api/lobbies/{id:guid}/leave",
            async (Guid id, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Leave(id, PlayerId(context)); await updates.Publish(id); return Results.NoContent();
            });
        
        app.MapDelete("/api/lobbies/{id:guid}/players/{target:guid}",
            async (Guid id, Guid target, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Kick(id, PlayerId(context), target); await updates.Publish(id); return Results.NoContent();
            });
        
        app.MapPost("/api/lobbies/{id:guid}/start",
            async (Guid id, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Start(id, PlayerId(context)); await updates.Publish(id); return Results.NoContent();
            });
        
        app.MapPost("/api/lobbies/{id:guid}/actions",
            async (Guid id, ActionRequest request, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Act(id, PlayerId(context), request); await updates.Publish(id); return Results.NoContent();
            });
        
        app.MapHub<ArenaHub>("/api/live");
    }

    private static Guid PlayerId(HttpContext context)
    {
        if (!Guid.TryParse(context.Request.Headers["X-Player-Id"],
                out var id))
        {
            throw new ArenaException(400, "Set a nickname before playing.");
        }
        
        return id;
    }
}
