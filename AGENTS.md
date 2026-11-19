# Project guide

Update this guide alongside project changes. Record verified startup and test commands, environment-specific workarounds, and behavior changes that affect future work.

## Architecture

- `Core/Domain`: cards, deck, ranks, suits and player actions.
- `Core/Game`: Texas Hold’em rules, game session, turn progression and payouts. Keep this layer independent of ASP.NET and React.
- `Api/Contracts`: request records, player identity and application errors.
- `Api/Services`: the in-memory `Arena` application service. Partial files separate lobby membership, game lifecycle, room storage and per-player projections. The shared lock protects the maps of rooms, game sessions and browser seats, including the single active game check. A room ID doubles as its game ID. `GameSession.SessionId` identifies the session, including across hands.
- `Api/Endpoints`: HTTP registration, lobby discovery and game commands. Endpoints delegate rules to the service.
- `Api/Hubs`: SignalR connection lifecycle and event-driven updates at `/api/live`. `JoinRoom(roomId)` resolves the browser seat from the HttpOnly cookie and validates membership. `ArenaUpdates` sends a separate projection to each connection so opponents' hole cards never appear in another player's payload. Preserve that boundary; do not broadcast a player's private snapshot to a group.
- `Frontend/src/hooks`: application/session state and SignalR connection/retry lifecycle.
- `useRoomConnection` retries a dropped transport for the same seat. Stepping away folds the hand and sits the seat out while preserving its stack. Abandonment releases the seat and transfers a running game's host role to a randomly selected remaining member. Both actions return to lobbies silently on success. Spectators with zero chips can receive updates without blocking play. Keep involuntary removal and failed leave requests visible.
- `Frontend/src/components`: lobby browser, room/game controls, dialogs and poker table. `App.tsx` composes these views. `api.ts` handles HTTP errors and seat headers; `types.ts` describes camelCase responses.
- `Frontend/src/style.css` owns the charcoal and burgundy theme and responsive layout. The table uses an oval above 900px and a seat grid at 900px and below. When changing table layout, check a full 10-seat table on both sides of the breakpoint and at 320px. Keep background suits decorative and preserve accessible action names when styling.
- `Tests/Core`: xUnit game and evaluator tests. `Tests/Api`: opt-in HTTP and SignalR integration tests using `WebApplicationFactory<Api.Program>`.
- `Frontend/e2e`: opt-in Playwright integration scenarios with independent browser contexts.

This is an in-memory casual-play prototype. API restarts clear all seats, rooms and games. Public player IDs do not grant access. The `poker.seat` HttpOnly, SameSite=Strict cookie authenticates an anonymous browser seat for HTTP and SignalR. Keep host, membership and turn checks on the server. Nicknames and the single `poker.game` record persist in local storage. `GET /api/players/me` is authoritative for the saved game. The UI clears finished game IDs from local and session storage, including from the lobby browser, while retaining the seat cookie for future games.

The [user stories and use cases](docs/user-stories-and-use-cases.md) document current gameplay and include PlantUML for the proposed SOLID refactor. The package boundaries and types in those diagrams are a plan. The implementation currently uses `Api.Services.Arena`.

## Conventions

- C#: file-scoped namespaces, four-space indentation, PascalCase types/methods/properties, `_camelCase` private fields and camelCase parameters/locals. Existing code uses records for contracts, nullable references, implicit usings in the API/core and primary constructors where useful.
- Put new C# types in files matching their names and related concern directories. Keep domain rules out of endpoints/hubs. Preserve atomic state changes under the service lock; never await network I/O while holding it.
- TypeScript/React: strict types, functional components and hooks, PascalCase component files, camelCase functions/state, single-quoted strings and semicolons. Use type-only imports for interfaces and clean up effects, connections and retry timers.
- Preserve accessible labels, roles and button names used by browser tests. Keep shared state in hooks and render concerns in components. Avoid duplicating HTTP/SignalR handling in views.
- The raise slider and exact number input share the chips the player puts in now. `game.minimumRaiseChips` is the viewer's server-calculated minimum contribution for a legal raise, including the call, or null when raising is unavailable. The action endpoint takes the increment above the current bet. A short all-in can raise without lowering the full-raise increment and reopens betting for a player who previously acted only once that player faces a cumulative full raise.
- In the raise dialog, show the minimum full raise-to bet and the chips it takes from the stack, including the call. Use an explicit all-in confirmation when the stack only covers a short raise.
- HTTP JSON and SignalR JSON both use camelCase properties and enum strings. Validate changes to both transports together.
- `game.lastResult.board` keeps the completed hand's public cards visible until the next deal. Project these as card labels in both transports, using the same formatter as the active board.
- Keep secrets out of code and logs. Lobby passwords remain salted PBKDF2 hashes and never appear in responses.

## Development and validation

Requires .NET SDK 10 and Node.js 24. In the prepared cloud environment, source `/workspace/.setup/poker-env.sh` before .NET commands. Use the existing checkout; do not create a worktree unless explicitly requested.

On Windows, use `npm.cmd` and `npx.cmd` in PowerShell if script execution policy blocks the `.ps1` shims. Use `curl.exe` for the health check. The API uses ASP.NET's console logging on Windows and disables the Windows Event Log provider so a restricted account can serve SignalR errors.

From the repository root:

```sh
dotnet restore PokerGame.slnx
npm ci --prefix Frontend
dotnet build PokerGame.slnx --configuration Release --no-restore
npm run build --prefix Frontend
# Fast CI suite: integration tests are deliberately excluded.
dotnet test PokerGame.slnx --configuration Release --no-build --filter 'Category!=Integration'
# Full backend suite, including HTTP/SignalR integration tests.
dotnet test PokerGame.slnx
# API integration tests only.
dotnet test PokerGame.slnx --filter 'Category=Integration'
# Browser integration suite (not run in CI).
npm run test:integration --prefix Frontend
```

Playwright starts its own API on port 5080 and Vite on 5180. Stop a development API using 5080 before running it. It uses `/usr/bin/chromium` when available, or `PLAYWRIGHT_CHROMIUM_EXECUTABLE`; otherwise run `npx playwright install --with-deps chromium` from `Frontend` first. `npm run test:e2e` remains an alias for the integration suite.

If Playwright reports a missing browser revision on Windows, run `npx.cmd playwright install chromium` from `Frontend`, or set `$env:PLAYWRIGHT_CHROMIUM_EXECUTABLE` to the full path of an installed Chromium browser such as Edge. If browser tests finish but teardown stalls in a restricted shell, check `netstat -ano` for listeners on 5080 and 5180. Windows process-tree cleanup via `taskkill.exe` may be denied. Stop only the processes started for that run, then check the ports again before retrying.

For interactive development, run `npm run dev --prefix Frontend` from the repository root. This starts the API on 5080, waits for the seat endpoint, and then starts Vite on 5173. Stop it with Ctrl+C to shut down both services. In cloud tasks, source `/workspace/.setup/poker-env.sh` first so the .NET SDK is available.

Vite hot-reloads the frontend while the API started by `dotnet run` keeps its current build. After backend changes, restart the combined dev command. The launcher refuses to start if 5080 already serves HTTP and checks that unauthenticated `/api/players/me` returns 401 before starting Vite. A 404 from that route indicates an older API, even when `/api/health` reports success. The nickname flow verifies its cookie with `/api/players/me` before opening lobbies.

If managing the API separately, use `dotnet run --project Api/Api.csproj --no-launch-profile --urls http://127.0.0.1:5080` and `npm run dev:frontend --prefix Frontend`. Do not run the combined command alongside a separately started API on the same port.

Vite proxies `/api`, including SignalR, to the API. Verify `curl -fsS http://127.0.0.1:5173/api/health` returns `{"status":"ok"}`. No database or message broker is required.

For a loopback-only run, use `npm run dev --prefix Frontend -- --host 127.0.0.1`. Open `http://127.0.0.1:5173` in two browser profiles or in a regular window and a private window. Register two nicknames, create a lobby in one session, and join it with the password in the other. The host can start once both sessions have joined.

Frontend code uses Prettier: run `npm run format --prefix Frontend` after edits and `npm run format:check --prefix Frontend` to verify. Keep JSX and handlers multiline; keep HTTP mutations in hooks, not view markup. `useLobbyList` owns discovery polling; `useRoomConnection` owns live connections; `usePokerArena` coordinates session state and commands.

CI builds the entire backend and frontend and runs backend tests excluding the `Category=Integration` trait. API integration tests carry that trait; browser tests carry `@integration` and are invoked only explicitly. Keep these tests runnable locally rather than disabling assertions or marking them permanently skipped.
