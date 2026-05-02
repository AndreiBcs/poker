<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '@/stores/game'
import { useAuthStore } from '@/stores/auth'
import OpponentSeat from './OpponentSeat.vue'
import LocalPlayer from './PlayerSeat.vue'
import CommunityCards from './CommunityCards.vue'
import ActionPanel from './ActionPanel.vue'

const game  = useGameStore()
const auth  = useAuthStore()

const opponents = computed(() =>
  game.players.filter(p => p.userId !== auth.userId)
)

const localPlayer = computed(() =>
  game.players.find(p => p.userId === auth.userId) ?? null
)

const emit = defineEmits<{
  (e: 'fold'): void
  (e: 'call'): void
  (e: 'raise', amount: number): void
  (e: 'check'): void
}>()
</script>

<template>
  <div class="table-wrapper">
    <div class="table-felt">

      <!-- OPPONENTS ARC -->
      <div class="opponents-arc">
        <OpponentSeat
          v-for="player in opponents"
          :key="player.userId"
          :player="player"
        />
      </div>

      <!-- COMMUNITY CARDS -->
      <div class="table-center">
        <CommunityCards :cards="game.communityCards" :pot="game.pot" />
      </div>

      <!-- LOCAL PLAYER ROW -->
      <div class="local-row">
        <div class="local-left">
          <!-- spacer or future side info -->
        </div>

        <LocalPlayer v-if="localPlayer" :player="localPlayer" />

        <div class="local-right">
          <ActionPanel
            v-if="game.isMyTurn && localPlayer"
            :currentBet="game.currentBet"
            :player="localPlayer"
            @fold="emit('fold')"
            @call="emit('call')"
            @raise="(amt) => emit('raise', amt)"
            @check="emit('check')"
          />
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
.table-wrapper {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
}

.table-felt {
  width: 100%;
  max-width: 900px;
  min-height: 560px;
  background:
    radial-gradient(ellipse at center, #1a4a2e 0%, #0f2d1a 60%, #0a1f12 100%);
  border-radius: 120px;
  border: 6px solid #2a1f0e;
  outline: 3px solid rgba(201, 168, 76, 0.15);
  box-shadow:
    0 0 0 8px #1a1208,
    0 24px 80px rgba(0,0,0,0.8);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 2rem 3rem;
  position: relative;
}

/* OPPONENTS */
.opponents-arc {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  gap: 1.5rem;
  flex-wrap: wrap;
  padding-bottom: 0.5rem;
}

/* give the arc a subtle curve by nudging middle seats up */
.opponents-arc > *:nth-child(1),
.opponents-arc > *:last-child  { transform: translateY(16px); }

.opponents-arc > *:nth-child(2),
.opponents-arc > *:nth-last-child(2) { transform: translateY(6px); }

/* CENTER */
.table-center {
  display: flex;
  justify-content: center;
  align-items: center;
  flex: 1;
}

/* LOCAL PLAYER */
.local-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  padding-top: 0.5rem;
}

.local-left, .local-right {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: flex-end;
}
</style>