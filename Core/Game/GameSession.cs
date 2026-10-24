using Core.Domain;
using Core.Domain.Enums;

namespace Core.Game;

public sealed class GameSession
{
    private readonly TexasHoldemRules _rules = new();
    private readonly Lock _lock = new();
    private readonly HandEvaluator _handEvaluator = new();
    internal Deck? Deck;

    public Guid LobbyId { get; }
    public string LobbyName { get; internal set; }
    public Guid? OwnerUserId { get; internal set; }
    public int SmallBlind { get; private set; }
    public int BigBlind { get; private set; }
    public int Pot { get; set; }
    public int CurrentBet { get; internal set; }
    public int MinRaise { get; internal set; }
    public GamePhase Phase { get; internal set; } = GamePhase.WaitingForPlayers;
    public int DealerIndex { get; internal set; } = -1;
    public int SmallBlindIndex { get; internal set; } = -1;
    public int BigBlindIndex { get; internal set; } = -1;
    public int? CurrentTurnIndex { get; internal set; }
    public GameResult? LastResult { get; private set; }
    public List<GamePlayer> Players { get; } = new();
    public List<Card> Board { get; } = new();

    public GameSession(
        Guid lobbyId,
        string lobbyName,
        Guid? ownerUserId,
        int smallBlind,
        int bigBlind,
        List<(Guid Id, string Username)> players,
        int startingChips)
    {
        LobbyId = lobbyId;
        LobbyName = lobbyName;
        OwnerUserId = ownerUserId;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        SyncPlayers(players, startingChips);
    }


    private void SyncPlayers(
        IEnumerable<(Guid UserId, string Username)> players,
        int startingChips)
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

    private T WithLock<T>(Func<T> work)
    {
        lock (_lock)
        {
            return work();
        }
    }

    public void HandlePlayerLeft(Guid userId)
    {
        lock (_lock)
        {
            var playerIndex = Players.FindIndex(p => p.UserId == userId);
            if (playerIndex < 0)
            {
                return;
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
                return;
            }

            if (TryFinishHand())
            {
                return;
            }

            if (TryAdvanceRound())
            {
                return;
            }

            if (CurrentTurnIndex.HasValue && CurrentTurnIndex.Value < Players.Count && Players[CurrentTurnIndex.Value].UserId == userId)
            {
                CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(CurrentTurnIndex.Value, p => p.CanAct));
            }
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

    private void ResetTableForNextHand()
    {
        Phase = GamePhase.WaitingForPlayers;
        Pot = 0;
        CurrentBet = 0;
        MinRaise = BigBlind;
        Board.Clear();
        CurrentTurnIndex = null;
        Deck = null;
        SmallBlindIndex = -1;
        BigBlindIndex = -1;

        foreach (var player in Players)
        {
            player.ResetForHand();
        }
    }

    public GameStartStatus StartHand()
        => WithLock(() => TexasHoldemRules.StartHand(this));

    public GameActionResult ApplyAction(Guid userId, PlayerAction action, int amount)
        => WithLock(() => TexasHoldemRules.ApplyAction(this, userId, action, amount));

    internal void DealHoleCards(Deck deck, int cardsPerPlayer)
    {
        var dealOrder = new List<GamePlayer>();
        var startIndex = SmallBlindIndex >= 0 ? SmallBlindIndex : 0;
        var index = startIndex;
        foreach (var t in Players)
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
        if (bettingPlayers.Count <= 1 && activePlayers.Count > 1
            && bettingPlayers.All(p => p.CurrentBet == CurrentBet))
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

        }

        CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(DealerIndex, player => player.CanAct));
        return true;
    }

    private void DealBoardCards(int count)
    {
        if (Deck is null)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            Board.Add(Deck.Draw());
        }
    }

    internal void DealRemainingBoard()
    {
        if (Deck is null)
        {
            return;
        }

        while (Board.Count < 5)
        {
            Board.Add(Deck.Draw());
        }
    }

    public void ResolveShowdown(List<GamePlayer> activePlayers)
    {
        // Ranking 1 is strongest. Each contribution level forms a separate pot;
        // folded chips stay in pots but folded players cannot win them.
        var winnings = new Dictionary<Guid, int>();
        var levels = Players.Select(p => p.TotalCommitted).Where(n => n > 0).Distinct().Order().ToList();
        var previous = 0;
        foreach (var level in levels)
        {
            var contributors = Players.Where(p => p.TotalCommitted >= level).ToList();
            var sidePot = (level - previous) * contributors.Count;
            previous = level;
            var eligible = activePlayers.Where(p => p.TotalCommitted >= level).ToList();
            // A sole uncalled contribution is returned even if its owner left.
            if (eligible.Count == 0)
            {
                // No live hand matched this portion: return the uncalled chips.
                foreach (var contributor in contributors)
                {
                    var refund = sidePot / contributors.Count;
                    contributor.Chips += refund;
                    winnings[contributor.UserId] = winnings.GetValueOrDefault(contributor.UserId) + refund;
                }
                continue;
            }
            var scores = eligible.Select(p => (Player: p, Score: _handEvaluator.EvaluateScore(p.HoleCards, Board))).ToList();
            var best = scores.Min(p => p.Score);
            var winners = scores.Where(p => p.Score == best).Select(p => p.Player)
                .OrderBy(p => (p.SeatIndex - DealerIndex - 1 + Players.Count) % Players.Count).ToList();
            for (var i = 0; i < winners.Count; i++)
            {
                var payout = sidePot / winners.Count + (i < sidePot % winners.Count ? 1 : 0);
                winners[i].Chips += payout;
                winnings[winners[i].UserId] = winnings.GetValueOrDefault(winners[i].UserId) + payout;
            }
        }
        var bestPlayer = activePlayers.MinBy(p => _handEvaluator.EvaluateScore(p.HoleCards, Board));
        LastResult = new GameResult(Pot,
            bestPlayer is null ? "" : _handEvaluator.GetHandDescription(bestPlayer.HoleCards, Board),
            winnings.Select(w => new GameWinner(w.Key, Players.First(p => p.UserId == w.Key).Username, w.Value)).ToList());
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
        foreach (var t in Players)
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

public sealed class GamePlayer(Guid userId, string username, int seatIndex, int chips)
{
    public Guid UserId { get; } = userId;
    public string Username { get; set; } = username;
    public int SeatIndex { get; set; } = seatIndex;
    public int Chips { get; set; } = chips;
    public int CurrentBet { get; set; }
    public bool HasFolded { get; set; }
    public bool IsAllIn { get; set; }
    public bool HasActedThisRound { get; set; }
    public int TotalCommitted { get; private set; }
    public List<Card> HoleCards { get; } = new();

    public bool IsInHand => !HasFolded && (Chips > 0 || IsAllIn);

    public bool CanAct => !HasFolded && !IsAllIn && Chips > 0;

    public void ResetForHand()
    {
        CurrentBet = 0;
        TotalCommitted = 0;
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
        TotalCommitted += bet;
        if (Chips == 0)
        {
            IsAllIn = true;
        }

        return bet;
    }
}

public sealed record GameResult(int Pot, string HandDescription, IReadOnlyList<GameWinner> Winners);

public sealed record GameWinner(Guid UserId, string Username, int Winnings);
