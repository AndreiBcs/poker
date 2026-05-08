namespace GameService.State
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public enum GamePhase
    {
        WaitingForPlayers, 
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown
    }
}