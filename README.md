# Poker

## Flow and rules

1. Set a nickname (1–24 characters). The nickname and saved game IDs are stored locally. An HttpOnly cookie identifies the seat across tabs and browser restarts. Use separate browser profiles to play independent seats.
2. Browse open lobbies or create one with a name, password and capacity of 2–10. The creator immediately joins as host. Lobby passwords are salted and hashed in memory and never returned in API responses.
3. Joining requires the correct password and an available seat. Waiting-room membership updates live. The host can kick guests or start with at least two players. Leaving transfers the host role to the next player; the last departure removes the lobby.
4. Starting closes the lobby to new joins. Cards are dealt only after all members have a live SignalR connection. A dropped connection pauses actions until reconnection; refreshing automatically reconnects. A player who cannot reconnect can explicitly leave. If someone leaves while the initial connection barrier is pending, a one-player lobby reopens.
5. The server shuffles, deals private cards, posts blinds and enforces turns. Players can fold, check, call or raise. A raise amount is an increment above the current table bet, not the total contribution. Calling with insufficient chips goes all-in. Side pots are settled by contribution; ties split the pot, with odd chips awarded clockwise after the dealer.
6. Each player sees only their own cards. The UI shows the board, chip counts, current bet, turn, dealer and blind badges, plus the latest action and hand result. The host deals the next hand. Dealer and blinds rotate. A player with no chips can still reconnect and spectate while the game continues.
7. Leaving an active table folds the hand and saves the seat and remaining chips. Use **Reconnect** in the lobby browser to return or **Abandon** to release the seat and its remaining chips. A browser seat can have one active game. Abandon it before creating or joining another. If the host abandons an ongoing game, a random remaining member becomes host. When fewer than two players have chips after a hand, the game ends and its saved IDs are cleared automatically.

This is an in-memory casual-play prototype. Restarting the API clears players, lobbies and games. The browser cookie authenticates an anonymous seat. The table uses initials for player avatars and adapts its layout to narrow screens.

## Development

From the repository root, install the frontend dependencies and start the API and frontend together.

```sh
npm ci --prefix Frontend
npm run dev --prefix Frontend -- --host 127.0.0.1
```

Open `http://127.0.0.1:5173` in two separate browser profiles, or use a regular window and a private window. Choose a nickname in each, create a lobby in one, and join it with the password in the other. The host can then start the game. In PowerShell, use `npm.cmd` if execution policy blocks `npm`.

See [AGENTS.md](AGENTS.md) for architecture, conventions, startup commands and test categories. Live state uses the SignalR hub at `/api/live`; HTTP handles lobby/game commands.

## Validation

```sh
dotnet test PokerGame.slnx
npm run build --prefix Frontend
cd Frontend
npm run test:integration
```

See [the API contract](docs/api.md), [test plan](docs/test-plan.md), and [user stories, use cases, and proposed architecture](docs/user-stories-and-use-cases.md).

CI builds backend/frontend and runs `dotnet test PokerGame.slnx --filter 'Category!=Integration'`. API and browser integration suites remain opt-in locally.
