using System;
using System.Collections.Generic;

namespace BlackjackNet
{
    public class CardDeck
    {
        private List<PlayingCard> cards;
        private Random rng;

        public CardDeck()
        {   
            cards = new List<PlayingCard>();
            rng = new Random();
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            cards.Clear();
            var suits = (PlayingCard.Suit[])Enum.GetValues(typeof(PlayingCard.Suit));
            for (int s = 0; s < suits.Length; s++)
            {
                var suit = suits[s];
                
                    for (int i = 2; i <= 10; i++)
                        cards.Add(new PlayingCard(suit, PlayingCard.Type.Number, i));

                cards.Add(new PlayingCard(suit, PlayingCard.Type.Jack, 10));
                cards.Add(new PlayingCard(suit, PlayingCard.Type.Queen, 10));
                cards.Add(new PlayingCard(suit, PlayingCard.Type.King, 10));
                cards.Add(new PlayingCard(suit, PlayingCard.Type.Ace, 1));              
            }
        }

        public PlayingCard DrawCard()
        {
            if (cards.Count == 0)
                throw new InvalidOperationException("Nu mai sunt cărți în deck!");
            int index = rng.Next(cards.Count);
            var card = cards[index];
            cards.RemoveAt(index);
            return card;
        }

        public int GetTotalNumberOfCards() {
            return cards.Count;
        }

        public void Reset() {
            InitializeDeck();
        }
    }
}