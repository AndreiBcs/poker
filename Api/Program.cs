using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokerArena.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Arena>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
var app = builder.Build();
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (ArenaException ex) when (!context.Response.HasStarted)
    {
        context.Response.StatusCode = ex.Status;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});
app.UseWebSockets();
app.MapGet("/api/health", () => new { status = "ok" });
app.MapPost("/api/players", (RegisterPlayer request, Arena arena) => Results.Ok(arena.Register(request)));
app.MapGet("/api/players/{id:guid}", (Guid id, Arena arena) => arena.GetPlayer(id));
app.MapGet("/api/lobbies", (Arena arena) => arena.List());
app.MapPost("/api/lobbies", (CreateLobby request, HttpContext context, Arena arena) => Results.Ok(new { id = arena.Create(PlayerId(context), request) }));
app.MapGet("/api/lobbies/{id:guid}", (Guid id, HttpContext context, Arena arena) => arena.State(id, PlayerId(context)));
app.MapPost("/api/lobbies/{id:guid}/join", (Guid id, JoinLobby request, HttpContext context, Arena arena) =>
{ arena.Join(id, PlayerId(context), request); return Results.NoContent(); });
app.MapPost("/api/lobbies/{id:guid}/leave", (Guid id, HttpContext context, Arena arena) =>
{ arena.Leave(id, PlayerId(context)); return Results.NoContent(); });
app.MapDelete("/api/lobbies/{id:guid}/players/{target:guid}", (Guid id, Guid target, HttpContext context, Arena arena) =>
{ arena.Kick(id, PlayerId(context), target); return Results.NoContent(); });
app.MapPost("/api/lobbies/{id:guid}/start", (Guid id, HttpContext context, Arena arena) =>
{ arena.Start(id, PlayerId(context)); return Results.NoContent(); });
app.MapPost("/api/lobbies/{id:guid}/actions", (Guid id, ActionRequest request, HttpContext context, Arena arena) =>
{ arena.Act(id, PlayerId(context), request); return Results.NoContent(); });
// Only server-created IDs identify seats. They are intentionally not accounts or authentication.
app.Map("/api/lobbies/{id:guid}/live", async (Guid id, HttpContext context, Arena arena) =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    if (!Guid.TryParse(context.Request.Query["playerId"], out var playerId)) throw new ArenaException(400, "Player ID is required.");
    arena.State(id, playerId); // Verify membership before upgrading.
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    arena.Connect(id, playerId);
    using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    var receive = ReceiveUntilClose(socket, lifetime.Token);
    try
    {
        while (socket.State == WebSocketState.Open && !receive.IsCompleted)
        {
            object state;
            try { state = new { type = "state", payload = arena.State(id, playerId) }; }
            catch (ArenaException ex)
            {
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "removed", error = ex.Message }, json), WebSocketMessageType.Text, true, lifetime.Token);
                break;
            }
            await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(state, json), WebSocketMessageType.Text, true, lifetime.Token);
            await Task.WhenAny(receive, Task.Delay(250, lifetime.Token));
        }
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Connection ended", CancellationToken.None);
    }
    catch (Exception ex) when (ex is WebSocketException or OperationCanceledException) { }
    finally
    {
        arena.Disconnect(id, playerId);
        lifetime.Cancel();
        try { await receive; } catch (Exception ex) when (ex is WebSocketException or OperationCanceledException) { }
    }
});
app.Run();

static Guid PlayerId(HttpContext context)
{
    if (!Guid.TryParse(context.Request.Headers["X-Player-Id"], out var id)) throw new ArenaException(400, "Set a nickname before playing.");
    return id;
}
static async Task ReceiveUntilClose(WebSocket socket, CancellationToken token)
{
    var buffer = new byte[1024];
    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(buffer, token);
        if (result.MessageType == WebSocketMessageType.Close) return;
        // Commands use HTTP; stop clients from streaming unsolicited payloads.
        if (result.Count > 0) return;
    }
}
public partial class Program { }
