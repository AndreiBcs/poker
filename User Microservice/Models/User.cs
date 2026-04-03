namespace User_Microservice.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int Level { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
