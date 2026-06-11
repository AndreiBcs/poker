using Source.Domain.Enums;
using Source.Domain.GameLogic;
using Source.Domain.ValueObjects;
using Source.Features.Game.Rules;

namespace Source.Features.Game;

public sealed class GameSession
{
    private readonly IGameModeRules _rules;
    private readonly object _lock = new();
    private readonly HandEvaluator _handEvaluator = new();
   internal Deck? _deck;

    public Guid LobbyId { get; }
public string LobbyName { get; internal set; }
    public Guid? OwnerUserId { get; internal set; }
    public int SmallBlind { get; internal set; }
    public int BigBlind { get; internal set; }
    public int Pot { get; internal set; }
    public int CurrentBet { get; internal set; }
    public int MinRaise { get; internal set; }
    public GamePhase Phase { get; internal set; } = GamePhase.WaitingForPlayers;
    public int DealerIndex { get; internal set; } = -1;
    public int SmallBlindIndex { get; internal set; } = -1;
    public int BigBlindIndex { get; internal set; } = -1;
    public int? CurrentTurnIndex { get; internal set; }
    public GameResult? LastResult { get; internal set; }
    public List<GamePlayer> Players { get; } = new();
    public List<Card> Board { get; } = new();

    public GameSession(
        Guid lobbyId, 
        string lobbyName, 
        Guid? ownerUserId, 
        int smallBlind, 
        int bigBlind, 
        List<(Guid Id, string Username)> players, 
        int startingChips,
        GameType gameType)
    {
        LobbyId = lobbyId;
        LobbyName = lobbyName;
        OwnerUserId = ownerUserId;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        SyncPlayers(players, startingChips);
        _rules = gameType switch
        {
            GameType.TexasHoldem => new TexasHoldemRules(),
            GameType.FiveCardDraw => new FiveCardDrawRules(),
            _ => throw new NotSupportedException($"Game type {gameType} is not supported.")
        };
        
    }
    

    public void SyncPlayers(IEnumerable<(Guid UserId, string Username)> players, int startingChips)
    {
        lock (_lock)
        {
            var previousDealerId = DealerIndex >= 0 && DealerIndex < Players.Count
                ? Players[DealerIndex].UserId
                : (Guid?)null;

            var existingPlayers = Players.ToDictionary(player => player.UserId);

            var playerList = players.ToList();
            Players.Clear();
            for (var i = 0; i < playerList.Count; i++)
            {
                var entry = playerList[i];
                if (!existingPlayers.TryGetValue(entry.UserId, out var existing))
                {
                    Players.Add(new GamePlayer(entry.UserId, entry.Username, i, startingChips));
                }
                else
                {
                    existing.SeatIndex = i;
                    existing.Username = entry.Username;
                    Players.Add(existing);
                }
            }

            if (previousDealerId.HasValue)
            {
                DealerIndex = Players.FindIndex(p => p.UserId == previousDealerId.Value);
            }
            else
            {
                DealerIndex = -1;
            }
        }
    }

    public void UpdateBlinds(int smallBlind, int bigBlind)
    {
        lock (_lock)
        {
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
        }
    }

    public void UpdateLobbyInfo(string lobbyName, Guid? ownerUserId)
    {
        lock (_lock)
        {
            LobbyName = lobbyName;
            OwnerUserId = ownerUserId;
        }
    }

    public T WithLock<T>(Func<T> work)
    {
        lock (_lock)
        {
            return work();
        }
    }

    public void UpdatePlayerConnection(Guid userId, string? ipAddress)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.UserId == userId);
            if (player is not null)
            {
                player.IpAddress = ipAddress;
            }
        }
    }

    public bool HandlePlayerLeft(Guid userId)
    {
        lock (_lock)
        {
            var playerIndex = Players.FindIndex(p => p.UserId == userId);
            if (playerIndex < 0)
            {
                return false;
            }

            var player = Players[playerIndex];
            player.HasFolded = true;
            player.IsAllIn = false;
            player.HasActedThisRound = true;
            player.Chips = 0;
            player.CurrentBet = 0;
            player.HoleCards.Clear();

            if (Phase is GamePhase.WaitingForPlayers or GamePhase.Showdown)
            {
                return true;
            }

            if (TryFinishHand())
            {
                return true;
            }

            if (TryAdvanceRound())
            {
                return true;
            }

            if (CurrentTurnIndex.HasValue && CurrentTurnIndex.Value < Players.Count && Players[CurrentTurnIndex.Value].UserId == userId)
            {
                CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(CurrentTurnIndex.Value, p => p.CanAct));
            }

            return true;
        }
    }

    public bool HasEnoughPlayers()
    {
        lock (_lock)
        {
            return Players.Count(player => player.Chips > 0) >= 2;
        }
    }

    public void ResetToWaiting()
    {
        lock (_lock)
        {
            ResetTableForNextHand();
            DealerIndex = -1;
        }
    }

    internal void PrepareForNewHand()
    {
        ResetTableForNextHand();
        LastResult = null;
    }

    internal void ResetTableForNextHand()
    {
        Phase = GamePhase.WaitingForPlayers;
        Pot = 0;
        CurrentBet = 0;
        MinRaise = BigBlind;
        Board.Clear();
        CurrentTurnIndex = null;
        _deck = null;
        SmallBlindIndex = -1;
        BigBlindIndex = -1;

        foreach (var player in Players)
        {
            player.ResetForHand();
        }
    }

public GameStartStatus StartHand() 
    => WithLock(() => _rules.StartHand(this));

  public GameActionResult ApplyAction(Guid userId, PlayerAction action, int amount) 
    => WithLock(() => _rules.ApplyAction(this, userId, action, amount));

    internal void DealHoleCards(Deck deck, int cardsPerPlayer)
    {
        var dealOrder = new List<GamePlayer>();
        var startIndex = SmallBlindIndex >= 0 ? SmallBlindIndex : 0;
        var index = startIndex;
        for (var i = 0; i < Players.Count; i++)
        {
            var player = Players[index];
            if (player.Chips > 0)
            {
                dealOrder.Add(player);
            }

            index = (index + 1) % Players.Count;
        }

        for (var round = 0; round < cardsPerPlayer; round++)
        {
            foreach (var player in dealOrder)
            {
                player.HoleCards.Add(deck.Draw());
            }
        }
    }

    internal int PostBlind(GamePlayer player, int blind)
    {
        var posted = player.Bet(blind);
        Pot += posted;
        return posted;
    }

    internal bool TryFinishHand()
    {
        var activePlayers = Players.Where(p => p.IsInHand).ToList();
        if (activePlayers.Count == 1)
        {
            var winner = activePlayers[0];
            winner.Chips += Pot;
            LastResult = new GameResult(Pot, "All others folded.", new List<GameWinner>
            {
                new(winner.UserId, winner.Username, Pot)
            });
            ResetTableForNextHand();
            return true;
        }

        var bettingPlayers = Players.Where(p => p.CanAct).ToList();
        if (bettingPlayers.Count == 0 && activePlayers.Count > 1)
        {
            if (Phase is GamePhase.PreFlop or GamePhase.Flop or GamePhase.Turn or GamePhase.River)
            {
                DealRemainingBoard();
            }

            ResolveShowdown(activePlayers);
            return true;
        }

        return false;
    }

    internal bool TryAdvanceRound()
    {
        if (!IsBettingRoundComplete())
        {
            return false;
        }

        foreach (var player in Players)
        {
            player.CurrentBet = 0;
            player.HasActedThisRound = false;
        }

        CurrentBet = 0;
        MinRaise = BigBlind;

        switch (Phase)
        {
            case GamePhase.PreFlop:
                DealBoardCards(3);
                Phase = GamePhase.Flop;
                break;
            case GamePhase.Flop:
                DealBoardCards(1);
                Phase = GamePhase.Turn;
                break;
            case GamePhase.Turn:
                DealBoardCards(1);
                Phase = GamePhase.River;
                break;
            case GamePhase.River:
                ResolveShowdown(Players.Where(p => p.IsInHand).ToList());
                return true;
            case GamePhase.InitialDeal:
                Phase = GamePhase.DrawCards;
                break;
            case GamePhase.DrawCards:
                Phase = GamePhase.SecondBetting;
                break;
            case GamePhase.SecondBetting:
                ResolveShowdown(Players.Where(p => p.IsInHand).ToList());
                return true;
        }

        CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(DealerIndex, player => player.CanAct));
        return true;
    }

    internal void DealBoardCards(int count)
    {
        if (_deck is null)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            Board.Add(_deck.Draw());
        }
    }

    internal void DealRemainingBoard()
    {
        if (_deck is null)
        {
            return;
        }

        while (Board.Count < 5)
        {
            Board.Add(_deck.Draw());
        }
    }

    internal void ResolveShowdown(List<GamePlayer> activePlayers)
    {
        var bestScore = int.MinValue;
        var winners = new List<GamePlayer>();
        foreach (var player in activePlayers)
        {
            var score = _handEvaluator.EvaluateScore(player.HoleCards, Board);
            if (score > bestScore)
            {
                bestScore = score;
                winners.Clear();
                winners.Add(player);
            }
            else if (score == bestScore)
            {
                winners.Add(player);
            }
        }

        var potShare = winners.Count > 0 ? Pot / winners.Count : 0;
        var remainder = winners.Count > 0 ? Pot % winners.Count : 0;
        var winnerList = new List<GameWinner>();
        for (var i = 0; i < winners.Count; i++)
        {
            var payout = potShare + (i == 0 ? remainder : 0);
            winners[i].Chips += payout;
            winnerList.Add(new GameWinner(winners[i].UserId, winners[i].Username, payout));
        }

        var description = winners.Count > 0
            ? _handEvaluator.GetHandDescription(winners[0].HoleCards, Board)
            : "";

        LastResult = new GameResult(Pot, description, winnerList);
        ResetTableForNextHand();
    }

    private bool IsBettingRoundComplete()
    {
        var bettingPlayers = Players.Where(p => p.CanAct).ToList();
        if (bettingPlayers.Count == 0)
        {
            return true;
        }

        return bettingPlayers.All(player => player.HasActedThisRound && player.CurrentBet == CurrentBet);
    }

    internal int FindNextIndex(int startIndex, Func<GamePlayer, bool> predicate)
    {
        if (Players.Count == 0)
        {
            return -1;
        }

        var index = startIndex;
        for (var i = 0; i < Players.Count; i++)
        {
            index = (index + 1) % Players.Count;
            if (predicate(Players[index]))
            {
                return index;
            }
        }

        return -1;
    }

    internal int? NormalizeTurnIndex(int index)
        => index >= 0 ? index : null;
}

public sealed class GamePlayer
{
    public GamePlayer(Guid userId, string username, int seatIndex, int chips)
    {
        UserId = userId;
        Username = username;
        SeatIndex = seatIndex;
        Chips = chips;
    }

    public Guid UserId { get; }
    public string Username { get; set; }
    public int SeatIndex { get; set; }
    public int Chips { get; set; }
    public int CurrentBet { get; set; }
    public bool HasFolded { get; set; }
    public bool IsAllIn { get; set; }
    public bool HasActedThisRound { get; set; }
    public string? IpAddress { get; set; }
    public List<Card> HoleCards { get; } = new();

    public bool IsInHand => !HasFolded && (Chips > 0 || IsAllIn);

    public bool CanAct => !HasFolded && !IsAllIn && Chips > 0;

    public void ResetForHand()
    {
        CurrentBet = 0;
        HasFolded = false;
        IsAllIn = false;
        HasActedThisRound = false;
        HoleCards.Clear();
    }

    public int Bet(int amount)
    {
        if (amount <= 0 || Chips <= 0)
        {
            return 0;
        }

        var bet = Math.Min(amount, Chips);
        Chips -= bet;
        CurrentBet += bet;
        if (Chips == 0)
        {
            IsAllIn = true;
        }

        return bet;
    }
}

public sealed record GameResult(int Pot, string HandDescription, IReadOnlyList<GameWinner> Winners);

public sealed record GameWinner(Guid UserId, string Username, int Winnings);
