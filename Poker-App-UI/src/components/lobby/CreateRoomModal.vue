<script setup lang="ts">
import { ref } from 'vue'
import FloatingInput from '@/components/ui/FloatingInput.vue'

const emit = defineEmits<{
  (e: 'create', name: string, maxPlayers: number): void
  (e: 'close'): void
}>()

const name = ref('')
const maxPlayers = ref(6)
const error = ref<string | null>(null)

function handleSubmit() {
  error.value = null

  if (!name.value.trim()) {
    error.value = 'Room name is required'
    return
  }
  if (maxPlayers.value < 2 || maxPlayers.value > 10) {
    error.value = 'Players must be between 2 and 10'
    return
  }

  emit('create', name.value.trim(), maxPlayers.value)
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">

      <div class="modal-header">
        <h2>Create Table</h2>
        <button class="close-btn" @click="emit('close')">✕</button>
      </div>

      <form @submit.prevent="handleSubmit">
        <FloatingInput v-model="name" id="roomName" label="Table Name" type="text" />

        <div class="slider-group">
          <label>Max Players: <strong>{{ maxPlayers }}</strong></label>
          <input type="range" v-model="maxPlayers" min="2" max="10" step="1" />
          <div class="slider-range">
            <span>2</span>
            <span>10</span>
          </div>
        </div>

        <p v-if="error" class="error">{{ error }}</p>

        <div class="modal-actions">
          <button type="button" class="btn btn-ghost" @click="emit('close')">Cancel</button>
          <button type="submit" class="btn btn-primary">Create</button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 100;
}

.modal {
  background: #161b22;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 2rem;
  width: 340px;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h2 {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  color: #f0e8d8;
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  color: var(--color-text-muted);
  font-size: 1rem;
  cursor: pointer;
  line-height: 1;
}

.close-btn:hover { color: var(--color-text); }

form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.slider-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.slider-group label {
  font-size: 0.875rem;
  color: var(--color-text-muted);
}

.slider-group strong {
  color: var(--color-gold);
}

.slider-group input[type='range'] {
  width: 100%;
  accent-color: var(--color-gold);
}

.slider-range {
  display: flex;
  justify-content: space-between;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

.error {
  margin: 0;
  color: #e05555;
  font-size: 0.85rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}
</style>