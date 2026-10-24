namespace Api;

public record CreateLobby(string Name, string Password, int MaxPlayers = 6,
    int SmallBlind = 25, int BigBlind = 50);
