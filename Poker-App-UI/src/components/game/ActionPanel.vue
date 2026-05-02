<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Player } from '@/stores/game'

const props = defineProps<{
  currentBet: number
  player: Player
}>()

const emit = defineEmits<{
  (e: 'fold'): void
  (e: 'call'): void
  (e: 'raise', amount: number): void
  (e: 'check'): void
}>()

const raiseAmount = ref(props.currentBet * 2 || 20)
const showRaiseSlider = ref(false)

const canCheck  = computed(() => props.currentBet === 0 || props.player.bet === props.currentBet)
const callAmount = computed(() => props.currentBet - props.player.bet)
const minRaise  = computed(() => props.currentBet * 2 || 20)
const maxRaise  = computed(() => props.player.chips)

function handleRaise() {
  emit('raise', raiseAmount.value)
  showRaiseSlider.value = false
}
</script>

<template>
  <div class="action-panel">
    <div class="panel-label">Your Turn</div>

    <div class="raise-slider" v-if="showRaiseSlider">
      <div class="raise-header">
        <span>Raise to</span>
        <strong>{{ raiseAmount.toLocaleString() }}</strong>
      </div>
      <input
        type="range"
        v-model="raiseAmount"
        :min="minRaise"
        :max="maxRaise"
        step="10"
      />
      <div class="raise-range">
        <span>{{ minRaise }}</span>
        <span>{{ maxRaise.toLocaleString() }}</span>
      </div>
    </div>

    <div class="actions">
      <button class="btn-action fold" @click="emit('fold')">
        Fold
      </button>

      <button v-if="canCheck" class="btn-action check" @click="emit('check')">
        Check
      </button>
      <button v-else class="btn-action call" @click="emit('call')">
        Call <span class="amount">{{ callAmount.toLocaleString() }}</span>
      </button>

      <button
        class="btn-action raise"
        :class="{ active: showRaiseSlider }"
        @click="showRaiseSlider ? handleRaise() : showRaiseSlider = true"
      >
        {{ showRaiseSlider ? 'Confirm' : 'Raise' }}
      </button>
    </div>

  </div>
</template>

<style scoped>
.action-panel {
  display: flex;
  flex-direction: column;
  gap: 10px;
  align-items: center;
}

.panel-label {
  font-size: 0.7rem;
  font-weight: 500;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--color-gold);
  animation: pulse 1.2s ease-in-out infinite;
}

.raise-slider {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 10px 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 200px;
}

.raise-header {
  display: flex;
  justify-content: space-between;
  font-size: 0.8rem;
  color: var(--color-text-muted);
}

.raise-header strong {
  color: var(--color-gold);
}

.raise-slider input[type='range'] {
  width: 100%;
  accent-color: var(--color-gold);
}

.raise-range {
  display: flex;
  justify-content: space-between;
  font-size: 0.7rem;
  color: var(--color-text-muted);
}

.actions {
  display: flex;
  gap: 8px;
}

.btn-action {
  padding: 10px 20px;
  border-radius: var(--radius-sm);
  font-family: var(--font-body);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  border: 1px solid transparent;
  transition: opacity 0.15s, transform 0.1s;
  display: flex;
  align-items: center;
  gap: 5px;
}

.btn-action:active { transform: scale(0.96); }

.fold {
  background: rgba(224, 85, 85, 0.12);
  color: #e05555;
  border-color: rgba(224, 85, 85, 0.25);
}

.fold:hover { background: rgba(224, 85, 85, 0.2); }

.check {
  background: rgba(45, 106, 79, 0.15);
  color: #4ade80;
  border-color: rgba(45, 106, 79, 0.3);
}

.check:hover { background: rgba(45, 106, 79, 0.25); }

.call {
  background: rgba(45, 106, 79, 0.15);
  color: #4ade80;
  border-color: rgba(45, 106, 79, 0.3);
}

.call:hover { background: rgba(45, 106, 79, 0.25); }

.raise {
  background: rgba(201, 168, 76, 0.12);
  color: var(--color-gold);
  border-color: rgba(201, 168, 76, 0.25);
}

.raise:hover, .raise.active { background: rgba(201, 168, 76, 0.2); }

.amount {
  font-size: 0.75rem;
  opacity: 0.8;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.4; }
}
</style>