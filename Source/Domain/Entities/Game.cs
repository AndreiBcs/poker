namespace Source.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public List<User> Players { get; set; } = new List<User>(6);
        public int SmallBlind { get; set; } = 50;
        public int BigBlind { get; set; } = 100;
    }
}