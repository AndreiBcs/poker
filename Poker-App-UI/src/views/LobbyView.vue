<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getRooms, createRoom, joinRoom, type Room } from '@/services/lobbyService'
import RoomCard from '@/components/lobby/RoomCard.vue'
import CreateRoomModal from '@/components/lobby/CreateRoomModal.vue'

const router = useRouter()
const auth = useAuthStore()

const rooms = ref<Room[]>([])
const loadingRooms = ref(false)
const joiningRoomId = ref<string | null>(null)
const showCreateModal = ref(false)
const error = ref<string | null>(null)

async function fetchRooms() {
  loadingRooms.value = true
  error.value = null
  try {
    rooms.value = await getRooms()
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    loadingRooms.value = false
  }
}

async function handleJoin(roomId: string) {
  joiningRoomId.value = roomId
  try {
    await joinRoom(roomId)
    router.push({ name: 'game', params: { roomId } })
  } catch (e: unknown) {
    error.value = (e as Error).message
    joiningRoomId.value = null
  }
}

async function handleCreate(name: string, maxPlayers: number) {
  try {
    const room = await createRoom({ name, maxPlayers })
    showCreateModal.value = false
    await joinRoom(room.id)
    router.push({ name: 'game', params: { roomId: room.id } })
  } catch (e: unknown) {
    error.value = (e as Error).message
  }
}

function handleLogout() {
  auth.logout()
  router.push({ name: 'login' })
}

onMounted(() => {
  fetchRooms()
  const interval = setInterval(fetchRooms, 10_000)
  onUnmounted(() => clearInterval(interval))
})
</script>

<template>
  <div class="lobby">

    <nav class="nav">
      <div class="nav-logo" @click="router.push({ name: 'home' })" style="cursor: pointer">
        ♠ PokerApp
      </div>
      <div class="nav-actions">
        <span class="nav-user">{{ auth.username }}</span>
        <button class="btn btn-primary" @click="showCreateModal = true">+ Create Room</button>
        <button class="btn btn-ghost" @click="handleLogout">Logout</button>
      </div>
    </nav>

    <main class="lobby-main">
      <div class="lobby-header">
        <div>
          <h2 class="lobby-title">Tables</h2>
          <p class="lobby-sub">Join an open table or create your own</p>
        </div>
        <button
          class="btn btn-ghost refresh-btn"
          :disabled="loadingRooms"
          @click="fetchRooms"
        >
          {{ loadingRooms ? 'Refreshing...' : '↻ Refresh' }}
        </button>
      </div>

      <p v-if="error" class="error">{{ error }}</p>

      <div v-if="loadingRooms && rooms.length === 0" class="state-msg">
        Loading tables...
      </div>

      <div v-else-if="rooms.length === 0" class="state-msg">
        <span class="state-icon">♠</span>
        <p>No tables open yet.</p>
        <button class="btn btn-primary" @click="showCreateModal = true">Create the first one</button>
      </div>

      <div v-else class="room-list">
        <RoomCard
          v-for="room in rooms"
          :key="room.id"
          :room="room"
          :loading="joiningRoomId === room.id"
          @join="handleJoin"
        />
      </div>
    </main>

    <CreateRoomModal
      v-if="showCreateModal"
      @create="handleCreate"
      @close="showCreateModal = false"
    />

  </div>
</template>

<style scoped>
.lobby {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.lobby-main {
  flex: 1;
  max-width: 720px;
  width: 100%;
  margin: 0 auto;
  padding: 2.5rem 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.lobby-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}

.lobby-title {
  font-family: var(--font-display);
  font-size: 1.8rem;
  font-weight: 700;
  color: #f0e8d8;
  margin: 0;
}

.lobby-sub {
  font-size: 0.875rem;
  color: var(--color-text-muted);
  margin: 0.25rem 0 0;
}

.refresh-btn {
  font-size: 0.85rem;
  padding: 7px 14px;
}

.room-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.state-msg {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  margin-top: 5rem;
  color: var(--color-text-muted);
  font-size: 0.95rem;
}

.state-icon {
  font-size: 2.5rem;
  opacity: 0.2;
}

.error {
  color: #e05555;
  font-size: 0.875rem;
}

@media (max-width: 768px) {
  .lobby-main   { padding: 1.5rem 1rem; }
  .lobby-header { flex-direction: column; align-items: flex-start; gap: 1rem; }
  .nav          { padding: 1rem 1.25rem; }
}
</style>