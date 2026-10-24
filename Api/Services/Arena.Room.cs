using Api.Contracts;
using Core.Game;

namespace Api.Services;

public sealed partial class Arena
{
    private sealed class Room
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required string Name { get; init; }
        public required Guid OwnerId { get; set; }
        public required CreateLobby Settings { get; init; }
        public required byte[] Salt { get; init; }
        public required byte[] PasswordHash { get; init; }
        public List<Player> Players { get; } = new();
        public Dictionary<Guid, int> Connections { get; } = new();
        public GameSession? Game { get; set; }
        public bool StartRequested { get; set; }
        public string Notice { get; private set; } = "Lobby created.";
        public long Revision { get; private set; }
        public void Announce(string text) { Notice = text; Revision++; }
    }
}
