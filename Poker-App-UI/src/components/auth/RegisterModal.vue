<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import FloatingInput from '@/components/ui/FloatingInput.vue'

const router = useRouter()

const username = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

async function handleSubmit() {
  error.value = null

  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  loading.value = true

  try {
    const response = await fetch('https://your-users-service/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: username.value,
        email: email.value,
        password: password.value
      })
    })

    if (!response.ok) {
      const data = await response.json()
      error.value = data.message ?? 'Registration failed, please try again'
      return
    }

    router.push({ name: 'login' })
  } catch {
    error.value = 'Could not reach the server, try again'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="auth-wrapper">
    <div class="auth-box">
      <h1>Sign Up</h1>

      <p class="auth-prompt">
        Already have an account?
        <button class="link-btn" @click="router.push({ name: 'login' })">Log in</button>
      </p>

      <form @submit.prevent="handleSubmit">
        <FloatingInput v-model="username" id="username" label="Username" type="text" />
        <FloatingInput v-model="email"    id="email"    label="Email"    type="email" />
        <FloatingInput v-model="password" id="password" label="Password" type="password" />
        <FloatingInput
          v-model="confirmPassword"
          id="confirmPassword"
          label="Confirm Password"
          type="password"
        />

        <p v-if="error" class="error">{{ error }}</p>

        <button class="btn btn-primary" style="width: 100%" type="submit" :disabled="loading">
          {{ loading ? 'Creating account...' : 'Sign Up' }}
        </button>
      </form>
    </div>
  </section>
</template>

<style scoped>
.auth-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
}

.auth-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
  padding: 2.5rem 2rem;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  width: 320px;
  background: var(--color-surface);
}

h1 {
  font-family: var(--font-display);
  color: #f0e8d8;
  margin: 0;
}

.auth-prompt {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.link-btn {
  background: none;
  border: none;
  color: var(--color-gold);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0;
  text-decoration: underline;
}

form {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
  width: 100%;
}

.error {
  margin: 0;
  color: #e05555;
  font-size: 0.85rem;
  align-self: flex-start;
}
</style>