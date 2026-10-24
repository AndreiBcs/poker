using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Api;

// Project private cards separately for every connection, never for a shared group.
public sealed class ArenaUpdates(Arena arena, IHubContext<ArenaHub> hub)
{
    private readonly ConcurrentDictionary<string, (Guid RoomId, Guid PlayerId)> _connections = new();
    private readonly SemaphoreSlim _publishGate = new(1, 1);

    public void Connect(string connectionId, Guid roomId, Guid playerId)
    {
        arena.Connect(roomId, playerId);
        _connections[connectionId] = (roomId, playerId);
    }

    public async Task Disconnect(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var seat)) return;
        arena.Disconnect(seat.RoomId, seat.PlayerId);
        await Publish(seat.RoomId);
    }

    public async Task Publish(Guid roomId)
    {
        await _publishGate.WaitAsync();
        try
        {
            foreach (var (connectionId, seat) in _connections.Where(c => c.Value.RoomId == roomId))
            {
                object state;
                try { state = arena.State(roomId, seat.PlayerId); }
                catch (ArenaException ex)
                {
                    if (_connections.TryRemove(connectionId, out _)) arena.Disconnect(roomId, seat.PlayerId);
                    await hub.Clients.Client(connectionId).SendAsync("Removed", ex.Message);
                    continue;
                }
                await hub.Clients.Client(connectionId).SendAsync("State", state);
            }
        }
        finally { _publishGate.Release(); }
    }
}
