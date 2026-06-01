using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Source.Domain.Enums;
using Source.Infrastructure.Authentication;

namespace Source.Features.Game;

public sealed class GameWebSocketHandler
{
    private readonly GameService _gameService;
    private readonly GameConnectionManager _connections;
    private readonly ILogger<GameWebSocketHandler> _logger;

    public GameWebSocketHandler(GameService gameService, GameConnectionManager connections, ILogger<GameWebSocketHandler> logger)
    {
        _gameService = gameService;
        _connections = connections;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var lobbyIdValue = context.Request.Query["lobbyId"].ToString();
        if (!Guid.TryParse(lobbyIdValue, out var lobbyId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var isMember = await _gameService.IsUserInLobbyAsync(lobbyId, userId.Value);
        if (!isMember)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        _connections.AddConnection(lobbyId, userId.Value, socket, ipAddress);
        await _gameService.RegisterConnectionAsync(lobbyId, userId.Value, ipAddress);

        await ReceiveLoopAsync(socket, lobbyId, userId.Value);

        _connections.RemoveConnection(lobbyId, userId.Value);
        await _gameService.UnregisterConnectionAsync(lobbyId, userId.Value);
    }

    private async Task ReceiveLoopAsync(WebSocket socket, Guid lobbyId, Guid userId)
    {
        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open)
        {
            var message = await ReceiveMessageAsync(socket, buffer);
            if (message is null)
            {
                break;
            }

            await HandleMessageAsync(message, lobbyId, userId);
        }
    }

    private async Task HandleMessageAsync(string message, Guid lobbyId, Guid userId)
    {
        GameClientMessage? clientMessage;
        try
        {
            clientMessage = JsonSerializer.Deserialize<GameClientMessage>(message, GameJson.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid websocket message payload.");
            await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto("Invalid message.") });
            return;
        }

        if (clientMessage is null || string.IsNullOrWhiteSpace(clientMessage.Type))
        {
            await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto("Invalid message.") });
            return;
        }

        switch (clientMessage.Type.Trim().ToLowerInvariant())
        {
            case "start_game":
                var startOutcome = await _gameService.StartGameAsync(lobbyId, userId);
                if (startOutcome.Status != GameStartStatus.Started)
                {
                    await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto(startOutcome.Error ?? "Cannot start game.") });
                }

                break;
            case "action":
                var actionPayload = clientMessage.Payload.Deserialize<GameActionPayload>(GameJson.Options);
                if (actionPayload is null)
                {
                    await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto("Invalid action payload.") });
                    return;
                }

                if (!TryParseAction(actionPayload.Action, out var action))
                {
                    await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto("Unknown action.") });
                    return;
                }

                var result = await _gameService.ApplyActionAsync(lobbyId, userId, action, actionPayload.Amount);
                if (!result.Success)
                {
                    await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto(result.Error ?? "Action failed.") });
                }

                break;
            default:
                await _connections.SendAsync(lobbyId, userId, new { type = "error", payload = new GameErrorDto("Unsupported message type.") });
                break;
        }
    }

    private static bool TryParseAction(string? actionValue, out PlayerAction action)
    {
        action = PlayerAction.Check;
        if (string.IsNullOrWhiteSpace(actionValue))
        {
            return false;
        }

        return Enum.TryParse(actionValue, true, out action);
    }

    private static async Task<string?> ReceiveMessageAsync(WebSocket socket, byte[] buffer)
    {
        var builder = new StringBuilder();
        WebSocketReceiveResult? result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            var segment = Encoding.UTF8.GetString(buffer, 0, result.Count);
            builder.Append(segment);
        }
        while (!result.EndOfMessage);

        return builder.ToString();
    }
}
