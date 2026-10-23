import { useEffect, useRef, useState } from 'react';
import { api, ApiError } from './api';
import type { Game, LobbySummary, Player, Room } from './types';

type Modal = { kind: 'join'; lobby: LobbySummary } | { kind: 'raise' } | { kind: 'leave' } | null;
const nicknameKey = 'poker.nickname';
const playerKey = 'poker.player';
const roomKey = 'poker.room';

function storedPlayer(): Player | null {
  try { return JSON.parse(sessionStorage.getItem(playerKey) || 'null'); } catch { return null; }
}

export default function App() {
  const [nickname, setNickname] = useState(localStorage.getItem(nicknameKey) || '');
  const [editingNickname, setEditingNickname] = useState(!localStorage.getItem(nicknameKey));
  const [player, setPlayer] = useState<Player | null>(storedPlayer);
  const [view, setView] = useState<'home' | 'lobbies'>('home');
  const [roomId, setRoomId] = useState<string | null>(sessionStorage.getItem(roomKey));
  const [room, setRoom] = useState<Room | null>(null);
  const [lobbies, setLobbies] = useState<LobbySummary[]>([]);
  const [modal, setModal] = useState<Modal>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [live, setLive] = useState(false);
  const [raise, setRaise] = useState(50);
  const busyRef = useRef(false);

  function exitRoom() {
    sessionStorage.removeItem(roomKey);
    setRoomId(null); setRoom(null); setModal(null); setView('lobbies');
  }

  async function run(work: () => Promise<void>) {
    if (busyRef.current) return;
    busyRef.current = true; setBusy(true); setError('');
    try { await work(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Something went wrong. Please retry.'); }
    finally { busyRef.current = false; setBusy(false); }
  }

  async function ensurePlayer() {
    let current = player;
    if (current && current.nickname === nickname.trim()) {
      try { await api(`/players/${current.id}`); return current; }
      catch (e) { if (!(e instanceof ApiError) || e.status !== 404) throw e; }
    }
    current = await api<Player>('/players', undefined, 'POST', { nickname });
    setPlayer(current);
    localStorage.setItem(nicknameKey, current.nickname);
    sessionStorage.setItem(playerKey, JSON.stringify(current));
    return current;
  }

  function enter(id: string) { sessionStorage.setItem(roomKey, id); setRoomId(id); setModal(null); }

  useEffect(() => {
    if (view !== 'lobbies' || roomId) return;
    let stopped = false;
    const load = async () => {
      try { const result = await api<LobbySummary[]>('/lobbies'); if (!stopped) setLobbies(result); }
      catch (e) { if (!stopped) setError(e instanceof Error ? e.message : 'Cannot load lobbies.'); }
    };
    void load(); const timer = setInterval(() => void load(), 2000);
    return () => { stopped = true; clearInterval(timer); };
  }, [view, roomId]);

  useEffect(() => {
    if (!roomId || !player) return;
    let stopped = false;
    let socket: WebSocket | undefined;
    let retry: ReturnType<typeof setTimeout> | undefined;
    const connect = async () => {
      try {
        const state = await api<Room>(`/lobbies/${roomId}`, player.id);
        if (stopped) return;
        setRoom(state);
        const url = new URL(`/api/lobbies/${roomId}/live`, window.location.href);
        url.protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        url.searchParams.set('playerId', player.id);
        socket = new WebSocket(url);
        socket.onopen = () => { if (!stopped) setLive(true); };
        socket.onmessage = event => {
          if (stopped) return;
          const message = JSON.parse(event.data);
          if (message.type === 'state') setRoom(message.payload);
          if (message.type === 'removed') { setError(message.error); exitRoom(); }
        };
        socket.onclose = () => { if (!stopped) { setLive(false); retry = setTimeout(() => void connect(), 1200); } };
      } catch (e) {
        if (stopped) return;
        if (e instanceof ApiError && [403, 404].includes(e.status)) { setError(e.message); exitRoom(); }
        else { setLive(false); retry = setTimeout(() => void connect(), 1500); }
      }
    };
    void connect();
    return () => { stopped = true; clearTimeout(retry); socket?.close(); setLive(false); };
  }, [roomId, player]);

  // Refreshing a tab preserves its seat; nickname persists across browser visits.
  useEffect(() => {
    if (roomId && !player) { sessionStorage.removeItem(roomKey); setRoomId(null); }
  }, [roomId, player]);

  async function command(path: string, body?: unknown, method = 'POST') {
    await api(`/lobbies/${roomId}${path}`, player!.id, method, body);
    setRoom(await api<Room>(`/lobbies/${roomId}`, player!.id));
    setModal(null);
  }

  const host = !!room && player?.id === room.ownerId;
  const game = room?.game;
  const myTurn = game?.currentTurnUserId === player?.id;
  const mySeat = game?.players.find(p => p.id === player?.id);
  const toCall = game && mySeat ? game.currentBet - mySeat.currentBet : 0;
  const maxRaise = mySeat ? mySeat.chips - toCall : 0;
  const canAct = live && room?.status === 'playing' && myTurn && !busy;

  return <main>
    <header><h1>Poker Arena</h1><span>Texas Hold’em</span>{player && <span>Playing as {player.nickname}</span>}</header>
    {error && <div role="alert" className="notice">{error} <button onClick={() => setError('')}>Dismiss</button></div>}
    {!roomId && view === 'home' && <section>
      <h2>Take a seat</h2>
      <form onSubmit={e => { e.preventDefault(); void run(async () => { await ensurePlayer(); setEditingNickname(false); setView('lobbies'); }); }}>
        <>{editingNickname ? <label>Nickname<input autoComplete="nickname" required maxLength={24} value={nickname} onChange={e => setNickname(e.target.value)} /></label> : <p>Welcome, {nickname}. <button type="button" onClick={() => setEditingNickname(true)}>Change nickname</button></p>}</>
        <button disabled={busy || !nickname.trim()}>Look for lobbies</button>
      </form>
    </section>}
    {!roomId && view === 'lobbies' && <>
      <button onClick={() => { setEditingNickname(true); setView('home'); }}>Change nickname</button>
      <section><h2>Open lobbies</h2>
        {!lobbies.length && <p>No open lobbies yet. Create one below.</p>}
        <ul>{lobbies.map(lobby => <li key={lobby.id}>
          <strong>{lobby.name}</strong> · {lobby.playerCount}/{lobby.maxPlayers} players · Password required{' '}
          <button disabled={busy || lobby.playerCount >= lobby.maxPlayers} onClick={() => { setModal({ kind: 'join', lobby }); setError(''); }}>Join</button>
        </li>)}</ul>
      </section>
      <section><h2>Create a lobby</h2>
        <form onSubmit={e => {
          e.preventDefault(); const form = new FormData(e.currentTarget);
          void run(async () => {
            const current = await ensurePlayer();
            const result = await api<{ id: string }>('/lobbies', current.id, 'POST', {
              name: form.get('name'), password: form.get('password'), maxPlayers: Number(form.get('maxPlayers')),
              smallBlind: Number(form.get('smallBlind')), bigBlind: Number(form.get('bigBlind')),
            }); enter(result.id);
          });
        }}>
          <label>Lobby name<input name="name" required maxLength={60}/></label>
          <label>Lobby password<input name="password" type="password" autoComplete="new-password" required maxLength={100}/></label>
          <label>Maximum players<input name="maxPlayers" type="number" min="2" max="10" defaultValue="6" required/></label>
          <details><summary>Blinds (2,000 starting chips)</summary>
            <label>Small blind<input name="smallBlind" type="number" min="1" max="1000" defaultValue="25" required/></label>
            <label>Big blind<input name="bigBlind" type="number" min="1" max="2000" defaultValue="50" required/></label>
          </details>
          <button disabled={busy}>Create lobby</button>
        </form>
      </section>
    </>}
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
    {modal && <div className="backdrop"><div role="dialog" aria-modal="true" aria-labelledby="dialog-title" className="dialog">
      {modal.kind === 'join' && <form key={modal.lobby.id} onSubmit={e => {
        e.preventDefault(); const password = new FormData(e.currentTarget).get('password'); const id = modal.lobby.id;
        void run(async () => { const current = await ensurePlayer(); await api(`/lobbies/${id}/join`, current.id, 'POST', { password }); enter(id); });
      }}><h2 id="dialog-title">Join {modal.lobby.name}</h2>
        <label>Lobby password<input autoFocus type="password" name="password" required maxLength={100}/></label>
        <button disabled={busy}>Join lobby</button>
      </form>}
      {modal.kind === 'raise' && <form onSubmit={e => { e.preventDefault(); void run(() => command('/actions', { action: 'raise', amount: raise })); }}>
        <h2 id="dialog-title">Raise the bet</h2><p>This adds to the current table bet of {game?.currentBet}.</p>
        <label>Raise by<input autoFocus type="number" required min={game?.minRaise} max={maxRaise} value={raise} onChange={e => setRaise(Number(e.target.value))}/></label>
        <button disabled={busy}>Confirm raise</button>
      </form>}
      {modal.kind === 'leave' && <><h2 id="dialog-title">Leave this lobby?</h2><p>{room?.startRequested ? 'Leaving folds your hand and forfeits your remaining stack.' : host ? 'The next player becomes host. An empty lobby closes.' : 'You can rejoin with the lobby password.'}</p>
        <button disabled={busy} onClick={() => void run(async () => { await api(`/lobbies/${roomId}/leave`, player!.id, 'POST'); exitRoom(); })}>Leave</button></>}
      {error && <p role="alert">{error}</p>}
      <button disabled={busy} onClick={() => setModal(null)}>Cancel</button>
    </div></div>}
  </main>;
}

function PokerTable({ game, playerId, room }: { game: Game; playerId: string; room: Room }) {
  return <>
    <h2>Poker table</h2><p>{game.phase} · Current bet {game.currentBet} · Minimum raise {game.minRaise}</p>
    <div className="table-wrap"><div className="poker-table">
      <div className="board"><strong>Pot: {game.pot}</strong><div aria-label="Community cards">{game.board.length ? game.board.map(c => <span className="card" key={c}>{c}</span>) : 'No community cards yet'}</div></div>
      {game.players.map((p, i) => {
        const angle = (2 * Math.PI * i) / game.players.length - Math.PI / 2;
        return <div key={p.id} className={`seat ${game.currentTurnUserId === p.id ? 'turn' : ''}`} style={{ left: `${50 + 43 * Math.cos(angle)}%`, top: `${50 + 42 * Math.sin(angle)}%` }}>
          <div className="avatar" aria-hidden="true">{p.nickname.slice(0, 2).toUpperCase()}</div>
          <strong>{p.nickname}{p.id === playerId ? ' (you)' : ''}</strong>
          <div>{p.chips} chips · Bet {p.currentBet}</div>
          <div>{p.hasFolded ? 'Folded' : p.isAllIn ? 'All in' : ''} {p.isDealer ? 'D ' : ''}{p.isSmallBlind ? 'SB ' : ''}{p.isBigBlind ? 'BB' : ''}</div>
          {!room.players.some(member => member.id === p.id && member.connected) && <small>Disconnected</small>}
          <div aria-label={`${p.nickname}'s cards`}>{p.holeCards.length ? p.holeCards.map(c => <span className="card" key={c}>{c}</span>) : Array.from({ length: p.cardCount }, (_, n) => <span className="card" key={n}>?</span>)}</div>
        </div>;
      })}
    </div></div>
  </>;
}
