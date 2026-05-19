import {ENV} from '$lib/config/env'
import {getToken} from './auth.service'
import type {User, UpdateUserDto} from '$lib/types/user'

const BASE = `${ENV.USER_API_URL}/api/users`


function authHeaders(){
    return {
        'content-Type': 'application/json',
        Authorization: `Bearer ${getToken()}`
    }
}


// user actions

export async function getUser(id: string): Promise<User>{
    const res = await fetch(`${BASE}/${id}`, {
        headers: authHeaders()
    })

    //const data = await res.json()

    if(!res.ok) throw new Error('User not found.')

    return await res.json() as User
}


export async function updateUser(id: string, dto: UpdateUserDto): Promise<User>{
    const res = await fetch(`${BASE}/update-user/${id}`, {
        method: 'PUT',
        headers: authHeaders(),
        body: JSON.stringify(dto)
    })

    return await res.json() as User
} 


export async function deleteUser(id: string): Promise<void>{
    const res = await fetch(`${BASE}/delete-user/${id}`, {
        method: 'DELETE',
        headers: authHeaders()
    })

    if(!res.ok) throw new Error()
}