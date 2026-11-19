import type { PokerArena } from '../hooks/usePokerArena';
import { PokerTable } from './PokerTable';

export function RoomView({ arena }: { arena: PokerArena }) {
  const {
    player,
    roomId,
    room,
    setModal,
    busy,
    live,
    setRaiseChips,
    run,
    command,
    host,
    game,
    myTurn,
    toCall,
    minimumRaiseChips,
    canAct,
    spectating,
  } = arena;
  return (
    <>
      {roomId && !room && <p role="status">Connecting to your lobby…</p>}
      {roomId && room && (
        <>
          <section className="room-panel">
            <div className="row room-heading">
              <h2>{room.name}</h2>
              <button disabled={busy} onClick={() => setModal({ kind: 'leave' })}>
                Leave lobby
              </button>
            </div>
            <p className="room-status" role="status">
              {room.status === 'finished'
                ? 'Game over'
                : !live
                  ? 'Reconnecting…'
                  : room.status === 'waiting'
                    ? 'Waiting room'
                    : room.status === 'connecting'
                      ? 'Waiting for every player to connect…'
                      : room.status === 'paused'
                        ? 'Game paused: waiting for players to reconnect.'
                        : 'Game in progress'}
            </p>
            <p className="muted" aria-live="polite">
              {room.notice}
            </p>
            {!room.startRequested && (
              <>
                <p>
                  {room.players.length}/{room.maxPlayers} players · Blinds {room.smallBlind}/
                  {room.bigBlind}
                </p>
                <ul className="entry-list">
                  {room.players.map((p) => (
                    <li key={p.id}>
                      <div>
                        <strong>
                          {p.nickname}
                          {p.id === room.ownerId ? ' (host)' : ''}
                        </strong>
                        <small>{p.connected ? 'Connected' : 'Connecting'}</small>
                      </div>
                      {host && p.id !== player?.id && (
                        <button
                          disabled={busy}
                          onClick={() =>
                            void run(() => command(`/players/${p.id}`, undefined, 'DELETE'))
                          }
                        >
                          Kick {p.nickname}
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
                {host ? (
                  <button
                    className="primary"
                    disabled={busy || room.players.length < 2 || !live}
                    onClick={() => void run(() => command('/start'))}
                  >
                    Start game
                  </button>
                ) : (
                  <p>Waiting for the host to start.</p>
                )}
                {room.players.length < 2 && <p>Invite at least one other player to start.</p>}
              </>
            )}
          </section>
          {game && (
            <section className="game-panel">
              <PokerTable game={game} playerId={player!.id} room={room} />
              {spectating && <p className="muted">Spectating · You have no chips left.</p>}
              {game.lastResult && (
                <p className="hand-result" role="status">
                  {game.lastResult.winners.map((w) => `${w.username} won ${w.winnings}`).join(', ')}{' '}
                  · {game.lastResult.handDescription}
                </p>
              )}
              {game.phase === 'waitingForPlayers' ? (
                room.status === 'finished' ? (
                  <p>Leave and create a new lobby to play again.</p>
                ) : game.players.filter((p) => p.chips > 0 && !p.isSittingOut).length < 2 ? (
                  <p>Waiting for at least two players with chips to return to the table.</p>
                ) : host ? (
                  <button
                    className="primary"
                    disabled={busy || room.status !== 'playing'}
                    onClick={() => void run(() => command('/start'))}
                  >
                    Deal next hand
                  </button>
                ) : (
                  <p>Waiting for the host to deal the next hand.</p>
                )
              ) : (
                !spectating && (
                  <div className="row game-actions" aria-label="Game actions">
                    <span className={myTurn ? 'your-turn' : 'muted'}>
                      {myTurn
                        ? 'Your turn'
                        : `Waiting for ${game.players.find((p) => p.id === game.currentTurnUserId)?.nickname || 'players'}`}
                    </span>
                    {(['fold', 'check', 'call'] as const).map((action) => (
                      <button
                        key={action}
                        disabled={
                          !canAct ||
                          (action === 'check' && toCall > 0) ||
                          (action === 'call' && toCall <= 0)
                        }
                        onClick={() => void run(() => command('/actions', { action }))}
                      >
                        {action[0].toUpperCase() + action.slice(1)}
                      </button>
                    ))}
                    <button
                      className="primary"
                      disabled={!canAct || minimumRaiseChips === null}
                      onClick={() => {
                        if (minimumRaiseChips === null) return;
                        setRaiseChips(String(minimumRaiseChips));
                        setModal({ kind: 'raise' });
                      }}
                    >
                      Raise
                    </button>
                  </div>
                )
              )}
            </section>
          )}
        </>
      )}
    </>
  );
}
