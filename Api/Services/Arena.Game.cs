using Api.Contracts;
using Core.Domain.Enums;
using Core.Game;

namespace Api.Services;

public sealed partial class Arena
{
    public void Start(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            RequireOwner(room, playerId);
            if (room.Players.Count < 2) Fail("At least two players are needed.");
            var game = Game(room);
            if (game is not null)
            {
                if (!PlayersConnected(room, game))
                    Fail("Waiting for every player to reconnect.");
                
                var status = game.StartHand();
                if (status != GameStartStatus.Started) 
                    Fail(status == GameStartStatus.NotEnoughPlayers ?
                        "At least two players with chips must be at the table." : "A hand is already active.");
                
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
            var game = Game(room);
            if (IsFinished(game)) throw new ArenaException(409, "This game has ended.");
            game?.SetSittingOut(playerId, false);
            room.Connections[playerId] = room.Connections.GetValueOrDefault(playerId) + 1;
            room.Touch();
            TryStart(room);
        }
    }

    public void StepAway(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var game = Game(room);
            if (game is null) throw new ArenaException(409, "Game has not started.");
            game.SetSittingOut(playerId, true);
            room.Announce($"{GetPlayer(playerId).Nickname} stepped away.");
        }
    }

    public void Disconnect(Guid roomId, Guid playerId)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var count = room.Connections.GetValueOrDefault(playerId);
            if (count > 1) room.Connections[playerId] = count - 1;
            else if (!room.Connections.Remove(playerId)) return;
            room.Touch();
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

    private GameSession ActiveGame(Room room)
    {
        var game = Game(room) ?? throw new ArenaException(409, "Game has not started.");
        if (!PlayersConnected(room, game)) Fail("Game paused until players reconnect.");
        return game;
    }

    private GameSession? Game(Room room) => _games.GetValueOrDefault(room.Id);

    private static bool IsFinished(GameSession? game) =>
        game is { Phase: GamePhase.WaitingForPlayers } && !game.HasEnoughPlayers();

    private static bool PlayersConnected(Room room, GameSession game) => game.Players
        .Where(p => game.Phase == GamePhase.WaitingForPlayers ? p.Chips > 0 && !p.IsSittingOut : p.IsInHand)
        .All(p => room.Connections.GetValueOrDefault(p.UserId) > 0);

    private void TryStart(Room room)
    {
        if (!room.StartRequested || Game(room) is not null || room.Players.Count < 2 ||
            !room.Players.All(p => room.Connections.GetValueOrDefault(p.Id) > 0)) return;
        var game = new GameSession(room.Id, room.Name, room.OwnerId, room.Settings.SmallBlind, room.Settings.BigBlind,
            room.Players.Select(p => (p.Id, p.Nickname)).ToList(), 2000);
        game.StartHand();
        _games.Add(room.Id, game);
        room.Announce("All players connected. The game has begun.");
    }
}
