using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Core.GameLogic
{
   public class HandEvaluator
{
   
    public int EvaluateScore(List<Card> holeCards, List<Card> board)
    {
        var allCards = holeCards.Concat(board).ToList();
        
        // TODO: Trebuie implementata logica de pokeeer (Flush, Straight, Pair s)
     
        if (!allCards.Any()) return 0;
        return (int)allCards.Max(c => c.Rank); 
    }
}
}