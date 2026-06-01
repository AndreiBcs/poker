namespace GameService.State
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public enum GameType
    {
        TexasHoldem,
        FiveCardDraw
    }

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