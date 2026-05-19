namespace Source.Features.Lobby
{
    public record LobbyListItemResponse(
        Guid Id,
        string Name,
        int CurrentPlayers,
        int MaxPlayers,
        int SmallBlind,
        int BigBlind);

    public record LobbyPlayerResponse(Guid Id, string Username);

    public record LobbyDetailResponse(
        Guid Id,
        string Name,
        int MaxPlayers,
        int SmallBlind,
        int BigBlind,
        IReadOnlyList<LobbyPlayerResponse> Players);
}
