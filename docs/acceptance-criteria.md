# Acceptance criteria

- Core builds as a .NET library without ASP.NET, authentication or persistence references.
- Only Texas Hold’em is available.
- A nickname is required before joining/creating, and persists locally.
- Open lobbies show names and occupancy; joining requires the configured password.
- Creation accepts a name, password and maximum player count of 2–10 and joins the creator automatically.
- Waiting rooms show live membership; only the host can kick guests or start with at least one guest.
- No cards are dealt until all participants connect; disconnects pause actions and refresh reconnects.
- The server controls dealing, blinds, turns, legal actions, private cards and payouts.
- The frontend shows seats around a table, placeholder avatars, nicknames, cards, chip counts, turn and result notices, and basic action dialogs.
- Tests cover the engine, HTTP/WebSocket integration and the two-player browser flow.
