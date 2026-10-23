# Poker Arena

A small, anonymous Texas Hold’em app, refactored from the `Production` branch. Choose a nickname, create or join a password-protected lobby, and play with 2–10 people. No accounts, authentication, database, or message broker are required.

## Projects

- `Core/`: .NET class library containing cards, deck, hand evaluation, betting, blinds, turn order, side pots and hand settlement. It has no ASP.NET or persistence dependencies.
- `Api/`: ASP.NET Core HTTP API and WebSocket snapshots. Owns in-memory nicknames, lobbies, password checks, host controls, connections and private state projections.
- `Frontend/`: Vite + React + TypeScript. Minimal forms, waiting room, poker table, action dialogs and notices.
- `Core.Tests/`, `Api.Tests/`: engine regression tests and HTTP/WebSocket integration tests.
- `Frontend/e2e/`: a two-browser-context test of the actual UI.

The former `Source` Razor app, cookie authentication, account services, SQLite migrations and Five Card Draw have been removed. Texas Hold’em is the only game mode.

## Run locally

Requirements: .NET 10 SDK and Node.js 22.12+ (Node 24 recommended).

```sh
dotnet restore PokerGame.slnx
npm ci --prefix Frontend
```

Run the API in one terminal:

```sh
dotnet run --project Api --no-launch-profile --urls http://127.0.0.1:5080
```

Run the frontend in another:

```sh
cd Frontend
npm run dev
```

Vite listens on port 5173 and proxies `/api` HTTP and WebSocket requests to port 5080. No CORS or authentication setup is needed. The API health endpoint is `GET /api/health`.

In the cloud machine the SDK is installed in `/workspace/.dotnet`. If `dotnet` is not on your shell path:

```sh
export DOTNET_ROOT=/workspace/.dotnet
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME=/workspace/.dotnet-home
export NUGET_PACKAGES=/workspace/.nuget
export npm_config_cache=/workspace/.npm
```

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

Playwright starts an isolated API on port 5080 and Vite on port 5180; stop development servers on those ports first. It uses `/usr/bin/chromium` if present, or `PLAYWRIGHT_CHROMIUM_EXECUTABLE` when set. Otherwise install its browser once with `npx playwright install chromium`.

See [the API contract](docs/api.md) and [test plan](docs/test-plan.md).
