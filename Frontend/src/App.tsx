import { usePokerArena } from './hooks/usePokerArena';
import { LobbyBrowser } from './components/LobbyBrowser';
import { RoomView } from './components/RoomView';
import { ArenaDialog } from './components/ArenaDialog';

export default function App() {
  const arena = usePokerArena();
  const { nickname, setNickname, editingNickname, setEditingNickname, player, view, setView, roomId, error, setError, busy, run, ensurePlayer } = arena;
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
    <LobbyBrowser arena={arena}/>
    <RoomView arena={arena}/>
    <ArenaDialog arena={arena}/>
  </main>;
}
