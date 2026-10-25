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
      <header>
        <h1>Poker Arena</h1>
        <span>Texas Hold’em</span>
        {player && <span>Playing as {player.nickname}</span>}
      </header>
      {error && (
        <div role="alert" className="notice">
          {error} <button onClick={() => setError('')}>Dismiss</button>
        </div>
      )}
      <NicknameForm arena={arena} />
      <LobbyBrowser arena={arena} />
      <RoomView arena={arena} />
      <ArenaDialog arena={arena} />
    </main>
  );
}
