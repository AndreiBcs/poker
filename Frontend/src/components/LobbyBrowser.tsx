import type { PokerArena } from '../hooks/usePokerArena';
import { api } from '../api';

export function LobbyBrowser({ arena }: { arena: PokerArena }) {
  const { setEditingNickname, view, setView, roomId, lobbies, setModal, setError, busy, run, ensurePlayer, enter } = arena;
  return <>
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
  </>;
}
