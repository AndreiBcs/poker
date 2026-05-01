using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Core.GameLogic
{
     public record Card(Suit Suit, Rank Rank);

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
         _cards = new Stack<Card>(deck.OrderBy(_ => rng.Next()));
    }

      public Card Draw() => _cards.Pop();
   }
}