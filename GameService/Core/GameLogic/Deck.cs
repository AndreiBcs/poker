using System;
using System.Collections.Generic;
using System.Linq;
using GameService.State; 

namespace GameService.Core.GameLogic
{
    public class Deck
    {
        private Stack<Card> _cards = new();

        public Deck() => Shuffle();

        public void Shuffle()
        {
            var deck = new List<Card>();
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
            foreach (Rank r in Enum.GetValues(typeof(Rank)))
                deck.Add(new Card(s, r));
           
            var rng = new Random();
          
            for (int i = deck.Count - 1; i > 0; i--)
            {
              int j = rng.Next(i + 1);
              (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            _cards = new Stack<Card>(deck);
        }

        public Card Draw() => _cards.Pop();
    }
}