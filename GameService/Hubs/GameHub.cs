

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
            Table tableState = _gameManager.GetTable(roomId);

            foreach (var player in tableState.Players)
            {
                var safeDto = _tableStateDtoProvider.BuildTableStateDto(tableState, player.Id);
                await Clients.User(player.Id).SendAsync("StateUpdated", safeDto);
            }
        }
    }
}