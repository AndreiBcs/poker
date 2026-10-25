namespace Core.Game;

public enum GameStartStatus
{
    Started,
    AlreadyInProgress,
    NotEnoughPlayers
}
public record GameActionResult(bool Success, string? Error);
