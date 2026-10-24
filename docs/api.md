# API contract

All routes are under `/api`. Requests and responses are JSON. Mutation and member-state requests supply `X-Player-Id` from `POST /players`. This ID is a seat identifier, not an authentication credential. SignalR clients supply the room and player IDs to `JoinRoom(roomId, playerId)` after connecting.

| Method | Route | Body / result |
| --- | --- | --- |
| GET | `/health` | `{ status: "ok" }` |
| POST | `/players` | `{ nickname }` → `{ id, nickname }` |
| GET | `/players/{id}` | Current player, or 404 after server restart |
| GET | `/lobbies` | Open lobby summaries; no password material |
| POST | `/lobbies` | `{ name, password, maxPlayers, smallBlind?, bigBlind? }` → `{ id }`; host automatically joins |
| GET | `/lobbies/{id}` | Member-specific room and game snapshot |
| POST | `/lobbies/{id}/join` | `{ password }`; validates password, capacity and membership |
| POST | `/lobbies/{id}/leave` | Leaves room; transfers host or closes empty room |
| DELETE | `/lobbies/{id}/players/{target}` | Host-only kick before start |
| POST | `/lobbies/{id}/start` | Host starts game or deals next completed hand |
| POST | `/lobbies/{id}/actions` | `{ action: "fold" \| "check" \| "call" \| "raise", amount?: number }` |
| SignalR | `/live` | Invoke `JoinRoom(roomId, playerId)`; receive `State(room)` and `Removed(message)` events; commands use HTTP |

Successful mutations return 204 except player/lobby creation (200 with JSON). Invalid input/actions return 400, forbidden membership/host operations or incorrect passwords return 403, and missing resources return 404. Domain errors return `{ error: string }`; malformed JSON uses ASP.NET's normal 400 response.

Names are trimmed and length-limited; nicknames are not unique. Capacity is 2–10. Starting chips are 2,000; default blinds are 25/50. Small blind must be 1–1,000; big blind must be at least the small blind and at most 2,000. A player can occupy only one lobby at a time. Requests are serialized around arena mutations so concurrent joins cannot overfill a lobby.

Room status is `waiting`, `connecting`, `playing`, or `paused`. Starting requests close a lobby to new joins, then wait for all live connections. The SignalR hub emits `State` with a member-specific room snapshot after membership, connection or game changes; there is no periodic socket polling. A removed member receives `Removed` with a message and is unsubscribed from further snapshots. Multiple connections for the same seat are reference-counted, so closing one does not disconnect another. Membership is checked by `JoinRoom` and before every snapshot. Each connection may join once; failed membership checks do not bind it. The frontend retries disconnected hubs and revalidates HTTP membership before joining again. An API restart requires registering a new ID. Both transports serialize properties and enums in camelCase.

Game phases are `waitingForPlayers`, `preFlop`, `flop`, `turn`, `river`, `showdown` (settlement currently returns directly to waiting, retaining `lastResult`). Cards are text labels such as `A♠`. Opponents' `holeCards` are always empty; `cardCount` supports card backs. No IP addresses are exposed. A room revision and latest notice support lightweight action notifications; there is no durable event log.
