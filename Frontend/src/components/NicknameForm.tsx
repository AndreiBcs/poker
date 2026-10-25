import type { PokerArena } from '../hooks/usePokerArena';

export function NicknameForm({ arena }: { arena: PokerArena }) {
  const {
    nickname,
    setNickname,
    editingNickname,
    setEditingNickname,
    view,
    setView,
    roomId,
    busy,
    run,
    ensurePlayer,
  } = arena;
  return (
    <>
      {!roomId && view === 'home' && (
        <section>
          <h2>Take a seat</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void run(async () => {
                await ensurePlayer();
                setEditingNickname(false);
                setView('lobbies');
              });
            }}
          >
            <>
              {editingNickname ? (
                <label>
                  Nickname
                  <input
                    autoComplete="nickname"
                    required
                    maxLength={24}
                    value={nickname}
                    onChange={(e) => setNickname(e.target.value)}
                  />
                </label>
              ) : (
                <p>
                  Welcome, {nickname}.{' '}
                  <button type="button" onClick={() => setEditingNickname(true)}>
                    Change nickname
                  </button>
                </p>
              )}
            </>
            <button disabled={busy || !nickname.trim()}>Look for lobbies</button>
          </form>
        </section>
      )}
    </>
  );
}
