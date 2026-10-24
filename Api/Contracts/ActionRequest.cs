using Core.Domain.Enums;

namespace Api.Contracts;

public record ActionRequest(PlayerAction Action, int Amount = 0);
