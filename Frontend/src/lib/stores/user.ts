import {writable, derived} from 'svelte/store'
import type {User} from '$lib/types/user'

export const user = writable<User | null>(null)

export const isAuthenticated = derived(user, 
                            ($user) => $user !== null)

export function setUser(u: User){
    user.set(u)
}

export function clearUser(){
    user.set(null)
}