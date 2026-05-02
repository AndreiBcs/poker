using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GameService.Providers.Interfaces;
using GameService.Core; // Assuming GameManager is here

[Authorize]
public class GameHub(GameManager gameManager, ITableStateDtoProvider tableStateDtoProvider) : Hub
{
    private readonly GameManager _gameManager = gameManager;
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