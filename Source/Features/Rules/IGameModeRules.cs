using Source.Domain.Enums;

namespace Source.Features.Game.Rules;

public interface IGameModeRules
{
    GameStartStatus StartHand(GameSession session);
    GameActionResult ApplyAction(GameSession session, Guid userId, PlayerAction action, int amount);
}