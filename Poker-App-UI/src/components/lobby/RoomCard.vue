<script setup lang="ts">
import type { Room } from '@/services/lobbyService'

const props = defineProps<{
  room: Room
  loading: boolean
}>()

const emit = defineEmits<{
  (e: 'join', roomId: string): void
}>()

const isFull = props.room.playerCount >= props.room.maxPlayers
const isInProgress = props.room.status === 'inProgress'
const canJoin = !isFull && !isInProgress
</script>

<template>
  <div class="room-card" :class="{ disabled: !canJoin }">
    <div class="room-left">
      <span class="room-name">{{ room.name }}</span>
      <span class="room-players">{{ room.playerCount }} / {{ room.maxPlayers }} players</span>
    </div>
    <div class="room-right">
      <span class="room-status" :class="room.status">
        {{ room.status === 'inProgress' ? 'In Progress' : 'Waiting' }}
      </span>
      <button
        class="btn btn-primary"
        style="padding: 7px 18px; font-size: 0.85rem"
        :disabled="!canJoin || loading"
        @click="emit('join', room.id)"
      >
        {{ loading ? '...' : isFull ? 'Full' : isInProgress ? 'Started' : 'Join' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.room-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.25rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: border-color 0.2s;
}

.room-card:not(.disabled):hover {
  border-color: rgba(201, 168, 76, 0.3);
}

.room-card.disabled {
  opacity: 0.5;
}

.room-left {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.room-name {
  font-weight: 500;
  font-size: 0.95rem;
  color: #f0e8d8;
}

.room-players {
  font-size: 0.8rem;
  color: var(--color-text-muted);
}

.room-right {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.room-status {
  font-size: 0.75rem;
  font-weight: 500;
  padding: 3px 10px;
  border-radius: 99px;
}

.room-status.waiting {
  background: rgba(45, 106, 79, 0.2);
  color: #4ade80;
  border: 1px solid rgba(45, 106, 79, 0.4);
}

.room-status.inProgress {
  background: rgba(224, 85, 85, 0.15);
  color: #e05555;
  border: 1px solid rgba(224, 85, 85, 0.3);
}
</style>