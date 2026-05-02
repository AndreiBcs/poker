<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue: string
  label: string
  id: string
  type?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const model = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})
</script>

<template>
  <div class="input-group">
    <input
      :type="type ?? 'text'"
      :id="id"
      v-model="model"
      placeholder=" "
      required
    />
    <label :for="id">{{ label }}</label>
  </div>
</template>

<style scoped>
.input-group {
  position: relative;
  width: 100%;
}

.input-group input {
  width: 100%;
  padding: 12px 10px;
  border: 2px solid #aaa;
  border-radius: 5px;
  outline: none;
  font-size: 1rem;
  background: white;
  box-sizing: border-box;
}

.input-group label {
  position: absolute;
  top: 50%;
  left: 10px;
  transform: translateY(-50%);
  background: white;
  padding: 0 5px;
  color: #666;
  transition: 0.2s ease;
  pointer-events: none;
  border-radius: 0;
}

.input-group input:focus + label,
.input-group input:not(:placeholder-shown) + label {
  top: -8px;
  font-size: 0.8rem;
  color: #333;
  border: #333 solid 1px;
  border-radius: 4px 4px 0 0;
}

.input-group input:focus {
  border-color: #333;
}
</style>