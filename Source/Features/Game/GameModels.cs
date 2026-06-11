using System.Text.Json;
using System.Text.Json.Serialization;
using Source.Domain.Enums;

namespace Source.Features.Game;

public static class GameJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

public record GameClientMessage(string Type, JsonElement Payload);

public record GameActionPayload(string Action, int Amount);

public enum GameStartStatus
{
    LobbyNotFound,
    NotInLobby,
    NotLeader,
    Started,
    AlreadyInProgress,
    NotEnoughPlayers
}

public record GameStartOutcome(GameStartStatus Status, string? Error);

public record GameActionResult(bool Success, string? Error);

public record GameStateDto(
    Guid LobbyId,
    GamePhase Phase,
    int Pot,
    int CurrentBet,
    int MinRaise,
    Guid? CurrentTurnUserId,
    Guid? DealerUserId,
    Guid? SmallBlindUserId,
    Guid? BigBlindUserId,
    IReadOnlyList<CardDto> Board,
    IReadOnlyList<GamePlayerStateDto> Players,
    GameResultDto? LastResult,
    Guid ViewerUserId);

public record GamePlayerStateDto(
    Guid UserId,
    string Username,
    int SeatIndex,
    int Chips,
    int CurrentBet,
    bool HasFolded,
    bool IsAllIn,
    bool IsDealer,
    bool IsSmallBlind,
    bool IsBigBlind,
    bool IsConnected,
    string? IpAddress,
    IReadOnlyList<CardDto> HoleCards);

public record CardDto(string Rank, string Suit, string Code);

public record GameResultDto(int Pot, string? HandDescription, IReadOnlyList<GameWinnerDto> Winners);

public record GameWinnerDto(Guid UserId, string Username, int Winnings);

public record GameErrorDto(string Message);

public record GameLobbyInfo(Guid LobbyId, string LobbyName, int SmallBlind, int BigBlind, Guid? OwnerUserId);
