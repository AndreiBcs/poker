using HoldemPoker.Evaluator;
using Source.Domain.Enums;
using Card = Source.Domain.ValueObjects.Card;

namespace Source.Domain.GameLogic
{
    public class HandEvaluator 
    {
         public int EvaluateScore(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
         
            return HoldemHandEvaluator.GetHandRanking([.. pCards, .. bCards]);
        }

        public string GetHandCategory(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
            return HoldemHandEvaluator.GetHandCategory([.. pCards, .. bCards]).ToString();
        }

        public string GetHandDescription(List<Card> holeCards, List<Card> board)
        {
            var pCards = MapCards(holeCards).ToArray();
            var bCards = MapCards(board).ToArray();
            return HoldemHandEvaluator.GetHandDescription([.. pCards, .. bCards]);
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