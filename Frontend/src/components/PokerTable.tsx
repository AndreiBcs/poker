import type { Game, Room } from '../types';

const phaseLabels: Record<Game['phase'], string> = {
  waitingForPlayers: 'Waiting for players',
  preFlop: 'Pre-flop',
  flop: 'Flop',
  turn: 'Turn',
  river: 'River',
  showdown: 'Showdown',
};

function PlayingCard({ label }: { label: string }) {
  const suit = label.at(-1);
  return <span className={suit === '♥' || suit === '♦' ? 'card red-card' : 'card'}>{label}</span>;
}

export function PokerTable({ game, playerId, room }: { game: Game; playerId: string; room: Room }) {
  const board = game.lastResult?.board ?? game.board;

  return (
    <>
      <h2>Poker table</h2>
      {game.lastResult ? (
        <p>Hand complete</p>
      ) : (
        <p className="muted">
          {phaseLabels[game.phase]} · Current bet {game.currentBet} · Minimum raise by{' '}
          {game.minRaise}
        </p>
      )}
      <div className="table-wrap">
        <div className="poker-table">
          <div className="board">
            <strong>
              {game.lastResult ? `Final pot: ${game.lastResult.pot}` : `Pot: ${game.pot}`}
            </strong>
            <div className="cards" aria-label="Community cards">
              {board.length
                ? board.map((card) => <PlayingCard key={card} label={card} />)
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
                <div className="seat-heading">
                  <div className="avatar" aria-hidden="true">
                    {p.nickname.slice(0, 2).toUpperCase()}
                  </div>
                  <strong className="seat-name" title={p.nickname}>
                    {p.nickname}
                    {p.id === playerId ? ' (you)' : ''}
                  </strong>
                </div>
                <div className="muted">
                  {p.chips} chips · Bet {p.currentBet}
                </div>
                <div>
                  {p.isSittingOut
                    ? 'Sitting out'
                    : p.hasFolded
                      ? 'Folded'
                      : p.isAllIn
                        ? 'All in'
                        : p.chips === 0
                          ? 'Spectating'
                          : ''}{' '}
                  {p.isDealer ? 'D ' : ''}
                  {p.isSmallBlind ? 'SB ' : ''}
                  {p.isBigBlind ? 'BB' : ''}
                </div>
                {!room.players.some((member) => member.id === p.id && member.connected) && (
                  <small>Disconnected</small>
                )}
                <div className="cards" aria-label={`${p.nickname}'s cards`}>
                  {p.holeCards.length
                    ? p.holeCards.map((card) => <PlayingCard key={card} label={card} />)
                    : Array.from({ length: p.cardCount }, (_, n) => (
                        <span className="card card-back" key={n}>
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
