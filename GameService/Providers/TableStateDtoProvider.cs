using System.Linq;
using System.Collections.Generic;
using GameService.State;
using GameService.DTOs;
using GameService.State.Interfaces; // Updated namespace
using GameService.Core.GameLogic;

namespace GameService.Providers
{
    public class TableStateDtoProvider : ITableStateDtoProvider
    {
        public TableStateDto BuildTableStateDto(Table table, string viewerPlayerId)
        {
            return new TableStateDto(
                BoardCards: table.BoardCards,
                Pot: table.Pot,
                Phase: table.Phase,
                Players: table.Players.Select(p => new PlayerDto(
                    Id: p.Id,
                    Name: p.Name,
                    Chips: p.Chips,
                    CurrentBet: p.CurrentBet,
                    HasFolded: p.HasFolded,
                    IsAllIn: p.IsAllIn,
                    HoleCards: p.Id == viewerPlayerId ? p.HoleCards : new List<Card>() 
                )).ToList()
            );
        }
    }
}