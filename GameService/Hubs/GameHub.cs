using Microsoft.AspNetCore.SignalR;

namespace GameService.Hubs
{
    public class GameHub : Hub
    {
        // When a player clicks, their game client calls this method
        public async Task SendEchoPulse(double x, double y, string pulseType)
        {
            // The server instantly forwards this coordinates to ALL OTHER players
            // "ReceiveEchoPulse" is the Javascript function the clients will listen for
            await Clients.Others.SendAsync("ReceiveEchoPulse", x, y, pulseType, Context.ConnectionId);
        }

        // Optional: Run code when a player connects
        public override async Task OnConnectedAsync()
        {
            // E.g., spawn a new player
            await base.OnConnectedAsync();
        }
    }
}