namespace UserService.Infrastructure.Messaging
{

    // consumed - published by AuthService when a user registers
    public record UserRegisteredEvent(Guid UserId, string Username);


    // published
    public record UserDeletedEvent(Guid UserId);
    public record UserUpdatedEvent(Guid UserId, string NewUsername);
}