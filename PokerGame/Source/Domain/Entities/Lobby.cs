namespace Source.Domain.Entities
{
    public class Lobby
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public List<User> Players { get; set; } = new List<User>(6);
        public int MaxPlayers { get; set; } = 6;
        public int SmallBlind { get; set; } = 50;
        public int BigBlind { get; set; } = 100;

    }
}