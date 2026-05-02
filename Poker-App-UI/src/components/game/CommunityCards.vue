<script setup lang="ts">
import type { Card } from '@/stores/game'

defineProps<{
  cards: Card[]
  pot: number
}>()

const SUITS: Record<string, string> = {
  hearts: '♥', diamonds: '♦', clubs: '♣', spades: '♠'
}

const RED_SUITS = new Set(['hearts', 'diamonds'])
</script>

<template>
  <div class="community">

    <div class="board">
      <template v-for="i in 5" :key="i">
        <div v-if="cards[i - 1]" class="card revealed">
          <span class="card-rank">{{ cards[i - 1].rank }}</span>
          <span class="card-suit" :class="{ red: RED_SUITS.has(cards[i - 1].suit) }">
            {{ SUITS[cards[i - 1].suit] }}
          </span>
        </div>
        <div v-else class="card placeholder"></div>
      </template>
    </div>

    <div class="pot" v-if="pot > 0">
      <span class="pot-label">POT</span>
      <span class="pot-amount">{{ pot.toLocaleString() }}</span>
    </div>

  </div>
</template>

<style scoped>
.community {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.board {
  display: flex;
  gap: 8px;
}

.card {
  width: 52px;
  height: 74px;
  border-radius: var(--radius-sm);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 5px 6px;
}

.card.revealed {
  background: #fff;
  border: 1px solid rgba(0,0,0,0.1);
  box-shadow: 0 4px 16px rgba(0,0,0,0.4);
  animation: flipIn 0.4s ease both;
}

.card.placeholder {
  background: rgba(255,255,255,0.03);
  border: 1px dashed rgba(255,255,255,0.08);
}

.card-rank {
  font-family: var(--font-display);
  font-size: 1rem;
  font-weight: 700;
  color: #1a1a1a;
  line-height: 1;
}

.card-suit {
  font-size: 1.1rem;
  align-self: flex-end;
  color: #1a1a1a;
  line-height: 1;
}

.card-suit.red { color: #c0392b; }

.pot {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(201, 168, 76, 0.08);
  border: 1px solid rgba(201, 168, 76, 0.2);
  border-radius: 99px;
  padding: 4px 14px;
}

.pot-label {
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  color: rgba(201, 168, 76, 0.6);
}

.pot-amount {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-gold);
}

@keyframes flipIn {
  from { transform: rotateY(90deg) scale(0.8); opacity: 0; }
  to   { transform: rotateY(0deg)  scale(1);   opacity: 1; }
}
</style>