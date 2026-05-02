using System;

namespace BlackjackNet
{
    public class PlayingCard
    {
        public enum Suit { Club, Diamond, Heart, Spade }
        public enum Type { Number, Jack, Queen, King, Ace }

        public Suit CardSuit { get; private set; }
        public Type CardType { get; private set; }
        public int Value { get; private set; }

        // Înlocuim ImagePath cu ResourceKey
        public string ResourceKey { get; private set; }

        public PlayingCard(Suit suit, Type type, int value)
        {
            CardSuit = suit;
            CardType = type;
            Value = value;
            ResourceKey = GetResourceKey();
        }

        private string GetResourceKey()
        {
            string suit = CardSuit.ToString(); // Club, Diamond, Heart, Spade
            string valueStr;

            switch (CardType)
            {
                case Type.Ace:
                    valueStr = "Ace";
                    break;
                case Type.Jack:
                    valueStr = "Jack";
                    break;
                case Type.Queen:
                    valueStr = "Queen";
                    break;
                case Type.King:
                    valueStr = "King";
                    break;
                case Type.Number:
                    valueStr = Value.ToString(); // 2..10
                    break;
                default:
                    valueStr = Value.ToString();
                    break;
            }

            // Cheia de resursă: ex. "2_Club", "Jack_Heart", "Ace_Spade"
            return $"{valueStr}_{suit}";
        }
    }
}