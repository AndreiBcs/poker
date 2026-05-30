using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameService.State;
using GameService.Core.GameLogic;       // Required for IGameStateMachine
using GameService.State.Interfaces;     // Required if any interface moved here
namespace GameService.Core.GameLogic // Ensure this matches where the file lives!
{
    public class GameManager : IGameManager 
    {
        // ...g
        private readonly ConcurrentDictionary<string, Table> _tables = new();
        private readonly IEnumerable<IGameStateMachine> _stateMachines;

    // Remove the underscore from the parameter name inside the parentheses
        public GameManager(IEnumerable<IGameStateMachine> stateMachines) 
        {
            // Now the field (left) can properly receive the parameter (right)
            _stateMachines = stateMachines; 
        }

        public Table CreateRoom(string roomId)
        {
            var table = new Table(roomId);
            _tables.TryAdd(roomId, table);
            return table;
        }

        public bool DiscardCards(string roomId, string playerId, List<int> cardIndexesToDiscard)
        {
            var table = GetTable(roomId);
            if (table == null||table.Phase !=GamePhase.DrawCards)return false;

            var player = GetCurrentPlayerIfValid(table, playerId);
            if (player == null || player.HasDrawnCards) return false;

            var sortedIndexes = cardIndexesToDiscard.OrderByDescending(i=> i).ToList();

            foreach (var index in sortedIndexes)
            {
                if(index >= 0 && index<player.HoleCards.Count)
                player.HoleCards.RemoveAt(index);
            }

            int cardsToDraw = sortedIndexes.Count;
            for (int i = 0; i < cardsToDraw; i++)
            {
                player.HoleCards.Add(table.CurrentDeck.Draw());
            }

            player.HasDrawnCards = true;
            var nextPlayer=GetNextPlayerForDrawPhase(table);
            if(nextPlayer==null)
            AdvancePhase(roomId);
            else
            table.CurrentPlayerIndex=table.Players.IndexOf(nextPlayer);
            return true;
        }
        
        private Player? GetNextPlayerForDrawPhase(Table table)
        {
            int startIndex=table.CurrentPlayerIndex;
            int count=table.Players.Count;
            for(int i=1;i<=count;i++)
            {
                int checkIndex=(startIndex +i)%count;
                var p=table.Players[checkIndex];
                if(!p.HasFolded && !p.HasDrawnCards)
                return p;
            }
            return null;
            

        }
        public Table? JoinRoom(string roomId, Player player)
        {
            if (_tables.TryGetValue(roomId, out var table))
            {
                table.Players.Add(player);
                return table;
            }
            return null;
        }

        public Table? GetTable(string roomId)
        {
            _tables.TryGetValue(roomId, out var table);
            return table;
        }

        public bool PlaceBet(string roomId, string playerId, int amount)
        {
            var table = GetTable(roomId);
            if (table == null) return false;

            var player = GetCurrentPlayerIfValid(table, playerId);
            if (player == null || amount > player.Chips) return false; 
            
            player.Chips -= amount;
            player.CurrentBet += amount;
            table.Pot += amount;

            if (player.Chips == 0) player.IsAllIn = true;

            MoveToNextPlayer(table);
            return true;
        }

        public bool Fold(string roomId, string playerId)
        {
            var table = GetTable(roomId);
            if (table == null) return false;

            var player = GetCurrentPlayerIfValid(table, playerId);
            if (player == null) return false;

            player.HasFolded = true;

            MoveToNextPlayer(table);
            return true;
        }

        public bool Check(string roomId, string playerId)
        {
            var table = GetTable(roomId);
            if (table == null) return false;

            var player = GetCurrentPlayerIfValid(table, playerId);
            if (player == null) return false;

           
            int highestBet = table.Players.Max(p => p.CurrentBet);
            if (player.CurrentBet < highestBet) return false; 

            MoveToNextPlayer(table);
            return true;
        }

        public void AdvancePhase(string roomId)
        {
            var table = GetTable(roomId);
            if (table != null)
            {
                var machine=_stateMachines.First(m=>m.SupportedGameType==table.GameType);
               
                machine.AdvancePhase(table);
                ResetBetsForNewPhase(table);
            }
        }

       

        private Player? GetCurrentPlayerIfValid(Table table, string playerId)
        {
            if (table.Players.Count == 0) return null;
            var expectedPlayer = table.Players[table.CurrentPlayerIndex];
            

            return expectedPlayer.Id == playerId ? expectedPlayer : null;
        }

        private void MoveToNextPlayer(Table table)
        {
            var activePlayers = table.Players.Where(p => !p.HasFolded && !p.IsAllIn).ToList();
            
            
            if (IsBettingRoundOver(table))
            {
                if (table.Players.Count(p => !p.HasFolded) <= 1)
                {
                  
                  table.Phase = GamePhase.River;
                var machine = _stateMachines.First(m => m.SupportedGameType == table.GameType);
                    machine.AdvancePhase(table);
                }
                else
                {
                  
                    AdvancePhase(table.RoomId);
                }
                return;
            }

         
         
            do
            {
                table.CurrentPlayerIndex = (table.CurrentPlayerIndex + 1) % table.Players.Count;
            } 
            while (table.Players[table.CurrentPlayerIndex].HasFolded || table.Players[table.CurrentPlayerIndex].IsAllIn);
        }

        private bool IsBettingRoundOver(Table table)
        {
            var activePlayers = table.Players.Where(p => !p.HasFolded && !p.IsAllIn).ToList();
            if (activePlayers.Count <= 1) return true; 

            int highestBet = table.Players.Max(p => p.CurrentBet);
            
         
            return activePlayers.All(p => p.CurrentBet == highestBet);
        }

        private void ResetBetsForNewPhase(Table table)
        {
            
           
            foreach (var player in table.Players)
            {
                player.CurrentBet = 0;
            }
           
           
            table.CurrentPlayerIndex = 0; 
        }
    }
}