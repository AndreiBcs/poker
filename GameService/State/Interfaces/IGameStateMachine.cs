using System.Linq;
using GameService.State;
using GameService.State.Interfaces;
using GameService.Core.GameLogic;

namespace GameService.Core
{
    public interface IGameStateMachine
    {
        void StartNewHand(Table table);
        void  AdvancePhase(Table table);
        
    }
}