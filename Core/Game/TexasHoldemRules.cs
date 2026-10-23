using Core.Domain;
using Core.Domain.Enums;

namespace Core.Game;

public sealed class TexasHoldemRules
{
    public GameStartStatus StartHand(GameSession session)
    {
        if (session.Phase is GamePhase.PreFlop or GamePhase.Flop or GamePhase.Turn or GamePhase.River)
        {
            return GameStartStatus.AlreadyInProgress;
        }

        if (session.Players.Count(p => p.Chips > 0) < 2)
        {
            session.ResetToWaiting();
            return GameStartStatus.NotEnoughPlayers;
        }

        session.PrepareForNewHand();

        var deck = new Deck();
        session.Deck = deck;
        session.DealerIndex = session.FindNextIndex(session.DealerIndex, player => player.Chips > 0);
        var activePlayers = session.Players.Count(player => player.Chips > 0);

        if (activePlayers == 2)
        {
            session.SmallBlindIndex = session.DealerIndex;
            session.BigBlindIndex = session.FindNextIndex(session.DealerIndex, player => player.Chips > 0);
        }
        else
        {
            session.SmallBlindIndex = session.FindNextIndex(session.DealerIndex, player => player.Chips > 0);
            session.BigBlindIndex = session.FindNextIndex(session.SmallBlindIndex, player => player.Chips > 0);
        }

        session.DealHoleCards(deck, 2);
        var smallBlindAmount = session.PostBlind(session.Players[session.SmallBlindIndex], session.SmallBlind);
        var bigBlindAmount = session.PostBlind(session.Players[session.BigBlindIndex], session.BigBlind);
        session.CurrentBet = Math.Max(smallBlindAmount, bigBlindAmount);
        session.MinRaise = session.BigBlind;

        session.Phase = GamePhase.PreFlop;
        session.CurrentTurnIndex = session.NormalizeTurnIndex(session.FindNextIndex(session.BigBlindIndex, player => player.CanAct));

        if (session.CurrentTurnIndex is null)
        {
            session.DealRemainingBoard();
            session.ResolveShowdown(session.Players.Where(p => p.IsInHand).ToList());
        }

        return GameStartStatus.Started;
    }

    public GameActionResult ApplyAction(GameSession session, Guid userId, PlayerAction action, int amount)
    {
        if (session.Phase is GamePhase.WaitingForPlayers or GamePhase.Showdown)
        {
            return new GameActionResult(false, "The hand is not active.");
        }

        var playerIndex = session.Players.FindIndex(p => p.UserId == userId);
        if (playerIndex < 0)
        {

            return new GameActionResult(false, "Player not found.");

        }

        if (session.CurrentTurnIndex != playerIndex)
        {
            return new GameActionResult(false, "It is not your turn.");
        }

        var player = session.Players[playerIndex];
        if (!player.CanAct)
        {
            return new GameActionResult(false, "Player cannot act.");
        }

        switch (action)
        {
            case PlayerAction.Fold:
                player.HasFolded = true;
                player.HasActedThisRound = true;
                break;
            case PlayerAction.Check:
                if (player.CurrentBet != session.CurrentBet)
                {
                    return new GameActionResult(false, "Cannot check when there is a bet to call.");
                }
                player.HasActedThisRound = true;
                break;
            case PlayerAction.Call:
                var toCall = session.CurrentBet - player.CurrentBet;
                if (toCall <= 0)
                {
                    player.HasActedThisRound = true;
                    break;
                }

                var called = player.Bet(toCall);
                session.Pot += called;
                player.HasActedThisRound = true;
                break;
            case PlayerAction.Raise:
                if (amount <= 0)
                {
                    return new GameActionResult(false, "Raise amount must be positive.");
                }

                if (amount < session.MinRaise)
                {
                    return new GameActionResult(false, $"Minimum raise is {session.MinRaise}.");
                }

                var targetBet = (long)session.CurrentBet + amount;
                var needed = targetBet - player.CurrentBet;
                if (needed <= 0)
                {
                    return new GameActionResult(false, "Raise amount is too small.");
                }

                if (needed > player.Chips)
                {
                    return new GameActionResult(false, "Not enough chips to raise.");
                }

                var raised = player.Bet((int)needed);
                session.Pot += raised;
                session.CurrentBet = player.CurrentBet;
                session.MinRaise = amount;
                player.HasActedThisRound = true;

                foreach (var other in session.Players)
                {
                    if (other.UserId != player.UserId && other.CanAct)
                    {
                        other.HasActedThisRound = false;
                    }
                }
                break;
            default:
                return new GameActionResult(false, "Unknown action.");
        }

        if (session.TryFinishHand())
        {
            return new GameActionResult(true, null);
        }

        if (session.TryAdvanceRound())
        {
            return new GameActionResult(true, null);
        }

        session.CurrentTurnIndex = session.NormalizeTurnIndex(session.FindNextIndex(playerIndex, p => p.CanAct));
        return new GameActionResult(true, null);
    }
}