<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <main>
    <h1>Game Room: {{ route.params.roomId }}</h1>
  </main>
</template>