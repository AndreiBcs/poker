using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackjackNet
{
    public class Player : Participant
    {
        public override int CalculateScore()
        {
            // Caz special: două cărți și ambele sunt ași => 21
            if (hand.Count == 2 &&
                hand[0].CardType == PlayingCard.Type.Ace &&
                hand[1].CardType == PlayingCard.Type.Ace)
            {
                return 21;
            }
            int score = 0;
            int aceCount = 0;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                score = score + card.Value;
                if (card.CardType == PlayingCard.Type.Ace)
                    aceCount++;
            }
            while (aceCount > 0 && score <= 11)
            {
                score = score + 10;
                aceCount--;
            }

            return score;
        }
    }
}
