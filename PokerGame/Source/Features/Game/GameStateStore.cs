using System.Collections.Concurrent;

namespace Source.Features.Game;

public sealed class GameStateStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public GameSession GetOrAdd(Guid lobbyId, Func<GameSession> factory)
        => _sessions.GetOrAdd(lobbyId, _ => factory());

    public bool TryGet(Guid lobbyId, out GameSession session)
        => _sessions.TryGetValue(lobbyId, out session!);

    public void Remove(Guid lobbyId)
        => _sessions.TryRemove(lobbyId, out _);
}
