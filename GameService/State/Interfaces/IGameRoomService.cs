using GameService.State;
using System.Collections.Generic; // Added for List<int> compatibility

namespace GameService.Core
{
    public interface IGameRoomService
    {
        Table CreateRoom(string roomId);
        
        // FIX: Added '?' to allow returning null if the room doesn't exist
        Table? JoinRoom(string roomId, Player player);
        Table? GetTable(string roomId);
    }

    public interface IPlayerActionService
    {
        bool PlaceBet(string roomId, string playerId, int amount);
        bool Fold(string roomId, string playerId);
        bool Check(string roomId, string playerId);
    }

    public interface IGameManager : IGameRoomService, IPlayerActionService
    {
        bool DiscardCards(string roomId, string playerId, List<int> cardIndexesToDiscard);
        void AdvancePhase(string roomId);
    }
}