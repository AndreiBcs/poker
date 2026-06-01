using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Source.Features.Game;

public sealed record GameConnection(Guid UserId, Guid LobbyId, WebSocket Socket, string? IpAddress);

public sealed class GameConnectionManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, GameConnection>> _connections = new();

    public void AddConnection(Guid lobbyId, Guid userId, WebSocket socket, string? ipAddress)
    {
        var lobbyConnections = _connections.GetOrAdd(lobbyId, _ => new ConcurrentDictionary<Guid, GameConnection>());
        lobbyConnections[userId] = new GameConnection(userId, lobbyId, socket, ipAddress);
    }

    public void RemoveConnection(Guid lobbyId, Guid userId)
    {
        if (_connections.TryGetValue(lobbyId, out var lobbyConnections))
        {
            lobbyConnections.TryRemove(userId, out _);
        }
    }

    public IReadOnlyList<GameConnection> GetConnections(Guid lobbyId)
    {
        if (_connections.TryGetValue(lobbyId, out var lobbyConnections))
        {
            return lobbyConnections.Values.ToList();
        }

        return Array.Empty<GameConnection>();
    }

    public IReadOnlyDictionary<Guid, string?> GetIpAddresses(Guid lobbyId)
    {
        if (_connections.TryGetValue(lobbyId, out var lobbyConnections))
        {
            return lobbyConnections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IpAddress);
        }

        return new Dictionary<Guid, string?>();
    }

    public async Task SendAsync(Guid lobbyId, Guid userId, object message, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(lobbyId, out var lobbyConnections))
        {
            return;
        }

        if (!lobbyConnections.TryGetValue(userId, out var connection))
        {
            return;
        }

        var payload = JsonSerializer.Serialize(message, GameJson.Options);
        await TrySendAsync(connection.Socket, payload, cancellationToken);
    }

    public async Task BroadcastAsync(Guid lobbyId, Func<Guid, object> messageFactory, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(lobbyId, out var lobbyConnections))
        {
            return;
        }

        foreach (var (userId, connection) in lobbyConnections.ToList())
        {
            var payload = JsonSerializer.Serialize(messageFactory(userId), GameJson.Options);
            var success = await TrySendAsync(connection.Socket, payload, cancellationToken);
            if (!success)
            {
                lobbyConnections.TryRemove(userId, out _);
            }
        }
    }

    private static async Task<bool> TrySendAsync(WebSocket socket, string payload, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            return true;
        }
        catch (WebSocketException)
        {
            return false;
        }
    }
}
