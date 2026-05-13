namespace Source.Domain.Entities
{
    public class Lobby
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public List<User> Players { get; set; } = [6];
        public int SmallBling { get; set; } = 50;
        public int BigBlind { get; set; } = 100;

    }
}