namespace LobbyService.Domain
{
    public class Player
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = null!;
        public bool IsReady { get; set; }
    }
}