import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// --- Types ---
export type Suit = 'hearts' | 'diamonds' | 'clubs' | 'spades'
export type Rank = '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' | '10' | 'J' | 'Q' | 'K' | 'A'

export interface Card {
  suit: Suit
  rank: Rank
}

export type PlayerStatus = 'waiting' | 'active' | 'folded' | 'allIn' | 'disconnected'
export type GamePhase = 'waiting' | 'preflop' | 'flop' | 'turn' | 'river' | 'showdown'

export interface Player {
  userId: string
  username: string
  chips: number
  bet: number                 // current bet in this round
  hand: Card[]                // only populated for the local player
  status: PlayerStatus
  isTurn: boolean
}

export interface GameState {
  roomId: string
  phase: GamePhase
  players: Player[]
  communityCards: Card[]
  pot: number
  currentBet: number          // the bet everyone must match
  winnerId: string | null
}

export const useGameStore = defineStore('game', () => {
    // --- State ---
    const roomId = ref<string | null>(null)
    const phase = ref<GamePhase>('waiting')
    const players = ref<Player[]>([])
    const communityCards = ref<Card[]>([])
    const pot = ref(0)
    const currentBet = ref(0)
    const winnerId = ref<string | null>(null)
    const isConnected = ref(false)
            
    // --- Getters ---
    const localPlayer = computed(() =>
        players.value.find(p => p.userId === _localUserId.value) ?? null
    )
    const isMyTurn = computed(() => localPlayer.value?.isTurn ?? false)
    const activePlayers = computed(() =>
        players.value.filter(p => p.status !== 'folded' && p.status !== 'disconnected')
    )

    // Internal — not exposed, just used by getters above
    const _localUserId = ref<string | null>(null)

    // --- Actions ---
    function setLocalUser(userId: string) {
        _localUserId.value = userId
    }

    function applyGameState(state: GameState) {
        roomId.value = state.roomId
        phase.value = state.phase
        players.value = state.players
        communityCards.value = state.communityCards
        pot.value = state.pot
        currentBet.value = state.currentBet
        winnerId.value = state.winnerId
    }

    function playerJoined(player: Player) {
        const exists = players.value.find(p => p.userId === player.userId)
        if (!exists) players.value.push(player)
    }

    function playerLeft(userId: string) {
        players.value = players.value.filter(p => p.userId !== userId)
    }

    function playerActed(userId: string, status: PlayerStatus, bet: number, pot: number) {
        const player = players.value.find(p => p.userId === userId)
        if (player) {
        player.status = status
        player.bet = bet
        }
        pot = pot
    }

    function setTurn(userId: string) {
        players.value.forEach(p => {
        p.isTurn = p.userId === userId
        })
    }

    function dealHoleCards(cards: Card[]) {
        const player = localPlayer.value
        if (player) player.hand = cards
    }

    function revealCommunityCards(cards: Card[]) {
        communityCards.value = cards
    }

    function reset() {
        roomId.value = null
        phase.value = 'waiting'
        players.value = []
        communityCards.value = []
        pot.value = 0
        currentBet.value = 0
        winnerId.value = null
        isConnected.value = false
    }

    return {
        // State
        roomId, phase, players, communityCards,
        pot, currentBet, winnerId, isConnected,
        // Getters
        localPlayer, isMyTurn, activePlayers,
        // Actions
        setLocalUser, applyGameState, playerJoined,
        playerLeft, playerActed, setTurn,
        dealHoleCards, revealCommunityCards, reset
    }
})