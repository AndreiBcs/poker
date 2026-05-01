using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.State
{
   public class Table
{
    public string RoomId { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = new();
    public List<Card> BoardCards { get; set; } = new();
    
    public int Pot { get; set; } = 0;
    public int CurrentHighestBet { get; set; } = 0; 
    
    public GamePhase Phase { get; set; } = GamePhase.Waiting;
    public int CurrentTurnIndex { get; set; } = 0; 
    public Deck Deck { get; set; } = new();

    // Helpere
    public Player? GetCurrentPlayer() => 
        Players.Count > 0 ? Players[CurrentTurnIndex] : null;

    public void NextTurn()
    {
        
        do {
            CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count;
        } while (Players[CurrentTurnIndex].HasFolded || Players[CurrentTurnIndex].IsAllIn);
    }
}
}