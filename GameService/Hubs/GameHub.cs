using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[Authorize]
public class GameHub(GameManager gameManager) : Hub
{
    
    public async Task JoinRoom(string roomId)
    {
        var userId = Context.UserIdentifier; 
        var username = Context.User.FindFirst(ClaimTypes.Name)?.Value; 

      
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        
        await Clients.Group(roomId).SendAsync("PlayerJoined", username);
    }

  
    public async Task PlaceBet(string roomId, int amount)
    {
        var userId = Context.UserIdentifier;
        
        await Clients.Group(roomId).SendAsync("StateUpdated",);  // acolo dupa stateupdate trebe metoda catre arata datele mesei dar 
                                                                //fara sa vada restul  deci un DTO pentru tabla si dupa ceva care te lasa doar un player  pe tine deci am putea 
                                                                // injectie de dependenta
    }
}