using GameService.State;

namespace GameService.Core
{
    public interface IGameRoomService
    {
        Table CreateRoom(string roomId);
        Table JoinRoom(string roomId, Player player);
        Table GetTable(string roomId);
    }

    public interface IPlayerActionService
    {
        bool PlaceBet(string roomId, string playerId, int amount);
        bool Fold(string roomId, string playerId);
        bool Check(string roomId, string playerId);
    }

    public interface IGameManager : IGameRoomService, IPlayerActionService
    {
        
        void AdvancePhase(string roomId);
    }
}