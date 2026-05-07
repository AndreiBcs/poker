using System.Collections.Generic;
using HoldemPoker.Evaluator;
using GameService.State.Interfaces; // Use the new interface location

namespace GameService.Core.GameLogic
{
    public class HandEvaluator : IHandEvaluator
    {
        public int EvaluateScore(List<Card> holeCards, List<Card> board)
        {
           
            return HoldemHandEvaluator.GetHandRanking(holeCards, board);
        }

        public string GetHandCategory(List<Card> holeCards, List<Card> board)
        {
            return HoldemHandEvaluator.GetHandCategory(holeCards, board).ToString();
        }

        public string GetHandDescription(List<Card> holeCards, List<Card> board)
        {
            return HoldemHandEvaluator.GetHandDescription(holeCards, board);
        }
    }
}