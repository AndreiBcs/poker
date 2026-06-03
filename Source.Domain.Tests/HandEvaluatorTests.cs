using System.Collections.Generic;
using Source.Domain.Enums;
using Source.Domain.GameLogic;
using Source.Domain.ValueObjects;
using Xunit;

namespace Source.Domain.Tests;

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
        Assert.True(score > 0);
    }
}
