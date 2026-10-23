## Flow and rules

1. Set a nickname (1–24 characters). The nickname is saved in local storage. A server-created player ID and current lobby ID are kept in session storage so a refresh reconnects to the same seat; separate browser sessions can play different seats.
2. Browse open lobbies or create one with a name, password and capacity of 2–10. The creator immediately joins as host. Lobby passwords are salted and hashed in memory and never returned in API responses.
3. Joining requires the correct password and an available seat. Waiting-room membership updates live. The host can kick guests or start with at least two players. Leaving transfers the host role to the next player; the last departure removes the lobby.
4. Starting closes the lobby to new joins. Cards are dealt only after all members have a live WebSocket connection. A dropped connection pauses actions until reconnection; refreshing automatically reconnects. A player who cannot reconnect can explicitly leave. If someone leaves while the initial connection barrier is pending, a one-player lobby reopens.
5. The server shuffles, deals private cards, posts blinds and enforces turns. Players can fold, check, call or raise. A raise amount is an increment above the current table bet, not the total contribution. Calling with insufficient chips goes all-in. Side pots are settled by contribution; ties split the pot, with odd chips awarded clockwise after the dealer.
6. Each player sees only their own cards. The UI shows the board, chip counts, current bet, turn, dealer and blind badges, plus the latest action and hand result. The host deals the next hand; dealer/blinds rotate. Leaving during a game folds the hand and forfeits the remaining stack. If fewer than two players have chips, create a new lobby to play again.

This is intentionally an in-memory casual-play prototype. Restarting the API clears players, lobbies and games. Player IDs identify seats; they are **not authentication** or proof of ownership. No persistent bankroll, account recovery, turn timers or production deployment infrastructure is included. Rendering uses initials as placeholder profile pictures and deliberately minimal CSS.

## Validation

```sh
dotnet test PokerGame.slnx
npm run build --prefix Frontend
cd Frontend
npm run test:e2e
```

See [the API contract](docs/api.md) and [test plan](docs/test-plan.md).
