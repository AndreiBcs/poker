<script setup lang="ts">
defineProps<{ amount: number }>()

function format(n: number): string {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K'
  return n.toString()
}
</script>

<template>
  <div class="chip-stack">
    <div class="chip-icon">
      <div class="chip-inner">$</div>
    </div>
    <span class="chip-amount">{{ format(amount) }}</span>
  </div>
</template>

<style scoped>
.chip-stack {
  display: flex;
  align-items: center;
  gap: 5px;
}

.chip-icon {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: var(--color-gold);
  border: 2px dashed #0d1117;
  outline: 2px solid var(--color-gold);
  display: flex;
  align-items: center;
  justify-content: center;
}

.chip-inner {
  font-size: 0.6rem;
  font-weight: 700;
  color: #0d1117;
}

.chip-amount {
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--color-gold);
  letter-spacing: 0.03em;
}
</style>