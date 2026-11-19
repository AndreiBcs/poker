using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public sealed class ArenaHub(ArenaUpdates updates, Arena arena) : Hub
{
    public async Task JoinRoom(Guid roomId)
    {
        if (Context.Items.ContainsKey("joined")) throw new HubException("Already connected to a lobby.");
        try
        {
            var playerId = arena.SeatId(Context.GetHttpContext()?.Request.Cookies[SeatCookie.Name]);
            updates.Connect(Context.ConnectionId, roomId, playerId);
        }
        catch (ArenaException ex) { throw new HubException(ex.Message); }
        Context.Items["joined"] = true;
        await updates.Publish(roomId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await updates.Disconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
