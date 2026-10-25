import type { Game, Room } from '../types';

export function PokerTable({ game, playerId, room }: { game: Game; playerId: string; room: Room }) {
  return (
    <>
      <h2>Poker table</h2>
      <p>
        {game.phase} · Current bet {game.currentBet} · Minimum raise {game.minRaise}
      </p>
      <div className="table-wrap">
        <div className="poker-table">
          <div className="board">
            <strong>Pot: {game.pot}</strong>
            <div aria-label="Community cards">
              {game.board.length
                ? game.board.map((c) => (
                    <span className="card" key={c}>
                      {c}
                    </span>
                  ))
                : 'No community cards yet'}
            </div>
          </div>
          {game.players.map((p, i) => {
            const angle = (2 * Math.PI * i) / game.players.length - Math.PI / 2;
            return (
              <div
                key={p.id}
                className={`seat ${game.currentTurnUserId === p.id ? 'turn' : ''}`}
                style={{
                  left: `${50 + 43 * Math.cos(angle)}%`,
                  top: `${50 + 42 * Math.sin(angle)}%`,
                }}
              >
                <div className="avatar" aria-hidden="true">
                  {p.nickname.slice(0, 2).toUpperCase()}
                </div>
                <strong>
                  {p.nickname}
                  {p.id === playerId ? ' (you)' : ''}
                </strong>
                <div>
                  {p.chips} chips · Bet {p.currentBet}
                </div>
                <div>
                  {p.hasFolded ? 'Folded' : p.isAllIn ? 'All in' : ''} {p.isDealer ? 'D ' : ''}
                  {p.isSmallBlind ? 'SB ' : ''}
                  {p.isBigBlind ? 'BB' : ''}
                </div>
                {!room.players.some((member) => member.id === p.id && member.connected) && (
                  <small>Disconnected</small>
                )}
                <div aria-label={`${p.nickname}'s cards`}>
                  {p.holeCards.length
                    ? p.holeCards.map((c) => (
                        <span className="card" key={c}>
                          {c}
                        </span>
                      ))
                    : Array.from({ length: p.cardCount }, (_, n) => (
                        <span className="card" key={n}>
                          ?
                        </span>
                      ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
}
