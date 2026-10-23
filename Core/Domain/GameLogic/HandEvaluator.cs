using System.Collections.Generic;
using System.Linq;
using HoldemPoker.Evaluator;
using GameService.State.Interfaces;
using GameService.State;

namespace GameService.Core.GameLogic
{
    public class HandEvaluator : IHandEvaluator
    {
        public int EvaluateScore(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
         
            return HoldemHandEvaluator.GetHandRanking(pCards.Concat(bCards).ToArray());
        }

        public string GetHandCategory(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
            return HoldemHandEvaluator.GetHandCategory(pCards.Concat(bCards).ToArray()).ToString();
        }

        public string GetHandDescription(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
            return HoldemHandEvaluator.GetHandDescription(pCards.Concat(bCards).ToArray());
        }

        
        private IEnumerable<HoldemPoker.Cards.Card> MapCards(List<Card> cards)
        {
            return cards.Select(c => 
            {
               
                string rankString = c.Rank switch
                {
                    Rank.Ten => "T", Rank.Jack => "J", Rank.Queen => "Q", Rank.King => "K", Rank.Ace => "A",
                    _ => ((int)c.Rank).ToString()
                };
                string suitString = c.Suit.ToString()[0].ToString().ToLower(); 
                
                return HoldemPoker.Cards.Card.Parse($"{rankString}{suitString}");
            });
        }
    }
}