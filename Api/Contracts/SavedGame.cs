namespace Api.Contracts;

public sealed record SavedGame(Guid GameId, Guid? SessionId, string Name);
