using System;
using System.Linq;
using GameService.State;
using GameService.State.Interfaces;

namespace GameService.Core.GameLogic
{

    public class TexasHoldemStateMachine : IGameStateMachine
    {
        private readonly IHandEvaluator _handEvaluator;
        
       public GameType SupportedGameType => GameType.TexasHoldem;
        public TexasHoldemStateMachine(IHandEvaluator handEvaluator)
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
                List<Player> winners = new List<Player>();
                foreach (var player in activePlayers)
                {
                    var score = _handEvaluator.EvaluateScore(player.HoleCards, table.BoardCards);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        winner = player;
                        winners.Clear();
                        winners.Add(player);
                    }
                    else if(score == bestScore)
                    {
                        winners.Add(player);
                    }


                }
                 
                 if (winners.Count == 1)
                {
                    winners[0].Chips += table.Pot;
                }
                else if (winners.Count > 1)
                {
                    int splitPot = table.Pot / winners.Count;
                    int oddChips = table.Pot % winners.Count; 

                  
                    foreach(var p in winners)
                    {
                        p.Chips += splitPot;
                    }

                   
                    if (oddChips > 0)
                    {
                        var oddChipWinner = winners.OrderBy(w => 
                        {
                            int playerIndex = table.Players.IndexOf(w);
                       
                            int distance = (playerIndex - table.DealerIndex + table.Players.Count) % table.Players.Count;
                            
                         
                            return distance == 0 ? table.Players.Count : distance;
                        }).First();

                        oddChipWinner.Chips += oddChips;
                    }
                }
             
            }

            table.Pot = 0;
            table.Phase = GamePhase.WaitingForPlayers; 
        }
    }
}