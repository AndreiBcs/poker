using System;
using System.Linq;
using PokerArena.Core.Domain.Enums;
using PokerArena.Core.Domain.ValueObjects;
using PokerArena.Core.Game;
using Xunit;

namespace PokerArena.Core.Tests;

public class GameSessionTests
{
    private static GameSession Table(int count = 2) => new(Guid.NewGuid(), "Table", null, 25, 50,
        Enumerable.Range(0, count).Select(n => (Guid.NewGuid(), $"Player {n}")).ToList(), 2000);

    [Fact]
    public void StartDealsUniquePrivateCardsAndPostsHeadsUpBlinds()
    {
        var game = Table();
        Assert.Equal(GameStartStatus.Started, game.StartHand());
        Assert.Equal(75, game.Pot);
        Assert.Equal(game.DealerIndex, game.SmallBlindIndex);
        Assert.Equal(game.SmallBlindIndex, game.CurrentTurnIndex);
        Assert.Equal(4, game.Players.SelectMany(p => p.HoleCards).Distinct().Count());
        Assert.Equal(4000, game.Players.Sum(p => p.Chips) + game.Pot);
        Assert.Equal(GameStartStatus.AlreadyInProgress, game.StartHand());
    }

    [Fact]
    public void BettingRejectsWrongTurnAndInvalidCheckAndFoldAwardsPot()
    {
        var game = Table(); game.StartHand();
        var current = game.Players[game.CurrentTurnIndex!.Value];
        var other = game.Players.Single(p => p != current);
        Assert.False(game.ApplyAction(other.UserId, PlayerAction.Fold, 0).Success);
        Assert.False(game.ApplyAction(current.UserId, PlayerAction.Check, 0).Success);
        Assert.False(game.ApplyAction(current.UserId, PlayerAction.Raise, int.MaxValue).Success);
        Assert.True(game.ApplyAction(current.UserId, PlayerAction.Fold, 0).Success);
        Assert.Equal(GamePhase.WaitingForPlayers, game.Phase);
        Assert.Equal(other.UserId, Assert.Single(game.LastResult!.Winners).UserId);
        Assert.Equal(4000, game.Players.Sum(p => p.Chips));
        Assert.Equal(GameStartStatus.Started, game.StartHand());
        Assert.Equal(1, game.DealerIndex);
    }

    [Fact]
    public void CallsAndChecksProgressToRiverAndConserveChipsAtTenSeatTable()
    {
        var game = Table(10); game.StartHand();
        for (var action = 0; game.Phase != GamePhase.WaitingForPlayers && action < 100; action++)
        {
            var player = game.Players[game.CurrentTurnIndex!.Value];
            Assert.True(game.ApplyAction(player.UserId, player.CurrentBet < game.CurrentBet ? PlayerAction.Call : PlayerAction.Check, 0).Success);
        }
        Assert.Equal(GamePhase.WaitingForPlayers, game.Phase);
        Assert.NotNull(game.LastResult);
        Assert.Equal(20000, game.Players.Sum(p => p.Chips));
    }

    [Fact]
    public void ShowdownUsesLowerRankingAndSplitsSidePotsByContribution()
    {
        var game = Table(3);
        // Fixed hands: aces > kings > queens; a short stack can only win the main pot.
        var ranks = new[] { Rank.Ace, Rank.King, Rank.Queen };
        var contributions = new[] { 100, 300, 300 };
        for (var i = 0; i < 3; i++)
        {
            game.Players[i].Bet(contributions[i]);
            game.Players[i].HoleCards.AddRange(new[] { new Card(Suit.Spades, ranks[i]), new Card(Suit.Hearts, ranks[i]) });
        }
        game.Board.AddRange(new[] { new Card(Suit.Clubs, Rank.Two), new Card(Suit.Diamonds, Rank.Four),
            new Card(Suit.Clubs, Rank.Six), new Card(Suit.Diamonds, Rank.Eight), new Card(Suit.Clubs, Rank.Ten) });
        game.Pot = 700;
        game.ResolveShowdown(game.Players.ToList());
        Assert.Equal(2200, game.Players[0].Chips);
        Assert.Equal(2100, game.Players[1].Chips);
        Assert.Equal(1700, game.Players[2].Chips);
        Assert.Equal(6000, game.Players.Sum(p => p.Chips));
    }
    [Fact]
    public void ShortBigBlindDoesNotReduceTheBetBelowSmallBlind()
    {
        var game = Table();
        game.Players[1].Chips = 10;
        game.StartHand();
        Assert.Equal(25, game.CurrentBet);
        Assert.True(game.ApplyAction(game.Players[0].UserId, PlayerAction.Check, 0).Success);
        Assert.Equal(GamePhase.WaitingForPlayers, game.Phase);
        Assert.Equal(2010, game.Players.Sum(p => p.Chips));
    }

    [Fact]
    public void UnequalAllInCallsRunOutBoardAndConserveChips()
    {
        var game = Table(3);
        game.Players[0].Chips = 100;
        game.Players[1].Chips = 300;
        game.Players[2].Chips = 500;
        game.StartHand();
        Assert.True(game.ApplyAction(game.Players[0].UserId, PlayerAction.Call, 0).Success);
        Assert.True(game.ApplyAction(game.Players[1].UserId, PlayerAction.Raise, 250).Success);
        Assert.True(game.ApplyAction(game.Players[2].UserId, PlayerAction.Call, 0).Success);
        Assert.True(game.ApplyAction(game.Players[0].UserId, PlayerAction.Call, 0).Success);
        Assert.Equal(GamePhase.WaitingForPlayers, game.Phase);
        Assert.NotNull(game.LastResult);
        Assert.Equal(900, game.Players.Sum(p => p.Chips));
    }

}
