using System.Security.Cryptography;
using Core.Domain;
using Core.Domain.Enums;
using Core.Game;

namespace Api;

public sealed class ArenaException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public record RegisterPlayer(string Nickname);
public record Player(Guid Id, string Nickname);
public record CreateLobby(string Name, string Password, int MaxPlayers = 6,
    int SmallBlind = 25, int BigBlind = 50);
public record JoinLobby(string Password);
public record ActionRequest(PlayerAction Action, int Amount = 0);

public sealed class Arena
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Player> _players = new();
    private readonly Dictionary<Guid, Room> _rooms = new();

    private sealed class Room
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required string Name { get; init; }
        public required Guid OwnerId { get; set; }
        public required CreateLobby Settings { get; init; }
        public required byte[] Salt { get; init; }
        public required byte[] PasswordHash { get; init; }
        public List<Player> Players { get; } = new();
        public Dictionary<Guid, int> Connections { get; } = new();
        public GameSession? Game { get; set; }
        public bool StartRequested { get; set; }
        public string Notice { get; set; } = "Lobby created.";
        public long Revision { get; set; }
        public void Announce(string text) { Notice = text; Revision++; }
    }

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

    public object State(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var game = room.Game;
            var allConnected = room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0);
            return new
            {
                room.Id,
                room.Name,
                room.OwnerId,
                room.Settings.MaxPlayers,
                room.Settings.SmallBlind,
                room.Settings.BigBlind,
                room.StartRequested,
                Status = !room.StartRequested ? "waiting" : game is null ? "connecting" : allConnected ? "playing" : "paused",
                room.Notice,
                room.Revision,
                Players = room.Players.Select(p => new { p.Id, p.Nickname, Connected = room.Connections.GetValueOrDefault(p.Id) > 0 }).ToArray(),
                Game = game is null ? null : ProjectGame(game, playerId, room.Players.Select(p => p.Id).ToHashSet())
            };
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

    public void Start(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            RequireOwner(room, playerId);
            if (room.Players.Count < 2) Fail("At least two players are needed.");
            if (room.Game is not null)
            {
                if (!room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) Fail("Waiting for every player to reconnect.");
                var status = room.Game.StartHand();
                if (status != GameStartStatus.Started) Fail(status == GameStartStatus.NotEnoughPlayers ? "Not enough players with chips." : "A hand is already active.");
                room.Announce("A new hand started.");
                return;
            }
            room.StartRequested = true;
            room.Announce("Waiting for every player to connect.");
            TryStart(room);
        }
    }

    public void Connect(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            room.Connections[playerId] = room.Connections.GetValueOrDefault(playerId) + 1;
            TryStart(room);
        }
    }

    public void Disconnect(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var count = room.Connections.GetValueOrDefault(playerId);
            if (count > 1) room.Connections[playerId] = count - 1;
            else room.Connections.Remove(playerId);
        }
    }

    public void Act(Guid roomId, Guid playerId, ActionRequest request)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var game = ActiveGame(room);
            var outcome = game.ApplyAction(playerId, request.Action, request.Amount);
            if (!outcome.Success) Fail(outcome.Error!);
            room.Announce($"{GetPlayer(playerId).Nickname}: {request.Action.ToString().ToLowerInvariant()}.");
        }
    }

    private static GameSession ActiveGame(Room room)
    {
        if (!room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) Fail("Game paused until every player reconnects.");
        return room.Game ?? throw new ArenaException(409, "Game has not started.");
    }

    private static void TryStart(Room room)
    {
        if (!room.StartRequested || room.Game is not null || room.Players.Count < 2 ||
            !room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) return;
        room.Game = new GameSession(room.Id, room.Name, room.OwnerId, room.Settings.SmallBlind, room.Settings.BigBlind,
            room.Players.Select(p => (p.Id, p.Nickname)).ToList(), 2000);
        room.Game.StartHand();
        room.Announce("All players connected. The game has begun.");
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

    private static object ProjectGame(GameSession game, Guid viewerId, HashSet<Guid> members) => new
    {
        game.Phase,
        game.Pot,
        game.CurrentBet,
        game.MinRaise,
        CurrentTurnUserId = game.CurrentTurnIndex is { } turn ? game.Players[turn].UserId : (Guid?)null,
        Board = game.Board.Select(Card).ToArray(),
        Players = game.Players.Where(p => members.Contains(p.UserId)).Select(p => new
        {
            Id = p.UserId,
            Nickname = p.Username,
            p.SeatIndex,
            p.Chips,
            p.CurrentBet,
            p.HasFolded,
            p.IsAllIn,
            IsDealer = p.SeatIndex == game.DealerIndex,
            IsSmallBlind = p.SeatIndex == game.SmallBlindIndex,
            IsBigBlind = p.SeatIndex == game.BigBlindIndex,
            HoleCards = p.UserId == viewerId ? p.HoleCards.Select(Card).ToArray() : Array.Empty<string>(),
            CardCount = p.HoleCards.Count
        }).ToArray(),
        game.LastResult
    };

    private static string Card(Card card)
    {
        var rank = card.Rank switch { Rank.Ten => "10", Rank.Jack => "J", Rank.Queen => "Q", Rank.King => "K", Rank.Ace => "A", _ => ((int)card.Rank).ToString() };
        return rank + (card.Suit switch { Suit.Hearts => "♥", Suit.Diamonds => "♦", Suit.Clubs => "♣", _ => "♠" });
    }
}
