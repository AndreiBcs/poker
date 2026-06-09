using Source.Domain.Enums;
using Source.Domain.GameLogic;
using Source.Domain.ValueObjects;

namespace Source.Features.Game;

public sealed class GameSession
{
    private readonly object _lock = new();
    private readonly HandEvaluator _handEvaluator = new();
    private Deck? _deck;

    public Guid LobbyId { get; }
    public string LobbyName { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public int SmallBlind { get; private set; }
    public int BigBlind { get; private set; }
    public int Pot { get; private set; }
    public int CurrentBet { get; private set; }
    public int MinRaise { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.WaitingForPlayers;
    public int DealerIndex { get; private set; } = -1;
    public int SmallBlindIndex { get; private set; } = -1;
    public int BigBlindIndex { get; private set; } = -1;
    public int? CurrentTurnIndex { get; private set; }
    public GameResult? LastResult { get; private set; }

    public List<GamePlayer> Players { get; } = new();
    public List<Card> Board { get; } = new();

    public GameSession(Guid lobbyId, string lobbyName, Guid? ownerUserId, int smallBlind, int bigBlind, IEnumerable<(Guid UserId, string Username)> players, int startingChips)
    {
        LobbyId = lobbyId;
        LobbyName = lobbyName;
        OwnerUserId = ownerUserId;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        SyncPlayers(players, startingChips);
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
            Phase = GamePhase.WaitingForPlayers;
            Pot = 0;
            CurrentBet = 0;
            MinRaise = BigBlind;
            Board.Clear();
            CurrentTurnIndex = null;
            LastResult = null;

            foreach (var player in Players)
            {
                player.CurrentBet = 0;
                player.HasFolded = false;
                player.IsAllIn = false;
                player.HasActedThisRound = false;
                player.HoleCards.Clear();
            }
        }
    }

    public GameStartStatus StartHand()
    {
        lock (_lock)
        {
            if (Phase is GamePhase.PreFlop or GamePhase.Flop or GamePhase.Turn or GamePhase.River)
            {
                return GameStartStatus.AlreadyInProgress;
            }

            if (Players.Count(p => p.Chips > 0) < 2)
            {
                Phase = GamePhase.WaitingForPlayers;
                return GameStartStatus.NotEnoughPlayers;
            }

            LastResult = null;
            Pot = 0;
            CurrentBet = 0;
            MinRaise = BigBlind;
            Board.Clear();

            foreach (var player in Players)
            {
                player.ResetForHand();
            }

            var deck = new Deck();
            _deck = deck;
            DealerIndex = FindNextIndex(DealerIndex, player => player.Chips > 0);
            var activePlayers = Players.Count(player => player.Chips > 0);
            if (activePlayers == 2)
            {
                SmallBlindIndex = DealerIndex;
                BigBlindIndex = FindNextIndex(DealerIndex, player => player.Chips > 0);
            }
            else
            {
                SmallBlindIndex = FindNextIndex(DealerIndex, player => player.Chips > 0);
                BigBlindIndex = FindNextIndex(SmallBlindIndex, player => player.Chips > 0);
            }

            DealHoleCards(deck);
            PostBlind(Players[SmallBlindIndex], SmallBlind);
            var bigBlindAmount = PostBlind(Players[BigBlindIndex], BigBlind);
            CurrentBet = bigBlindAmount;
            MinRaise = BigBlind;

            Phase = GamePhase.PreFlop;
            CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(BigBlindIndex, player => player.CanAct));
            if (CurrentTurnIndex is null)
            {
                DealRemainingBoard();
                ResolveShowdown(Players.Where(p => p.IsInHand).ToList());
            }

            return GameStartStatus.Started;
        }
    }

    public GameActionResult ApplyAction(Guid userId, PlayerAction action, int amount)
    {
        lock (_lock)
        {
            if (Phase is GamePhase.WaitingForPlayers or GamePhase.Showdown)
            {
                return new GameActionResult(false, "The hand is not active.");
            }

            var playerIndex = Players.FindIndex(p => p.UserId == userId);
            if (playerIndex < 0)
            {
                return new GameActionResult(false, "Player not found.");
            }

            if (CurrentTurnIndex != playerIndex)
            {
                return new GameActionResult(false, "It is not your turn.");
            }

            var player = Players[playerIndex];
            if (!player.CanAct)
            {
                return new GameActionResult(false, "Player cannot act.");
            }

            switch (action)
            {
                case PlayerAction.Fold:
                    player.HasFolded = true;
                    player.HasActedThisRound = true;
                    break;
                case PlayerAction.Check:
                    if (player.CurrentBet != CurrentBet)
                    {
                        return new GameActionResult(false, "Cannot check when there is a bet to call.");
                    }

                    player.HasActedThisRound = true;
                    break;
                case PlayerAction.Call:
                    var toCall = CurrentBet - player.CurrentBet;
                    if (toCall <= 0)
                    {
                        player.HasActedThisRound = true;
                        break;
                    }

                    var called = player.Bet(toCall);
                    Pot += called;
                    player.HasActedThisRound = true;
                    break;
                case PlayerAction.Raise:
                    if (amount <= 0)
                    {
                        return new GameActionResult(false, "Raise amount must be positive.");
                    }

                    if (amount < MinRaise)
                    {
                        return new GameActionResult(false, $"Minimum raise is {MinRaise}.");
                    }

                    var targetBet = CurrentBet + amount;
                    var needed = targetBet - player.CurrentBet;
                    if (needed <= 0)
                    {
                        return new GameActionResult(false, "Raise amount is too small.");
                    }

                    if (needed > player.Chips)
                    {
                        return new GameActionResult(false, "Not enough chips to raise.");
                    }

                    var raised = player.Bet(needed);
                    Pot += raised;
                    CurrentBet = player.CurrentBet;
                    MinRaise = amount;
                    player.HasActedThisRound = true;
                    foreach (var other in Players)
                    {
                        if (other.UserId != player.UserId && other.CanAct)
                        {
                            other.HasActedThisRound = false;
                        }
                    }

                    break;
                default:
                    return new GameActionResult(false, "Unknown action.");
            }

            if (TryFinishHand())
            {
                return new GameActionResult(true, null);
            }

            if (TryAdvanceRound())
            {
                return new GameActionResult(true, null);
            }

            CurrentTurnIndex = NormalizeTurnIndex(FindNextIndex(playerIndex, p => p.CanAct));
            return new GameActionResult(true, null);
        }
    }

    private void DealHoleCards(Deck deck)
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

        for (var round = 0; round < 2; round++)
        {
            foreach (var player in dealOrder)
            {
                player.HoleCards.Add(deck.Draw());
            }
        }
    }

    private int PostBlind(GamePlayer player, int blind)
    {
        var posted = player.Bet(blind);
        Pot += posted;
        return posted;
    }

    private bool TryFinishHand()
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
            Pot = 0;
            Phase = GamePhase.Showdown;
            CurrentTurnIndex = null;
            return true;
        }

        var bettingPlayers = Players.Where(p => p.CanAct).ToList();
        if (bettingPlayers.Count == 0 && activePlayers.Count > 1)
        {
            DealRemainingBoard();
            ResolveShowdown(activePlayers);
            return true;
        }

        return false;
    }

    private bool TryAdvanceRound()
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
        if (_deck is null)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            Board.Add(_deck.Draw());
        }
    }

    private void DealRemainingBoard()
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

    private void ResolveShowdown(List<GamePlayer> activePlayers)
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
        Pot = 0;
        Phase = GamePhase.Showdown;
        CurrentTurnIndex = null;
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

    private int FindNextIndex(int startIndex, Func<GamePlayer, bool> predicate)
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

    private int? NormalizeTurnIndex(int index)
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
