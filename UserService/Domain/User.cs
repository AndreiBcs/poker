namespace UserService.Domain
{
    public class User
    {
        public Guid Id { get; init; } = default;
        public string Username { get; set; } = default!;
        public DateTime CreatedAt { get; init; } = default;        
    }
}