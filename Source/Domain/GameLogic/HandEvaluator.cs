using HoldemPoker.Evaluator;

namespace Source.Domain.GameLogic
{
    public class HandEvaluator 
    {
        public int EvaluateScore(List<Card> cards, List<Card> board)
        {
            var playerCards = MapCards(cards).ToArray();
            var boardCards = MapCards(board).ToArray();

            return HoldemHandEvaluator.GetHandRanking(pCards.Concat(bCards).ToArray());
        }
    }
}