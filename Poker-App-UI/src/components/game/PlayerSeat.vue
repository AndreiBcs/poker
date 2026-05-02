<script setup lang="ts">
import ChipStack from './ChipStack.vue'
import type { Player } from '@/stores/game'

defineProps<{ player: Player }>()

const SUITS: Record<string, string> = {
  hearts: '♥', diamonds: '♦', clubs: '♣', spades: '♠'
}

const RED_SUITS = new Set(['hearts', 'diamonds'])
</script>

<template>
  <div class="local-player">

    <div class="hole-cards">
      <template v-if="player.hand.length === 2">
        <div class="card" v-for="card in player.hand" :key="card.rank + card.suit">
          <span class="card-rank">{{ card.rank }}</span>
          <span class="card-suit" :class="{ red: RED_SUITS.has(card.suit) }">
            {{ SUITS[card.suit] }}
          </span>
        </div>
      </template>
      <template v-else>
        <div class="card placeholder" v-for="i in 2" :key="i"></div>
      </template>
    </div>

    <div class="player-info" :class="{ active: player.isTurn }">
      <div class="player-header">
        <span class="username">{{ player.username }}</span>
        <span class="you-badge">You</span>
      </div>
      <ChipStack :amount="player.chips" />
      <div class="turn-glow" v-if="player.isTurn">Your turn</div>
    </div>

  </div>
</template>

<style scoped>
.local-player {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.hole-cards {
  display: flex;
  gap: 8px;
}

.card {
  width: 62px;
  height: 88px;
  border-radius: var(--radius-md);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 6px 8px;
  background: #fff;
  border: 1px solid rgba(0,0,0,0.1);
  box-shadow: 0 6px 24px rgba(0,0,0,0.5);
  animation: dealIn 0.4s ease both;
}

.card.placeholder {
  background: rgba(255,255,255,0.04);
  border: 1px dashed rgba(255,255,255,0.08);
  box-shadow: none;
  animation: none;
}

.card-rank {
  font-family: var(--font-display);
  font-size: 1.1rem;
  font-weight: 700;
  color: #1a1a1a;
  line-height: 1;
}

.card-suit {
  font-size: 1.25rem;
  align-self: flex-end;
  color: #1a1a1a;
  line-height: 1;
}

.card-suit.red { color: #c0392b; }

/* PLAYER INFO */
.player-info {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 8px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  min-width: 130px;
  transition: border-color 0.3s, box-shadow 0.3s;
}

.player-info.active {
  border-color: var(--color-gold);
  box-shadow: 0 0 16px rgba(201, 168, 76, 0.3);
}

.player-header {
  display: flex;
  align-items: center;
  gap: 5px;
}

.username {
  font-size: 0.85rem;
  font-weight: 500;
  color: #f0e8d8;
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
}

.you-badge {
  font-size: 0.65rem;
  font-weight: 500;
  padding: 1px 6px;
  border-radius: 99px;
  background: rgba(201, 168, 76, 0.12);
  color: var(--color-gold);
  border: 1px solid rgba(201, 168, 76, 0.25);
}

.turn-glow {
  font-size: 0.7rem;
  color: var(--color-gold);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes dealIn {
  from { transform: translateY(20px) rotate(-3deg); opacity: 0; }
  to   { transform: translateY(0)    rotate(0deg);  opacity: 1; }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.4; }
}
</style>