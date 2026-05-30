using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GameService.Core.GameLogic; // Added missing using

namespace GameService.State
{
    public class Player
    {
            public bool HasActed { get; set; }
        public string Id { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty;
        public int Chips { get; set; } = 1000; 
        public int CurrentBet { get; set; } = 0; 
        public bool HasFolded { get; set; } = false;
        public bool IsAllIn { get; set; } = false;
        public List<Card> HoleCards { get; set; } = new();
        public bool HasDrawnCards { get; set; } = false; // New property to track if the player has drawn cards
        public void ResetForNewHand()
        {
            HoleCards.Clear();
            CurrentBet = 0;
            HasFolded = false;
            IsAllIn = false;
            HasDrawnCards=false;
        }
    }
}