using Core.Domain.Enums;

namespace Api;

public record ActionRequest(PlayerAction Action, int Amount = 0);
