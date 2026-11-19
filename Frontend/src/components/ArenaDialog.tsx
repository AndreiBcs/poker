import type { PokerArena } from '../hooks/usePokerArena';

export function ArenaDialog({ arena }: { arena: PokerArena }) {
  const {
    room,
    modal,
    setModal,
    error,
    busy,
    raiseChips,
    setRaiseChips,
    run,
    joinLobby,
    leaveLobby,
    abandonGame,
    command,
    host,
    game,
    toCall,
    stack,
    minimumRaiseChips,
    canAct,
  } = arena;
  const selectedChips = Number(raiseChips);
  const sliderChips = Math.min(stack, Math.max(0, Math.round(selectedChips || 0)));
  const raiseAmount = selectedChips - toCall;
  const fullRaiseChips = toCall + (game?.minRaise ?? 0);
  const shortAllInOnly = minimumRaiseChips !== null && minimumRaiseChips < fullRaiseChips;
  const canRaise =
    canAct &&
    minimumRaiseChips !== null &&
    raiseChips !== '' &&
    Number.isInteger(selectedChips) &&
    selectedChips <= stack &&
    selectedChips >= minimumRaiseChips;
  return (
    <>
      {modal && (
        <div className="backdrop">
          <div role="dialog" aria-modal="true" aria-labelledby="dialog-title" className="dialog">
            {modal.kind === 'join' && (
              <form
                key={modal.lobby.id}
                onSubmit={(e) => {
                  e.preventDefault();
                  const password = new FormData(e.currentTarget).get('password');
                  const id = modal.lobby.id;
                  void run(() => joinLobby(id, password));
                }}
              >
                <h2 id="dialog-title">Join {modal.lobby.name}</h2>
                <label>
                  Lobby password
                  <input autoFocus type="password" name="password" required maxLength={100} />
                </label>
                <button className="primary" disabled={busy}>
                  Join lobby
                </button>
              </form>
            )}
            {modal.kind === 'raise' && (
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  if (canRaise) {
                    void run(() => command('/actions', { action: 'raise', amount: raiseAmount }));
                  }
                }}
              >
                <h2 id="dialog-title">Raise the bet</h2>
                <div className="raise-slider">
                  <div className="raise-slider-label">
                    <label htmlFor="raise-chips">Chips to put in</label>
                    <input
                      className="raise-exact"
                      type="number"
                      inputMode="numeric"
                      aria-label="Exact chips to put in"
                      aria-describedby="raise-minimum"
                      min={0}
                      max={stack}
                      step={1}
                      value={raiseChips}
                      onChange={(e) => setRaiseChips(e.currentTarget.value)}
                    />
                  </div>
                  <input
                    autoFocus
                    id="raise-chips"
                    type="range"
                    min={0}
                    max={stack}
                    step={1}
                    value={sliderChips}
                    aria-describedby="raise-minimum"
                    onChange={(e) => setRaiseChips(e.currentTarget.value)}
                  />
                  <div className="raise-slider-limits" aria-hidden="true">
                    <span>0</span>
                    <span>All in ({stack})</span>
                  </div>
                </div>
                {game && (
                  <p id="raise-minimum" className="muted">
                    {minimumRaiseChips === null
                      ? 'Raising is unavailable on this turn.'
                      : `Minimum full raise to ${game.currentBet + game.minRaise} takes ${fullRaiseChips} chips from your stack${toCall > 0 ? `, including ${toCall} to call` : ''}.`}
                    {shortAllInOnly && (
                      <>
                        <br />
                        Your {stack}-chip stack qualifies for a short all-in.
                      </>
                    )}
                  </p>
                )}
                {canRaise && game && <p>Raise to {game.currentBet + raiseAmount}</p>}
                <button className="primary" disabled={!canRaise}>
                  {shortAllInOnly ? 'Confirm all-in' : 'Confirm raise'}
                </button>
              </form>
            )}
            {modal.kind === 'leave' && (
              <>
                <h2 id="dialog-title">Leave this lobby?</h2>
                <p>
                  {room?.status === 'finished'
                    ? 'Return to the lobby browser.'
                    : room?.game
                      ? 'Leaving folds this hand. You can reconnect from the lobby browser with your remaining chips.'
                      : host
                        ? 'A remaining player becomes host. An empty lobby closes.'
                        : 'You can rejoin with the lobby password.'}
                </p>
                <button disabled={busy} onClick={() => void run(leaveLobby)}>
                  Leave
                </button>
              </>
            )}
            {modal.kind === 'abandon' && (
              <>
                <h2 id="dialog-title">Abandon saved game?</h2>
                <p>
                  This releases your seat and any remaining chips. You can then start a new game.
                </p>
                <button disabled={busy} onClick={() => void run(abandonGame)}>
                  Abandon game
                </button>
              </>
            )}
            {error && <p role="alert">{error}</p>}
            <button disabled={busy} onClick={() => setModal(null)}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </>
  );
}
