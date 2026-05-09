import {ENV} from '$lib/config/env'
import type {AuthResponse, LoginDto, SignUpDto} from '$lib/types/user'
import {setUser, clearUser} from '$lib/stores/user'

const BASE = `${ENV.USER_API_URL}/api/auth`

let _token: string | null = null



export function getToken(): string | null{
    return _token
}


// session

function saveSession(data: AuthResponse){
    _token = data.token

    sessionStorage.setItem('token', data.token)

    document.cookie = `token=${data.token}; path=/; SameSite=Strict`
    
    setUser({
        id: data.id,
        username: data.username,
        email: data.email
    })
}

export function loadSession(){
    const token = sessionStorage.getItem('token')
    if(token) _token = token
}


// auth

export async function login(dto: LoginDto): Promise<AuthResponse>{
    const res = await fetch(`${BASE}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    })

    if(!res.ok){
        const err = await res.json()
        throw err
    }

    const data: AuthResponse = await res.json() as AuthResponse
    saveSession(data)
    return data
}


export async function signUp(dto: SignUpDto): Promise<AuthResponse>{
    const res = await fetch(`${BASE}/sign-up`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    })

    if(!res.ok){
        const err = await res.json()
        throw err
    }

    const data: AuthResponse = await res.json() as AuthResponse
    saveSession(data)
    return data
}


export function logout(){
    _token = null

    sessionStorage.removeItem('token')

    document.cookie = 'token=; path=/; max-age=0';
    
    clearUser()
}