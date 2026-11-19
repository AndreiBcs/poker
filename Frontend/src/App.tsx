import { NicknameForm } from './components/NicknameForm';
import { usePokerArena } from './hooks/usePokerArena';
import { LobbyBrowser } from './components/LobbyBrowser';
import { RoomView } from './components/RoomView';
import { ArenaDialog } from './components/ArenaDialog';

export default function App() {
  const arena = usePokerArena();
  const { player, error, setError } = arena;
  return (
    <main>
      <div className="background-suits" aria-hidden="true">
        <span>♠</span>
        <span>♥</span>
        <span>♣</span>
        <span>♦</span>
      </div>
      <header className="site-header">
        <div className="site-title">
          <span className="site-mark" aria-hidden="true">
            ♦
          </span>
          <div>
            <h1>Poker</h1>
            <span>Texas Hold’em</span>
          </div>
        </div>
        {player && <span className="identity">Playing as {player.nickname}</span>}
      </header>
      {error && (
        <div role="alert" className="notice">
          {error} <button onClick={() => setError('')}>Dismiss</button>
        </div>
      )}
      {arena.checkingSeat ? (
        <p role="status">Checking for a saved game…</p>
      ) : (
        <>
          <NicknameForm arena={arena} />
          <LobbyBrowser arena={arena} />
        </>
      )}
      <RoomView arena={arena} />
      <ArenaDialog arena={arena} />
    </main>
  );
}
