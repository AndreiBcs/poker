import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            redirect: '/lobby'
        },
        {
            path: '/login',
            name: 'login',
            component: () => import('@/views/LoginView.vue'),
            meta: { requireAuth: false}
        },
        {
            path: '/lobby',
            name: 'lobby',
            component: () => import('@/views/LobbyView.vue'),
            meta: { requireAuth: true}
        },
        {
            path: '/game/:roomId',
            name: 'game',
            component: () => import('@/views/GameView.vue'),
            meta: { requireAuth: true}
        },
        {
            path: '/:pathMatch(.*)*',
            redirect: '/lobby'
        }
    ]
})

router.beforeEach((to, _from, next) => {
    const auth = useAuthStore()

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        next({ name: 'login' })
    } else if (to.name === 'login' && auth.isAuthenticated) {
        next({ name: 'lobby' })
    } else {
        next()
    }
})

export default router