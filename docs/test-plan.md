# Texas Hold’em validation

Run `dotnet test PokerGame.slnx`, `npm run build --prefix Frontend`, and `npm run test:integration --prefix Frontend`.

Core tests cover card labels, evaluator rank conventions, 52 unique shuffled cards, deck reset, private dealing, heads-up blinds, turn validation, invalid raises, folds/payouts, ten-seat betting through showdown, side pots and chip conservation.

API integration tests use a real ASP.NET test host and SignalR clients. They cover nickname/capacity validation, auto-joined creator, password rejection, full lobbies, forbidden non-member access, host-only start/kick, host transfer, empty-room cleanup, concurrent joins, all-player connection barrier, private card projections, invalid turn/raise actions, folding, next hand, disconnect pause and reconnect.

The Playwright test uses two independent browser contexts to create and join a lobby, check host controls and private cards, and play through showdown. It verifies refresh recovery, retry after a socket drop, a failed step-away that keeps the player seated, reopening the saved game in a fresh tab, abandonment, and automatic cleanup in another player's lobby browser when the game ends. API tests cover cookie ownership, saved session ID validation, the one-active-game restriction, host transfer, and reconnecting as a busted spectator.

Manual checks for future UI work:

- Fill a table with up to ten independent sessions and check the seat grid at narrow widths.
- Disconnect a player's network and restore it; confirm action pause/resume.
- Play several full hands with raises and short-stack calls; confirm chip totals and host next-hand control.
- Close/reopen the app and confirm nickname persistence. Restart API and confirm stale-seat recovery when browsing/creating again.

No account, authentication, RabbitMQ or SQLite scenarios apply to this version.

API tests are marked `Category=Integration`; browser scenarios are tagged `@integration`. CI excludes both suites for now. The full local backend command still runs all API tests, and `npm run test:integration --prefix Frontend` runs the browser scenario.
