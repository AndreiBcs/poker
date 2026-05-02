using System.Collections.Generic;

namespace BlackjackNet
{
    
    public abstract class Participant
    {
        protected List<PlayingCard> hand = new List<PlayingCard>();

        public void AddCard(PlayingCard card)
        {
            hand.Add(card);
        }

        public IReadOnlyList<PlayingCard> Hand
        {
            get { return hand.AsReadOnly(); }
        }

        public abstract int CalculateScore();

        public void ClearHand()
        {
            hand.Clear();
        }
    }
}