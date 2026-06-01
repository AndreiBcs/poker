namespace Source.Features.Lobby
{
    public record LobbyCreateRequest(string Name, int MaxPlayers, int SmallBlind, int BigBlind);
    public record LobbyUpdateRequest(string Name, int MaxPlayers, int SmallBlind, int BigBlind);
    
}