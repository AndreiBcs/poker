import {ENV} from '$lib/config/env'
import {setGameState} from '$lib/stores/game'
import type {WsMessage} from '$lib/types/websocket'


let ws: WebSocket | null = null


// websocket actions


export function connect(tableId: string){
    ws = new WebSocket(`${ENV.WS_URL}/tables/${tableId}`)

    ws.onopen = () => console.log('[WS] connected')

    ws.onmessage = (e: MessageEvent) => {
        const msg: WsMessage = JSON.parse(e.data)

        switch(msg.event){
            case 'game_state':
                setGameState(msg.payload as any) // deak ajuta-ma
                break
            case 'player_joined':
                //
                break
            case 'player-left':
                //
                break
            case 'player_action':
                //
                break
            case 'error':
                //
                break
            default:
                throw new Error()
        }
    }

    ws.onclose = () => console.log('[WS] disconnected')
    ws.onerror = (e) => console.error('[WS] error', e)
}



export function send(msg: WsMessage){
    if(ws?.readyState === WebSocket.OPEN){
        ws.send(JSON.stringify(msg))
    }
    else{
        console.warn('[WS] tried to send but socket is not open')
    }
}



export function disconnect(){
    ws?.close()
    ws = null
}