using System.Security.Cryptography;

namespace Api;

public sealed partial class Arena
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Player> _players = new();
    private readonly Dictionary<Guid, Room> _rooms = new();

    public Player Register(RegisterPlayer request)
    {
        lock (_gate)
        {
            var nickname = Clean(request.Nickname, 24, "Nickname");
            var player = new Player(Guid.NewGuid(), nickname);
            _players.Add(player.Id, player);
            return player;
        }
    }

    public Player GetPlayer(Guid id)
    {
        lock (_gate) return _players.GetValueOrDefault(id) ?? throw new ArenaException(404, "Player not found. Set your nickname again.");
    }

    public object[] List()
    {
        lock (_gate) return _rooms.Values.Where(r => !r.StartRequested).Select(Summary).ToArray();
    }

    public Guid Create(Guid playerId, CreateLobby request)
    {
        lock (_gate)
        {
            var player = GetPlayer(playerId);
            EnsureAvailable(playerId);
            var name = Clean(request.Name, 60, "Lobby name");
            ValidatePassword(request.Password);
            if (request.MaxPlayers is < 2 or > 10) Fail("Choose 2–10 players.");
            if (request.SmallBlind is < 1 or > 1000 || request.BigBlind < request.SmallBlind || request.BigBlind > 2000)
                Fail("Blinds must be positive, with small blind <= big blind <= 2000.");
            var salt = RandomNumberGenerator.GetBytes(16);
            var room = new Room
            {
                Name = name,
                OwnerId = playerId,
                Settings = request with { Password = "" },
                Salt = salt,
                PasswordHash = Hash(request.Password, salt)
            };
            room.Players.Add(player);
            _rooms.Add(room.Id, room);
            return room.Id;
        }
    }

    public void Join(Guid roomId, Guid playerId, JoinLobby request)
    {
        lock (_gate)
        {
            var room = Find(roomId);
            var player = GetPlayer(playerId);
            if (room.Players.Any(p => p.Id == playerId)) return;
            if (room.StartRequested) Fail("This game has already started.");
            if (room.Players.Count >= room.Settings.MaxPlayers) Fail("Lobby is full.");
            EnsureAvailable(playerId);
            ValidatePassword(request.Password);
            if (!CryptographicOperations.FixedTimeEquals(Hash(request.Password, room.Salt), room.PasswordHash))
                throw new ArenaException(403, "Incorrect lobby password.");
            room.Players.Add(player);
            room.Announce($"{player.Nickname} joined.");
        }
    }

    public void Kick(Guid roomId, Guid playerId, Guid target)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            RequireOwner(room, playerId);
            if (room.StartRequested) Fail("Players can only be kicked in the waiting room.");
            if (target == playerId) Fail("Use Leave to close your lobby.");
            var player = room.Players.FirstOrDefault(p => p.Id == target) ?? throw new ArenaException(404, "Player not found.");
            room.Players.Remove(player);
            room.Connections.Remove(target);
            room.Announce($"{player.Nickname} was removed by the host.");
        }
    }

    public void Leave(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var player = room.Players.First(p => p.Id == playerId);
            room.Game?.HandlePlayerLeft(playerId);
            room.Players.Remove(player);
            room.Connections.Remove(playerId);
            if (room.Players.Count == 0) { _rooms.Remove(roomId); return; }
            if (room.OwnerId == playerId) room.OwnerId = room.Players[0].Id;
            if (room.Game is null && room.Players.Count < 2) room.StartRequested = false;
            room.Announce($"{player.Nickname} left.");
            TryStart(room);
        }
    }

    private static object Summary(Room room) => new
    {
        room.Id,
        room.Name,
        room.Settings.MaxPlayers,
        PlayerCount = room.Players.Count,
        HasPassword = true
    };
    private Room Find(Guid id) => _rooms.GetValueOrDefault(id) ?? throw new ArenaException(404, "Lobby no longer exists.");
    private Room Member(Guid roomId, Guid playerId)
    {
        var room = Find(roomId);
        return room.Players.All(p => p.Id != playerId) ? throw new ArenaException(403, "You are no longer in this lobby.") : room;
    }
    private void EnsureAvailable(Guid id)
    {
        if (_rooms.Values.Any(r => r.Players.Any(p => p.Id == id))) Fail("Leave your current lobby first.");
    }
    private static void RequireOwner(Room room, Guid id)
    {
        if (room.OwnerId != id) throw new ArenaException(403, "Only the host can do that.");
    }
    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length > 100) Fail("A lobby password of 1–100 characters is required.");
    }
    private static byte[] Hash(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    private static string Clean(string? value, int max, string label)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > max || text.Any(char.IsControl)) Fail($"{label} must contain 1–{max} printable characters.");
        return text!;
    }
    private static void Fail(string message) => throw new ArenaException(400, message);
}
