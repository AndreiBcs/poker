using Core.Game;

namespace Api;

public sealed partial class Arena
{
    public void Start(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            RequireOwner(room, playerId);
            if (room.Players.Count < 2) Fail("At least two players are needed.");
            if (room.Game is not null)
            {
                if (!room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) Fail("Waiting for every player to reconnect.");
                var status = room.Game.StartHand();
                if (status != GameStartStatus.Started) Fail(status == GameStartStatus.NotEnoughPlayers ? "Not enough players with chips." : "A hand is already active.");
                room.Announce("A new hand started.");
                return;
            }
            room.StartRequested = true;
            room.Announce("Waiting for every player to connect.");
            TryStart(room);
        }
    }

    public void Connect(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            room.Connections[playerId] = room.Connections.GetValueOrDefault(playerId) + 1;
            TryStart(room);
        }
    }

    public void Disconnect(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var count = room.Connections.GetValueOrDefault(playerId);
            if (count > 1) room.Connections[playerId] = count - 1;
            else room.Connections.Remove(playerId);
        }
    }

    public void Act(Guid roomId, Guid playerId, ActionRequest request)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var game = ActiveGame(room);
            var outcome = game.ApplyAction(playerId, request.Action, request.Amount);
            if (!outcome.Success) Fail(outcome.Error!);
            room.Announce($"{GetPlayer(playerId).Nickname}: {request.Action.ToString().ToLowerInvariant()}.");
        }
    }

    private static GameSession ActiveGame(Room room)
    {
        if (!room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) Fail("Game paused until every player reconnects.");
        return room.Game ?? throw new ArenaException(409, "Game has not started.");
    }

    private static void TryStart(Room room)
    {
        if (!room.StartRequested || room.Game is not null || room.Players.Count < 2 ||
            !room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) return;
        room.Game = new GameSession(room.Id, room.Name, room.OwnerId, room.Settings.SmallBlind, room.Settings.BigBlind,
            room.Players.Select(p => (p.Id, p.Nickname)).ToList(), 2000);
        room.Game.StartHand();
        room.Announce("All players connected. The game has begun.");
    }
}
