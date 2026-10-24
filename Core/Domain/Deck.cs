using Core.Domain.Enums;

namespace Core.Domain;

public class Deck
{
    private readonly List<Card> _cards = new();

    public Deck()
    {
        Reset();
    }

    public void Reset()
    {
        _cards.Clear();
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(suit, rank));
            }
        }

        Shuffle();
    }

    private void Shuffle()
    {
        for (var i = _cards.Count - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("The deck is empty.");
        }

        var index = _cards.Count - 1;
        var card = _cards[index];
        _cards.RemoveAt(index);
        return card;
    }
}