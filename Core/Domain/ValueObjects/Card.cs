using PokerArena.Core.Domain.Enums;

namespace PokerArena.Core.Domain.ValueObjects
{
    public record Card(Suit Suit, Rank Rank);
}