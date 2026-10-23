using System.Collections.Generic;
using PokerArena.Core.Domain.Enums;
using PokerArena.Core.Domain.GameLogic;
using PokerArena.Core.Domain.ValueObjects;
using Xunit;

namespace PokerArena.Core.Domain.Tests;

public class HandEvaluatorTests
{
    [Fact]
    public void GetHandCategory_ReturnsValue_ForValidHand()
    {
        var evaluator = new HandEvaluator();
        var holeCards = new List<Card>
        {
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Spades, Rank.King)
        };
        var board = new List<Card>
        {
            new Card(Suit.Spades, Rank.Queen),
            new Card(Suit.Spades, Rank.Jack),
            new Card(Suit.Spades, Rank.Ten),
            new Card(Suit.Diamonds, Rank.Two),
            new Card(Suit.Clubs, Rank.Three)
        };

        var category = evaluator.GetHandCategory(holeCards, board);
        var description = evaluator.GetHandDescription(holeCards, board);
        var score = evaluator.EvaluateScore(holeCards, board);

        Assert.False(string.IsNullOrWhiteSpace(category));
        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Equal(1, score);
    }
}
