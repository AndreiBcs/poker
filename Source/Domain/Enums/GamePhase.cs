namespace Source.Domain.Enums
{
    public enum GamePhase
    {
         WaitingForPlayers,
        
       // TexasHoldem:
        PreFlop,
        Flop,
        Turn,
        River,
        
        // Five Cards Draw:
        InitialDeal,  
        DrawCards,      
        SecondBetting,   
        
       //ambele se folosesc de showdown
        Showdown
    }
}