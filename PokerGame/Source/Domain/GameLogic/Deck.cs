using Source.Domain.Enums;
using Source.Domain.ValueObjects;

namespace Source.Domain.GameLogic
{
    public class Deck
    {
        private readonly List<Card> _cards = new();
        private readonly Random _random = new();

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

        public void Shuffle()
        {
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
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
}