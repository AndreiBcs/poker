namespace UserService.Infrastructure.Messaging
{
    public class RabbitMqOptions
    {
        public const string Section = "RabbitMQ";

        public string Host { get; init; } = "localhost";
        public int Port { get; init; } = 5672;
        public string Username { get; init; } = "guest";
        public string Password { get; init; } = "deakRegele";
    }
}