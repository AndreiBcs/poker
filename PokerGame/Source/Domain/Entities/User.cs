using System.Text.Json.Serialization;

namespace Source.Domain.Entities
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;

        [JsonIgnore]
        public string PasswordHash { get; set; } = default!;

        public Guid? LobbyId { get; set; }

        [JsonIgnore]
        public Lobby? Lobby { get; set; }
    }
}
