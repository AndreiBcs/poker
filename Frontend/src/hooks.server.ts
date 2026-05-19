import {redirect} from '@sveltejs/kit'
import type {Handle} from '@sveltejs/kit'

const PUBLIC_ROUTES = ['/login', '/register']


export const handle: Handle = async ({event, resolve}) =>{
    const token = event.cookies.get('token')
    const isPublic = PUBLIC_ROUTES.some(r => event.url.pathname.startsWith(r))

    if(!isPublic && !token){
        throw redirect(303, '/login')
    }

    if(isPublic && token){
        throw redirect(303, '/lobby')
    }

    return resolve(event)
}