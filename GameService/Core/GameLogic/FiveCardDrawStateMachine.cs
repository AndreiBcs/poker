using System.Linq;
using GameService.State;
using GameService.State.Interfaces;

namespace GameService.Core.GameLogic
{
    public class FiveCardDrawStateMachine : IGameStateMachine
    {
        public GameType SupportedGameType => GameType.FiveCardDraw;
        private readonly IHandEvaluator _handEvaluator;

        public FiveCardDrawStateMachine(IHandEvaluator handEvaluator)
        {
            _handEvaluator = handEvaluator;
        }

        public void StartNewHand(Table table)
        {
            if (table.Players.Count < 2) return;
            table.BoardCards.Clear(); // La Five Card Draw NU există cărți pe masă!
            table.Pot = 0;
            table.CurrentDeck = new Deck();

            foreach (var p in table.Players) p.ResetForNewHand();

            // La Five Card Draw se dau direct 5 cărți fiecăruia!
            for (int i = 0; i < 5; i++)
            {
                foreach (var player in table.Players.Where(p => p.Chips > 0))
                {
                    player.HoleCards.Add(table.CurrentDeck.Draw());
                }
            }

            table.Phase = GamePhase.InitialDeal; // S-au dat cărțile, urmează pariuri
        }

        public void AdvancePhase(Table table)
        {
            switch (table.Phase)
            {
                case GamePhase.InitialDeal:
                    table.Phase = GamePhase.DrawCards; // Trecem la runda de schimbat cărți
                    break;
                case GamePhase.DrawCards:
                    table.Phase = GamePhase.SecondBetting; // După ce au schimbat, pariază din nou
                    break;
                case GamePhase.SecondBetting:
                    table.Phase = GamePhase.Showdown;
                    // ResolveShowdown(table);
                    break;
            }
        }
    }
}