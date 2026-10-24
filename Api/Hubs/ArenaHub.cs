using Microsoft.AspNetCore.SignalR;

namespace Api;

public sealed class ArenaHub(ArenaUpdates updates) : Hub
{
    public async Task JoinRoom(Guid roomId, Guid playerId)
    {
        if (Context.Items.ContainsKey("joined")) throw new HubException("Already connected to a lobby.");
        try { updates.Connect(Context.ConnectionId, roomId, playerId); }
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
