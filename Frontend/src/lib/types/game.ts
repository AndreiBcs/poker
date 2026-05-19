export type Suit = '♠' | '♥' | '♦' | '♣';
export type GamePhase = 
    'waiting' | 'preflop' | 'flop' | 'turn' | 'river' | 'showdown';

export interface Card{
    suit: Suit,
    value: string
}

export interface GamePlayer{
    id: string,
    username: string,
    chips: number,
    hand: Card[],
    isActive: boolean,
    hasFolded: boolean
}

export interface GameState{
    tableId: string,
    phase: GamePhase,
    players: GamePlayer[],
    pot: number,
    communityCards: Card[],
    currentTurn: string
}
