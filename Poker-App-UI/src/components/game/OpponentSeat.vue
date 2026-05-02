<script setup lang="ts">
import ChipStack from './ChipStack.vue'
import type { Player } from '@/stores/game'

defineProps<{ player: Player }>()
</script>

<template>
  <div class="seat" :class="{ folded: player.status === 'folded', active: player.isTurn, disconnected: player.status === 'disconnected' }">

    <div class="cards">
      <div class="card-back" v-for="i in 2" :key="i" :class="{ folded: player.status === 'folded' }"></div>
    </div>

    <div class="seat-body">
      <div class="seat-header">
        <span class="username">{{ player.username }}</span>
      </div>

      <ChipStack :amount="player.chips" />

      <div class="last-action" v-if="player.status !== 'waiting' && player.status !== 'disconnected'">
        <span :class="['action-badge', player.status]">
          {{ player.status === 'folded'    ? 'Fold'
           : player.status === 'allIn'     ? 'All In'
           : player.bet > 0               ? `Bet ${player.bet}`
           : 'Check' }}
        </span>
      </div>

      <div class="turn-indicator" v-if="player.isTurn">
        <span>Thinking...</span>
        <div class="turn-bar"><div class="turn-fill"></div></div>
      </div>

      <div class="disconnected-label" v-if="player.status === 'disconnected'">
        Disconnected
      </div>
    </div>

  </div>
</template>

<style scoped>
.seat {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  transition: opacity 0.3s;
}

.seat.folded    { opacity: 0.4; }
.seat.disconnected { opacity: 0.3; }

.seat.active .seat-body {
  border-color: var(--color-gold);
  box-shadow: 0 0 12px rgba(201, 168, 76, 0.25);
}

/* CARDS */
.cards {
  display: flex;
  gap: 4px;
}

.card-back {
  width: 28px;
  height: 40px;
  border-radius: 4px;
  background: repeating-linear-gradient(
    135deg,
    #1a2a4a 0px, #1a2a4a 4px,
    #162240 4px, #162240 8px
  );
  border: 1px solid rgba(255,255,255,0.1);
  transition: opacity 0.3s;
}

.card-back.folded {
  transform: rotate(10deg);
  opacity: 0.3;
}

/* BODY */
.seat-body {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 8px 10px;
  display: flex;
  flex-direction: column;
  gap: 5px;
  min-width: 110px;
  align-items: center;
  transition: border-color 0.3s, box-shadow 0.3s;
}

.seat-header {
  display: flex;
  align-items: center;
  gap: 5px;
}

.username {
  font-size: 0.78rem;
  font-weight: 500;
  color: #f0e8d8;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 80px;
}

.dealer-chip {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  background: var(--color-gold);
  color: #0d1117;
  font-size: 0.6rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* ACTION BADGE */
.action-badge {
  font-size: 0.7rem;
  font-weight: 500;
  padding: 2px 8px;
  border-radius: 99px;
}

.action-badge.folded {
  background: rgba(224, 85, 85, 0.15);
  color: #e05555;
  border: 1px solid rgba(224, 85, 85, 0.25);
}

.action-badge.allIn {
  background: rgba(201, 168, 76, 0.15);
  color: var(--color-gold);
  border: 1px solid rgba(201, 168, 76, 0.3);
}

.action-badge.active {
  background: rgba(45, 106, 79, 0.2);
  color: #4ade80;
  border: 1px solid rgba(45, 106, 79, 0.4);
}

/* TURN TIMER BAR */
.turn-indicator {
  display: flex;
  flex-direction: column;
  gap: 3px;
  width: 100%;
}

.turn-indicator span {
  font-size: 0.65rem;
  color: var(--color-gold);
  text-align: center;
}

.turn-bar {
  height: 3px;
  background: rgba(255,255,255,0.08);
  border-radius: 99px;
  overflow: hidden;
  width: 100%;
}

.turn-fill {
  height: 100%;
  width: 100%;
  background: var(--color-gold);
  border-radius: 99px;
  animation: drain 30s linear forwards;
}

@keyframes drain {
  from { width: 100%; }
  to   { width: 0%; }
}

.disconnected-label {
  font-size: 0.65rem;
  color: #e05555;
}
</style>