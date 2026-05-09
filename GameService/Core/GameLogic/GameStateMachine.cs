using System;
using System.Linq;
using GameService.State;
using GameService.State.Interfaces;

namespace GameService.Core.GameLogic
{
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IHandEvaluator _handEvaluator;
        

        public GameStateMachine(IHandEvaluator handEvaluator)
        {
            _handEvaluator = handEvaluator;
            
        }

        public void StartNewHand(Table table)
        {
            if (table.Players.Count < 2) return; 

           
            table.BoardCards.Clear();
            table.Pot = 0;
            foreach (var p in table.Players) p.ResetForNewHand();

            
            table.CurrentDeck = new Deck();

         
            for (int i = 0; i < 2; i++)
            {
                foreach (var player in table.Players.Where(p => p.Chips > 0))
                {
                    player.HoleCards.Add(table.CurrentDeck.Draw());
                }
            }

            // TODO:  dedu ce blind si ordinea lor apoi da update la chips
            
            table.Phase = GamePhase.PreFlop;
            
           
        }

        public void AdvancePhase(Table table)
        {
            switch (table.Phase)
            {
                case GamePhase.PreFlop:
                    
                    table.BoardCards.Add(table.CurrentDeck.Draw());
                    table.BoardCards.Add(table.CurrentDeck.Draw());
                    table.BoardCards.Add(table.CurrentDeck.Draw());
                  
                    table.Phase = GamePhase.Flop;
                    break;

                case GamePhase.Flop:
                    
                    table.BoardCards.Add(table.CurrentDeck.Draw());
                    table.Phase = GamePhase.Turn;
                    break;

                case GamePhase.Turn:
                  
                    table.BoardCards.Add(table.CurrentDeck.Draw());
                    table.Phase = GamePhase.River;
                    break;

                case GamePhase.River:
                    table.Phase = GamePhase.Showdown;
                    ResolveShowdown(table);
                    break;
            }
        }

        private  void ResolveShowdown(Table table)
        {
            var activePlayers = table.Players.Where(p => !p.HasFolded).ToList();

            if (activePlayers.Count == 1)
            {
                
                activePlayers.First().Chips += table.Pot;
            }
            else
            {
              
                var bestScore = 0;
                Player?winner = null;

                foreach (var player in activePlayers)
                {
                    var score = _handEvaluator.EvaluateScore(player.HoleCards, table.BoardCards);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        winner = player;
                    }
                }

                if (winner != null)
                {
                     //TODO: fa si cazul in care e egal nu doar cand unul din cei doi castig
                    winner.Chips += table.Pot;
                }
            }

            table.Pot = 0;
            table.Phase = GamePhase.WaitingForPlayers; 
        }
    }
}