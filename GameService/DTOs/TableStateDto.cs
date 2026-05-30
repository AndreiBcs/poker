using System.Collections.Generic;
using GameService.Core.GameLogic;
using GameService.State;

namespace GameService.DTOs
{
    public record TableStateDto(
        List<Card> BoardCards,
        int Pot,
        GamePhase Phase,
        List<PlayerDto> Players
    );

    public record PlayerDto(
        string Id,
        string Name,
        int Chips,
        int CurrentBet,
        bool HasFolded,
        bool IsAllIn,
        List<Card> HoleCards
    );
}