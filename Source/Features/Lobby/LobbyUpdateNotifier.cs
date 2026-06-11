using System.Threading.Channels;

namespace Source.Features.Lobby;

public class LobbyUpdateNotifier
{
    // Standard C# event to notify subscribers
    public event Action? OnLobbyUpdated;

    // Call this whenever a lobby is created, joined, edited, or deleted
    public void Notify()
    {
        OnLobbyUpdated?.Invoke();
    }
}