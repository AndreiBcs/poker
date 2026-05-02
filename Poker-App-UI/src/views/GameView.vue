<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '@/stores/game'
import { useAuthStore } from '@/stores/auth'
import GameTable from '@/components/game/GameTable.vue'

const route  = useRoute()
const router = useRouter()
const game   = useGameStore()
const auth   = useAuthStore()

const roomId = route.params.roomId as string

// These will dispatch to SignalR when wired up
function handleFold()                { console.log('fold') }
function handleCall()                { console.log('call') }
function handleCheck()               { console.log('check') }
function handleRaise(amount: number) { console.log('raise', amount) }

function handleLeave() {
  game.reset()
  router.push({ name: 'lobby' })
}

onMounted(() => {
  game.setLocalUser(auth.userId!)
  // SignalR connection starts here in the next step
})

onUnmounted(() => {
  game.reset()
  // SignalR disconnect goes here
})
</script>

<template>
  <div class="game-view">

    <nav class="nav">
      <div class="nav-logo">♠ PokerApp</div>
      <div class="nav-actions">
        <span class="nav-user room-id">Table: {{ roomId }}</span>
        <span class="nav-user">{{ auth.username }}</span>
        <button class="btn btn-ghost" @click="handleLeave">Leave Table</button>
      </div>
    </nav>

    <GameTable
      @fold="handleFold"
      @call="handleCall"
      @check="handleCheck"
      @raise="handleRaise"
    />

  </div>
</template>

<style scoped>
.game-view {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #0a0f14;
}

.room-id {
  font-family: var(--font-display);
  color: var(--color-gold) !important;
  font-size: 0.8rem !important;
}
</style>