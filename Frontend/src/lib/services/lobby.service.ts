import {ENV} from '$lib/config/env'
import {getToken} from './auth.service'
import type {Table, CreateTableDto} from '$lib/types/lobby'

const BASE = `${ENV.LOBBY_API_URL}/tables`


function authHeaders(){
    return{
        'content-Type': 'application/json',
        Authorization: `Bearer ${getToken()}`
    }
}


// lobby actions

export async function getTables(): Promise<Table[]>{
    const res = await fetch(BASE, {headers: authHeaders()})



    return await res.json() as Table[]
}



export async function getTable(id: string): Promise<Table>{
    const res = await fetch(`${BASE}/${id}`, {
        headers: authHeaders()
    })


    return await res.json() as Table
}



export async function createTable(dto: CreateTableDto): Promise<Table>{
    const res = await fetch(BASE, {
        method: 'POST',
        headers: authHeaders(),
        body: JSON.stringify(dto)
    })


    return await res.json() as Table
}



export async function joinTable(id: string): Promise<void>{
    const res = await fetch(`${BASE}/${id}/join`, {
        method: 'POST',
        headers: authHeaders(),
    })


    if(!res.ok) throw new Error()
}



export async function leaveTable(id: string): Promise<void>{
    const res = await fetch(`${BASE}/${id}/leave`, {
        method: 'POST',
        headers: authHeaders(),
    })


    if(!res.ok) throw new Error() 
}