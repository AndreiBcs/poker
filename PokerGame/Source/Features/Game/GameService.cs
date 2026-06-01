using Microsoft.EntityFrameworkCore;
using Source.Domain.Enums;
using Source.Domain.ValueObjects;
using Source.Infrastructure.Persistence;

namespace Source.Features.Game;

public sealed class GameService
{
    private const int StartingChips = 2000;

    private readonly AppDbContext _db;
    private readonly GameStateStore _store;
    private readonly GameConnectionManager _connections;

    public GameService(AppDbContext db, GameStateStore store, GameConnectionManager connections)
    {
        _db = db;
        _store = store;
        _connections = connections;
    }

    public async Task<GameStartOutcome> StartGameAsync(Guid lobbyId, Guid userId)
    {
        var lobby = await _db.Lobbies.Include(lobby => lobby.Players)
            .FirstOrDefaultAsync(lobby => lobby.Id == lobbyId);
        if (lobby is null)
        {
            return new GameStartOutcome(GameStartStatus.LobbyNotFound, "Lobby not found.");
        }

        if (!lobby.Players.Any(player => player.Id == userId))
        {
            return new GameStartOutcome(GameStartStatus.NotInLobby, "You are not in this lobby.");
        }

        if (lobby.OwnerUserId != userId)
        {
            return new GameStartOutcome(GameStartStatus.NotLeader, "Only the lobby leader can start the game.");
        }

        var session = EnsureSession(lobby);
        var result = session.StartHand();
        if (result == GameStartStatus.NotEnoughPlayers)
        {
            return new GameStartOutcome(result, "Not enough players to start.");
        }

        if (result == GameStartStatus.AlreadyInProgress)
        {
            return new GameStartOutcome(result, "A hand is already in progress.");
        }

        await BroadcastStateAsync(lobbyId);
        return new GameStartOutcome(result, null);
    }

    public async Task<GameActionResult> ApplyActionAsync(Guid lobbyId, Guid userId, PlayerAction action, int amount)
    {
        var session = await EnsureSessionAsync(lobbyId);
        if (session is null)
        {
            return new GameActionResult(false, "Lobby not found.");
        }

        var result = session.ApplyAction(userId, action, amount);
        if (result.Success)
        {
            await BroadcastStateAsync(lobbyId);
        }

        return result;
    }

    public async Task<bool> IsUserInLobbyAsync(Guid lobbyId, Guid userId)
        => await _db.Users.AnyAsync(user => user.Id == userId && user.LobbyId == lobbyId);

    public async Task<GameStateDto?> GetStateAsync(Guid lobbyId, Guid userId)
    {
        var session = await EnsureSessionAsync(lobbyId);
        if (session is null)
        {
            return null;
        }

        return BuildState(session, userId);
    }

    public async Task RegisterConnectionAsync(Guid lobbyId, Guid userId, string? ipAddress)
    {
        var session = await EnsureSessionAsync(lobbyId);
        if (session is null)
        {
            return;
        }

        session.UpdatePlayerConnection(userId, ipAddress);
        await BroadcastStateAsync(lobbyId);
    }

    public async Task UnregisterConnectionAsync(Guid lobbyId, Guid userId)
    {
        if (_store.TryGet(lobbyId, out var session))
        {
            session.UpdatePlayerConnection(userId, null);
        }

        await BroadcastStateAsync(lobbyId);
    }

    public async Task BroadcastStateAsync(Guid lobbyId)
    {
        if (!_store.TryGet(lobbyId, out var session))
        {
            return;
        }

        await _connections.BroadcastAsync(lobbyId, userId =>
        {
            var state = BuildState(session, userId);
            return new { type = "state", payload = state };
        });
    }

    private GameSession EnsureSession(Domain.Entities.Lobby lobby)
    {
        var players = lobby.Players
            .OrderBy(player => player.Username)
            .ThenBy(player => player.Id)
            .Select(player => (player.Id, player.Username))
            .ToList();

        var session = _store.GetOrAdd(lobby.Id, () => new GameSession(
            lobby.Id,
            lobby.SmallBlind,
            lobby.BigBlind,
            players,
            StartingChips));

        if (session.Phase is GamePhase.WaitingForPlayers or GamePhase.Showdown)
        {
            session.UpdateBlinds(lobby.SmallBlind, lobby.BigBlind);
            session.SyncPlayers(players, StartingChips);
        }

        var ipMap = _connections.GetIpAddresses(lobby.Id);
        foreach (var (userId, ipAddress) in ipMap)
        {
            session.UpdatePlayerConnection(userId, ipAddress);
        }

        return session;
    }

    private async Task<GameSession?> EnsureSessionAsync(Guid lobbyId)
    {
        var lobby = await _db.Lobbies.Include(lobby => lobby.Players)
            .FirstOrDefaultAsync(lobby => lobby.Id == lobbyId);
        return lobby is null ? null : EnsureSession(lobby);
    }

    private GameStateDto BuildState(GameSession session, Guid viewerId)
    {
        var ipMap = _connections.GetIpAddresses(session.LobbyId);
        var dealerUserId = session.DealerIndex >= 0 && session.DealerIndex < session.Players.Count
            ? session.Players[session.DealerIndex].UserId
            : (Guid?)null;
        var smallBlindUserId = session.SmallBlindIndex >= 0 && session.SmallBlindIndex < session.Players.Count
            ? session.Players[session.SmallBlindIndex].UserId
            : (Guid?)null;
        var bigBlindUserId = session.BigBlindIndex >= 0 && session.BigBlindIndex < session.Players.Count
            ? session.Players[session.BigBlindIndex].UserId
            : (Guid?)null;
        var currentTurnUserId = session.CurrentTurnIndex.HasValue && session.CurrentTurnIndex.Value < session.Players.Count
            ? session.Players[session.CurrentTurnIndex.Value].UserId
            : (Guid?)null;

        var players = session.Players.Select(player =>
        {
            var revealCards = player.UserId == viewerId || session.Phase == GamePhase.Showdown;
            var holeCards = revealCards
                ? player.HoleCards.Select(MapCard).ToList()
                : new List<CardDto>();

            return new GamePlayerStateDto(
                player.UserId,
                player.Username,
                player.SeatIndex,
                player.Chips,
                player.CurrentBet,
                player.HasFolded,
                player.IsAllIn,
                player.UserId == dealerUserId,
                player.UserId == smallBlindUserId,
                player.UserId == bigBlindUserId,
                ipMap.ContainsKey(player.UserId),
                ipMap.TryGetValue(player.UserId, out var ipAddress) ? ipAddress : null,
                holeCards);
        }).ToList();

        var board = session.Board.Select(MapCard).ToList();
        var result = session.LastResult is null
            ? null
            : new GameResultDto(
                session.LastResult.Pot,
                session.LastResult.HandDescription,
                session.LastResult.Winners.Select(winner => new GameWinnerDto(
                    winner.UserId,
                    winner.Username,
                    winner.Winnings)).ToList());

        return new GameStateDto(
            session.LobbyId,
            session.Phase,
            session.Pot,
            session.CurrentBet,
            session.MinRaise,
            currentTurnUserId,
            dealerUserId,
            smallBlindUserId,
            bigBlindUserId,
            board,
            players,
            result,
            viewerId);
    }

    private static CardDto MapCard(Card card)
    {
        var rankLabel = card.Rank switch
        {
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)card.Rank).ToString()
        };

        var rankCode = card.Rank switch
        {
            Rank.Ten => "T",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)card.Rank).ToString()
        };

        var suitLabel = card.Suit.ToString();
        var suitCode = card.Suit switch
        {
            Suit.Hearts => "H",
            Suit.Diamonds => "D",
            Suit.Clubs => "C",
            Suit.Spades => "S",
            _ => ""
        };

        return new CardDto(rankLabel, suitLabel, $"{rankCode}{suitCode}");
    }
}
