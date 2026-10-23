# Texas Hold’em validation

Run `dotnet test PokerGame.slnx`, `npm run build --prefix Frontend`, and `npm run test:e2e --prefix Frontend`.

Core tests cover card labels, evaluator rank conventions, 52 unique shuffled cards, deck reset, private dealing, heads-up blinds, turn validation, invalid raises, folds/payouts, ten-seat betting through showdown, side pots and chip conservation.

API integration tests use a real ASP.NET test host and WebSocket clients. They cover nickname/capacity validation, auto-joined creator, password rejection, full lobbies, forbidden non-member access, host-only start/kick, host transfer, empty-room cleanup, concurrent joins, all-player connection barrier, private card projections, invalid turn/raise actions, folding, next hand, disconnect pause and reconnect.

The Playwright test uses two independent browser contexts to set nicknames, create a ten-seat-capacity lobby, reject a wrong password, join, kick and rejoin, start, inspect private card backs, fold, deal again, refresh/reconnect and leave. It also checks browser errors and nickname persistence.

Manual checks for future UI work:

- Fill a table with up to ten independent sessions; check seats at narrow widths (table scrolls horizontally).
- Disconnect a player's network and restore it; confirm action pause/resume.
- Play several full hands with raises and short-stack calls; confirm chip totals and host next-hand control.
- Close/reopen the app and confirm nickname persistence. Restart API and confirm stale-seat recovery when browsing/creating again.

No account, authentication, RabbitMQ or SQLite scenarios apply to this version.
