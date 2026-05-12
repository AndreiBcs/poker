namespace LobbyService.Domain
{
    public class Lobby(string name, Guid host)
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = name;
        public Guid HostId { get; init; } = host;
        public List<Player> Players { get; set; } = [];
        public bool HasStarted { get; set; } = false;
        public bool CanStart { get; set; } = false;


        public static int MaxPlayers { get; } = 6;
        public static int MinPlayers { get; } = 2;
    }
}