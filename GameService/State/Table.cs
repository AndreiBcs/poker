using System.Collections.Generic;
using GameService.Core.GameLogic;

namespace GameService.State
{
    public class Table
    {
        public string RoomId { get; set; }
        public int Pot { get; set; }
        public GamePhase Phase { get; set; }
        public List<Card> BoardCards { get; set; } = new List<Card>();
        public List<Player> Players { get; set; } = new List<Player>();
        public int CurrentPlayerIndex { get; set; }
        public Deck CurrentDeck { get; set; }=new Deck();

        public Table(string roomId)
        {
            RoomId = roomId;
            Phase = GamePhase.WaitingForPlayers;
        }
    }
}