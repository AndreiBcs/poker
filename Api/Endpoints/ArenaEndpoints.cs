using Api.Contracts;
using Api.Hubs;
using Api.Services;

namespace Api.Endpoints;

public static class ArenaEndpoints
{
    public static void MapArenaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", () => new { status = "ok" });

        app.MapPost("/api/players", (RegisterPlayer request, HttpContext context, Arena arena) =>
        {
            var (player, token) = arena.Register(request, context.Request.Cookies[SeatCookie.Name]);
            context.Response.Cookies.Append(SeatCookie.Name, token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = context.Request.IsHttps,
                MaxAge = TimeSpan.FromDays(365),
                Path = "/api"
            });
            return Results.Ok(player);
        });

        app.MapGet("/api/players/me", (HttpContext context, Arena arena) =>
            arena.SeatState(PlayerId(context, arena)));

        app.MapGet("/api/games/{id:guid}/sessions/{sessionId:guid}",
            (Guid id, Guid sessionId, HttpContext context, Arena arena) =>
                arena.State(id, PlayerId(context, arena), sessionId));

        app.MapGet("/api/lobbies", (Arena arena) => arena.List());

        app.MapPost("/api/lobbies", (CreateLobby request, HttpContext context, Arena arena) =>
            Results.Ok(new { id = arena.Create(PlayerId(context, arena), request) }));

        app.MapGet("/api/lobbies/{id:guid}", (Guid id, HttpContext context, Arena arena) =>
            arena.State(id, PlayerId(context, arena)));

        app.MapPost("/api/lobbies/{id:guid}/join",
            async (Guid id, JoinLobby request, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Join(id, PlayerId(context, arena), request);
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapPost("/api/lobbies/{id:guid}/leave",
            async (Guid id, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Leave(id, PlayerId(context, arena));
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapPost("/api/lobbies/{id:guid}/step-away",
            async (Guid id, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.StepAway(id, PlayerId(context, arena));
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapDelete("/api/lobbies/{id:guid}/players/{target:guid}",
            async (Guid id, Guid target, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Kick(id, PlayerId(context, arena), target);
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapPost("/api/lobbies/{id:guid}/start",
            async (Guid id, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Start(id, PlayerId(context, arena));
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapPost("/api/lobbies/{id:guid}/actions",
            async (Guid id, ActionRequest request, HttpContext context, Arena arena, ArenaUpdates updates) =>
            {
                arena.Act(id, PlayerId(context, arena), request);
                await updates.Publish(id);
                return Results.NoContent();
            });

        app.MapHub<ArenaHub>("/api/live");
    }

    private static Guid PlayerId(HttpContext context, Arena arena) =>
        arena.SeatId(context.Request.Cookies[SeatCookie.Name]);
}
