export interface Player { id: string; nickname: string }
export interface LobbySummary { id: string; name: string; playerCount: number; maxPlayers: number; hasPassword: boolean }
export interface Seat extends Player {
  seatIndex: number; chips: number; currentBet: number; hasFolded: boolean; isAllIn: boolean;
  isDealer: boolean; isSmallBlind: boolean; isBigBlind: boolean; holeCards: string[]; cardCount: number;
}
export interface Game {
  phase: 'waitingForPlayers' | 'preFlop' | 'flop' | 'turn' | 'river' | 'showdown';
  pot: number; currentBet: number; minRaise: number; currentTurnUserId: string | null;
  board: string[]; players: Seat[];
  lastResult: { pot: number; handDescription: string; winners: { userId: string; username: string; winnings: number }[] } | null;
}
export interface Room {
  id: string; name: string; ownerId: string; maxPlayers: number; smallBlind: number; bigBlind: number;
  startRequested: boolean; status: 'waiting' | 'connecting' | 'playing' | 'paused'; notice: string; revision: number;
  players: (Player & { connected: boolean })[]; game: Game | null;
}
