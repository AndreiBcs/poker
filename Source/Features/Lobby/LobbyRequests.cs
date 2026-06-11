using Source.Domain.Enums;

namespace Source.Features.Lobby
{
   public record LobbyCreateRequest(
    string Name,
    int MaxPlayers,
    int SmallBlind,
    int BigBlind ,
    GameType GameType);
    public record LobbyUpdateRequest(
    string Name,
    int MaxPlayers,
    int SmallBlind,
    int BigBlind,
    GameType GameType);
    
}