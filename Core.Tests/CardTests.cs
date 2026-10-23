using PokerArena.Core.Domain.Enums;
using PokerArena.Core.Domain.ValueObjects;
using Xunit;

namespace PokerArena.Core.Domain.Tests;

public class CardTests
{
    [Fact]
    public void Card_Should_Have_Correct_Properties()
    {
        // Arrange
        var suit = Suit.Hearts;
        var rank = Rank.Ace;

        // Act
        var card = new Card(suit, rank);

        // Assert
        Assert.Equal(suit, card.Suit);
        Assert.Equal(rank, card.Rank);
    }

    [Fact]
    public void Card_Should_Be_Equal_When_Same_Suit_And_Rank()
    {
        // Arrange
        var card1 = new Card(Suit.Spades, Rank.King);
        var card2 = new Card(Suit.Spades, Rank.King);

        // Act & Assert
        Assert.Equal(card1, card2);
    }
}
