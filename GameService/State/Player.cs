using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.State
{
    public class Player
{
    public string Id { get; set; } = string.Empty; 
    public string Name { get; set; } = string.Empty;
    public int Chips { get; set; } = 1000; 
    public int CurrentBet { get; set; } = 0; 
    public bool HasFolded { get; set; } = false;
    public bool IsAllIn { get; set; } = false;
    public List<Card> HoleCards { get; set; } = new();
}
}