export type WsEventType = 
    | 'game_state' | 'player_joined' | 'player-left' | 'player_action' | 'error'

export interface WsMessage<T = unknown>{
    event: WsEventType,
    payload: T
}

export interface PlayerActionPayload{
    action: 'fold' | 'check' | 'call' | 'raise',
    amount?: number
}