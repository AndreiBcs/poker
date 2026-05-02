import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'home',
            component: () => import('@/views/HomeView.vue'),
            meta: { requiresAuth: false }
        },
        {
            path: '/login',
            name: 'login',
            component: () => import('@/views/LoginView.vue'),
            meta: { requiresAuth: false}
        },
        {
            path: '/register',
            name: 'register',
            component: () => import('@/views/RegisterView.vue'),
            meta: { requiresAuth: false}
        },
        {
            path: '/lobby',
            name: 'lobby',
            component: () => import('@/views/LobbyView.vue'),
            meta: { requiresAuth: false}
        },
        {
            path: '/game/:roomId',
            name: 'game',
            component: () => import('@/views/GameView.vue'),
            meta: { requiresAuth: false}
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