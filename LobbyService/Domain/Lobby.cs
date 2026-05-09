namespace LobbyService.Domain
{
    public enum LobbyStatus
    {
        Waiting,
        InGame,
        Closed
    }

    public class Lobby(string name, Guid host)
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = name;
        public Guid HostId { get; init; } = host;
        public int MaxPlayers { get; } = 6;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public LobbyStatus Status { get; set; } = LobbyStatus.Waiting;
        public ICollection<Player> Players { get; set; } = [];
    }
}