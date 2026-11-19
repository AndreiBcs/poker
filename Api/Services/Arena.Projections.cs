using Api.Contracts;
using Core.Domain;
using Core.Domain.Enums;
using Core.Game;

namespace Api.Services;

public sealed partial class Arena
{
    public object State(Guid roomId, Guid playerId, Guid? sessionId = null)
    {
        lock (_gate)
        {
            var room = Member(roomId, playerId);
            var game = Game(room);
            if (sessionId.HasValue && (game?.SessionId != sessionId || IsFinished(game)))
                throw new ArenaException(404, "This saved game is no longer active.");
            return new
            {
                room.Id,
                room.Name,
                room.OwnerId,
                room.Settings.MaxPlayers,
                room.Settings.SmallBlind,
                room.Settings.BigBlind,
                room.StartRequested,
                Status = !room.StartRequested ? "waiting" : game is null ? "connecting" :
                    IsFinished(game) ? "finished" : PlayersConnected(room, game) ? "playing" : "paused",
                room.Notice,
                room.Revision,
                Players = room.Players.Select(p => 
                    new
                    {
                        p.Id,
                        p.Nickname,
                        Connected = room.Connections.GetValueOrDefault(p.Id) > 0
                    }).ToArray(),
                Game = game is null ? null : ProjectGame(game, playerId, room.Players.Select(p => p.Id).ToHashSet())
            };
        }
    }

    private static object ProjectGame(GameSession game, Guid viewerId, HashSet<Guid> members) => new
    {
        game.SessionId,
        game.Phase,
        game.Pot,
        game.CurrentBet,
        game.MinRaise,
        MinimumRaiseChips = game.MinimumRaiseChips(viewerId),
        CurrentTurnUserId = game.CurrentTurnIndex is { } turn ? game.Players[turn].UserId : (Guid?)null,
        Board = game.Board.Select(Card).ToArray(),
        Players = game.Players.Where(p => members.Contains(p.UserId)).Select(p => new
        {
            Id = p.UserId,
            Nickname = p.Username,
            p.SeatIndex,
            p.Chips,
            p.CurrentBet,
            p.HasFolded,
            p.IsAllIn,
            p.IsSittingOut,
            IsDealer = p.SeatIndex == game.DealerIndex,
            IsSmallBlind = p.SeatIndex == game.SmallBlindIndex,
            IsBigBlind = p.SeatIndex == game.BigBlindIndex,
            HoleCards = p.UserId == viewerId ? p.HoleCards.Select(Card).ToArray() : [],
            CardCount = p.HoleCards.Count
        }).ToArray(),
        LastResult = game.LastResult is { } result ? new
        {
            result.Pot,
            result.HandDescription,
            result.Winners,
            Board = result.Board.Select(Card).ToArray()
        } : null
    };

    private static string Card(Card card)
    {
        var rank = card.Rank switch
        {
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)card.Rank).ToString()
        };
        
        return rank + card.Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            _ => "♠"
        };
    }
}
