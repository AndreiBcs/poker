import type { PokerArena } from '../hooks/usePokerArena';

export function ArenaDialog({ arena }: { arena: PokerArena }) {
  const {
    room,
    modal,
    setModal,
    error,
    busy,
    raise,
    setRaise,
    run,
    joinLobby,
    leaveLobby,
    command,
    host,
    game,
    maxRaise,
  } = arena;
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
                <button disabled={busy}>Join lobby</button>
              </form>
            )}
            {modal.kind === 'raise' && (
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  void run(() => command('/actions', { action: 'raise', amount: raise }));
                }}
              >
                <h2 id="dialog-title">Raise the bet</h2>
                <p>This adds to the current table bet of {game?.currentBet}.</p>
                <label>
                  Raise by
                  <input
                    autoFocus
                    type="number"
                    required
                    min={game?.minRaise}
                    max={maxRaise}
                    value={raise}
                    onChange={(e) => setRaise(Number(e.target.value))}
                  />
                </label>
                <button disabled={busy}>Confirm raise</button>
              </form>
            )}
            {modal.kind === 'leave' && (
              <>
                <h2 id="dialog-title">Leave this lobby?</h2>
                <p>
                  {room?.startRequested
                    ? 'Leaving folds your hand and forfeits your remaining stack.'
                    : host
                      ? 'The next player becomes host. An empty lobby closes.'
                      : 'You can rejoin with the lobby password.'}
                </p>
                <button disabled={busy} onClick={() => void run(leaveLobby)}>
                  Leave
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
