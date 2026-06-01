using System.Collections.Generic;
using GameService.Core.GameLogic; 

namespace GameService.State.Interfaces
{
    public interface IHandEvaluator
    {
        int EvaluateScore(List<Card> holeCards, List<Card> board);
        string GetHandCategory(List<Card> holeCards, List<Card> board);
        string GetHandDescription(List<Card> holeCards, List<Card> board);
    }
}