namespace Source.Domain.GameLogic
{
    public class Deck
    {
        private readonly Stack<Card> Cards = [];
        public Deck() => Shuffle();

        public List<Card> Shuffle() => Cards.Shuffle();

        public Card Draw() => Cards.Pop();
    }
}