

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GameService.State.Interfaces; // Updated namespace
using GameService.Core;
using GameService.State;

namespace GameService.Hubs 
{
    [Authorize]
    public class GameHub(IGameManager gameManager, ITableStateDtoProvider tableStateDtoProvider) : Hub
    {
        private readonly IGameManager _gameManager = gameManager;
        private readonly ITableStateDtoProvider _tableStateDtoProvider = tableStateDtoProvider;

  public async Task PlaceBet(string roomId, int amount)
{
    Table? tableState = _gameManager.GetTable(roomId);

    // 👇 ADD THIS SAFETY CHECK
    if (tableState == null)
    {
        await Clients.Caller.SendAsync("Error", "Table not found.");
        return;
    }

    foreach (var player in tableState.Players)
    {
        var safeDto = _tableStateDtoProvider.BuildTableStateDto(tableState, player.Id);
        await Clients.User(player.Id).SendAsync("StateUpdated", safeDto);
    }
}
        public async Task DiscardCards(string roomId, List<int> cardIndexesToDiscard)
        {
           string playerId = Context.UserIdentifier ?? Context.ConnectionId;
            bool success = _gameManager.DiscardCards(roomId, playerId, cardIndexesToDiscard);
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", "Invalid discard action.");
                return;
            }

          Table? tableState = _gameManager.GetTable(roomId);

    // 👇 ADD THIS SAFETY CHECK HERE TOO
    if (tableState == null)
    {
        await Clients.Caller.SendAsync("Error", "Table not found.");
        return;
    }

            foreach (var player in tableState.Players)
            {
                var safeDto = _tableStateDtoProvider.BuildTableStateDto(tableState, player.Id);
                await Clients.User(player.Id).SendAsync("StateUpdated", safeDto);
            }
        }
    }
}