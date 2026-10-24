import type { PokerArena } from '../hooks/usePokerArena';
import { PokerTable } from './PokerTable';

export function RoomView({ arena }: { arena: PokerArena }) {
  const { player, roomId, room, setModal, busy, live, setRaise, run, command, host, game, myTurn, toCall, maxRaise, canAct } = arena;
  return <>
    {roomId && !room && <p role="status">Connecting to your lobby…</p>}
    {roomId && room && <>
      <section><div className="row"><h2>{room.name}</h2><button disabled={busy} onClick={() => setModal({ kind: 'leave' })}>Leave lobby</button></div>
        <p role="status">{!live ? 'Reconnecting…' : room.status === 'waiting' ? 'Waiting room' : room.status === 'connecting' ? 'Waiting for every player to connect…' : room.status === 'paused' ? 'Game paused: waiting for players to reconnect.' : 'Game in progress'}</p>
        <p aria-live="polite">{room.notice}</p>
        {!room.startRequested && <>
          <p>{room.players.length}/{room.maxPlayers} players · Blinds {room.smallBlind}/{room.bigBlind}</p>
          <ul>{room.players.map(p => <li key={p.id}>{p.nickname}{p.id === room.ownerId ? ' (host)' : ''} · {p.connected ? 'Connected' : 'Connecting'}{' '}
            {host && p.id !== player?.id && <button disabled={busy} onClick={() => void run(() => command(`/players/${p.id}`, undefined, 'DELETE'))}>Kick {p.nickname}</button>}
          </li>)}</ul>
          {host ? <button disabled={busy || room.players.length < 2 || !live} onClick={() => void run(() => command('/start'))}>Start game</button> : <p>Waiting for the host to start.</p>}
          {room.players.length < 2 && <p>Invite at least one other player to start.</p>}
        </>}
      </section>
      {game && <section>
        <PokerTable game={game} playerId={player!.id} room={room}/>
        {game.lastResult && <p role="status">{game.lastResult.winners.map(w => `${w.username} won ${w.winnings}`).join(', ')} · {game.lastResult.handDescription}</p>}
        {game.phase === 'waitingForPlayers' ? <>
          {host ? <button disabled={busy || room.status !== 'playing' || game.players.filter(p => p.chips > 0).length < 2} onClick={() => void run(() => command('/start'))}>Deal next hand</button> : <p>Waiting for the host to deal the next hand.</p>}
          {game.players.filter(p => p.chips > 0).length < 2 && <p>Game over. Leave and create a new lobby to play again.</p>}
        </> : <div className="row" aria-label="Game actions">
          <span>{myTurn ? 'Your turn' : `Waiting for ${game.players.find(p => p.id === game.currentTurnUserId)?.nickname || 'players'}`}</span>
          {(['fold', 'check', 'call'] as const).map(action => <button key={action} disabled={!canAct || (action === 'check' && toCall > 0) || (action === 'call' && toCall <= 0)} onClick={() => void run(() => command('/actions', { action }))}>{action[0].toUpperCase() + action.slice(1)}</button>)}
          <button disabled={!canAct || maxRaise < game.minRaise} onClick={() => { setRaise(game.minRaise); setModal({ kind: 'raise' }); }}>Raise</button>
        </div>}
      </section>}
    </>}
  </>;
}
