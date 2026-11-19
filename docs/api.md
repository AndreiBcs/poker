# API contract

All routes are under `/api`. Requests and responses are JSON. `POST /players` sets the `poker.seat` cookie using `HttpOnly`, `SameSite=Strict`, and `Secure` on HTTPS. The browser sends it automatically for same-origin HTTP and SignalR requests. The server derives the player ID from the cookie. SignalR clients invoke `JoinRoom(roomId)` after connecting. Browser mutations and hub requests must have the same origin as the API frontend.

| Method | Route | Body / result |
| --- | --- | --- |
| GET | `/health` | `{ status: "ok" }` |
| POST | `/players` | `{ nickname }` → `{ id, nickname }`; reuses a valid cookie and updates the nickname outside an active membership |
| GET | `/players/me` | `{ player, savedGame }`; an active saved entry is `{ gameId, sessionId, name }`. `sessionId` is null in the waiting room. |
| GET | `/lobbies` | Open lobby summaries; no password material |
| POST | `/lobbies` | `{ name, password, maxPlayers, smallBlind?, bigBlind? }` → `{ id }`; host automatically joins |
| GET | `/lobbies/{id}` | Member-specific room and game snapshot |
| POST | `/lobbies/{id}/join` | `{ password }`; validates password, capacity and membership |
| POST | `/lobbies/{id}/step-away` | Folds the current hand and sits the seat out while preserving its remaining chips and membership |
| POST | `/lobbies/{id}/leave` | Permanently leaves or abandons the saved seat and its stack. Transfers a running game's host role randomly or closes an empty room. |
| GET | `/games/{id}/sessions/{sessionId}` | Validates the saved pair, seat membership, and that the game is ongoing. Returns the room snapshot. |
| DELETE | `/lobbies/{id}/players/{target}` | Host-only kick before start |
| POST | `/lobbies/{id}/start` | Host starts game or deals next completed hand |
| POST | `/lobbies/{id}/actions` | `{ action: "fold" \| "check" \| "call" \| "raise", amount?: number }` |
| SignalR | `/live` | Invoke `JoinRoom(roomId)` using the seat cookie. Receives `State(room)` and `Removed(message)`. Commands use HTTP. |

For a raise, `amount` is the increase above `game.currentBet`. `game.minRaise` is the last full raise increment (the big blind before the first full raise of the round). `game.minimumRaiseChips` is the minimum contribution for the viewer's current turn, including any amount to call. It is null when the viewer cannot raise. A short all-in can raise without reducing the full-raise increment.

Successful mutations return 204 except player/lobby creation (200 with JSON). Invalid input and actions return 400. Missing or expired seat cookies return 401. Forbidden membership and host operations or incorrect passwords return 403. Missing resources and expired saved sessions return 404. Domain errors return `{ error: string }`.

Names are trimmed and length-limited; nicknames are not unique. Capacity is 2–10. Starting chips are 2,000; default blinds are 25/50. Small blind must be 1–1,000; big blind must be at least the small blind and at most 2,000. A player can occupy only one lobby at a time. Requests are serialized around arena mutations so concurrent joins cannot overfill a lobby.

Room status is `waiting`, `connecting`, `playing`, `paused`, or `finished`. Starting waits for every seated player to connect. During play, only participants who can affect the hand or are eligible for the next deal are required to be connected. Sitting-out seats and busted spectators can return while the game is ongoing. The SignalR hub emits member-specific `State` snapshots after changes. A removed member receives `Removed`. Connections for the same seat are reference-counted. The browser retries a dropped connection and rechecks HTTP membership before joining again. The lobby browser checks for changes to the saved game and clears it when the server reports it has ended. Both transports serialize properties and enums in camelCase.

Game phases are `waitingForPlayers`, `preFlop`, `flop`, `turn`, `river`, `showdown` (settlement currently returns directly to waiting). Until the next deal, `lastResult.board` contains the community cards dealt during the completed hand. Cards are text labels such as `A♠`. Opponents' `holeCards` are always empty; `cardCount` supports card backs. No IP addresses are exposed. A room revision and latest notice support lightweight action notifications; there is no durable event log.
