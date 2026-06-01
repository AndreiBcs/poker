using GameService.State;

namespace GameService.Core.GameLogic
{
    public interface IGameStateMachine
    {
        GameType SupportedGameType { get; } 
        void StartNewHand(Table table);
        void AdvancePhase(Table table);
    }
}