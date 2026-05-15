namespace Source.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
    }
}