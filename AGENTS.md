# Project guide

## Architecture

- `Core/Domain`: cards, deck, ranks, suits and player actions.
- `Core/Game`: Texas Hold’em rules, game session, turn progression and payouts. Keep this layer independent of ASP.NET and React.
- `Api/Contracts`: request records, player identity and application errors.
- `Api/Services`: the in-memory `Arena` application service. Partial files separate lobby membership, game lifecycle, room storage and per-player state projections. The shared lock protects all room/game mutations; preserve its scope when changing state.
- `Api/Endpoints`: HTTP registration, lobby discovery and game commands. Endpoints delegate rules to the service.
- `Api/Hubs`: SignalR connection lifecycle and event-driven updates at `/api/live`. `JoinRoom` validates membership. `ArenaUpdates` sends a separate projection to each connection so opponents' hole cards never appear in another player's payload. Preserve that boundary; do not broadcast a player's private snapshot to a group.
- `Frontend/src/hooks`: application/session state and SignalR connection/retry lifecycle.
- `Frontend/src/components`: lobby browser, room/game controls, dialogs and poker table. `App.tsx` composes these views. `api.ts` handles HTTP errors and seat headers; `types.ts` describes camelCase responses.
- `Tests/Core`: xUnit game and evaluator tests. `Tests/Api`: opt-in HTTP and SignalR integration tests using `WebApplicationFactory<Api.Program>`.
- `Frontend/e2e`: opt-in Playwright integration scenarios with independent browser contexts.

This is an in-memory casual-play prototype. API restarts clear all seats and rooms. Player IDs are seat identifiers, not authentication. Keep host/membership/turn checks on the server. Nicknames persist in local storage; seats and room IDs persist per tab in session storage.

## Conventions

- C#: file-scoped namespaces, four-space indentation, PascalCase types/methods/properties, `_camelCase` private fields and camelCase parameters/locals. Existing code uses records for contracts, nullable references, implicit usings in the API/core and primary constructors where useful.
- Put new C# types in files matching their names and related concern directories. Keep domain rules out of endpoints/hubs. Preserve atomic state changes under the service lock; never await network I/O while holding it.
- TypeScript/React: strict types, functional components and hooks, PascalCase component files, camelCase functions/state, single-quoted strings and semicolons. Use type-only imports for interfaces and clean up effects, connections and retry timers.
- Preserve accessible labels, roles and button names used by browser tests. Keep shared state in hooks and render concerns in components. Avoid duplicating HTTP/SignalR handling in views.
- HTTP JSON and SignalR JSON both use camelCase properties and enum strings. Validate changes to both transports together.
- Keep secrets out of code and logs. Lobby passwords remain salted PBKDF2 hashes and never appear in responses.

## Development and validation

Requires .NET SDK 10 and Node.js 24. In the prepared cloud environment, source `/workspace/.setup/poker-env.sh` before .NET commands. Use the existing checkout; do not create a worktree unless explicitly requested.

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

For interactive development, run `npm run dev --prefix Frontend` from the repository root. This starts the API on 5080, waits for its health check, and then starts Vite on 5173. Stop it with Ctrl+C to shut down both services. In cloud tasks, source `/workspace/.setup/poker-env.sh` first so the .NET SDK is available.

If managing the API separately, use `dotnet run --project Api/Api.csproj --no-launch-profile --urls http://127.0.0.1:5080` and `npm run dev:frontend --prefix Frontend`. Do not run the combined command alongside a separately started API on the same port.

Vite proxies `/api`, including SignalR, to the API. Verify `curl -fsS http://127.0.0.1:5173/api/health` returns `{"status":"ok"}`. No database or message broker is required.

Frontend code uses Prettier: run `npm run format --prefix Frontend` after edits and `npm run format:check --prefix Frontend` to verify. Keep JSX and handlers multiline; keep HTTP mutations in hooks, not view markup. `useLobbyList` owns discovery polling; `useRoomConnection` owns live connections; `usePokerArena` coordinates session state and commands.

CI builds the entire backend and frontend and runs backend tests excluding the `Category=Integration` trait. API integration tests carry that trait; browser tests carry `@integration` and are invoked only explicitly. Keep these tests runnable locally rather than disabling assertions or marking them permanently skipped.
