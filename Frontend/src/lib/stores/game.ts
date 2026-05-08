import {writable} from 'svelte/store'
import type {GameState} from '$lib/types/game'

export const game = writable<GameState | null>(null)

export function setGameState(state: GameState){
    game.set(state)
}

export function clearGameState(){
    game.set(null)
}