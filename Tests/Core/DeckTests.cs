using System;
using System.Collections.Generic;
using Core.Domain;
using Xunit;

namespace Core.Tests.Core;

public class DeckTests
{
    [Fact]
    public void Draw_ReturnsUniqueCards_AndThrowsWhenEmpty()
    {
        var deck = new Deck();
        var seen = new HashSet<string>();

        for (var i = 0; i < 52; i++)
        {
            var card = deck.Draw();
            seen.Add($"{card.Suit}-{card.Rank}");
        }

        Assert.Equal(52, seen.Count);
        Assert.Throws<InvalidOperationException>(deck.Draw);
    }

    [Fact]
    public void Reset_RebuildsFullDeck()
    {
        var deck = new Deck();
        for (var i = 0; i < 10; i++)
        {
            deck.Draw();
        }

        deck.Reset();

        var seen = new HashSet<string>();
        for (var i = 0; i < 52; i++)
        {
            var card = deck.Draw();
            seen.Add($"{card.Suit}-{card.Rank}");
        }

        Assert.Equal(52, seen.Count);
    }
}
