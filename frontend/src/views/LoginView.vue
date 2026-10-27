<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')

const isLoading = ref(false)
const errorMessage = ref('')

async function handleLogin() {
  if (isLoading.value) return

  errorMessage.value = ''
  isLoading.value = true

  try {
    await authStore.signIn({
      email: email.value.trim(),
      password: password.value,
    })

    // Возвращаем пользователя на запрошенную страницу.
    // Пока единственный защищённый маршрут — /profile.
    const redirect = route.query.redirect

    const destination = redirect === '/profile' ? '/profile' : '/'

    await router.replace(destination)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Произошла ошибка при входе.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <div class="login-card">
      <RouterLink to="/" class="back-link"> ← На главную </RouterLink>

      <h1>Добро пожаловать!</h1>

      <p class="description">Войдите в аккаунт, чтобы продолжить работу с дневником мигрени.</p>

      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label for="email">Электронная почта</label>

          <input
            id="email"
            v-model.trim="email"
            type="email"
            placeholder="example@mail.ru"
            autocomplete="username"
            required
          />
        </div>

        <div class="form-group">
          <label for="password">Пароль</label>

          <input
            id="password"
            v-model="password"
            type="password"
            placeholder="Введите пароль"
            autocomplete="current-password"
            required
          />
        </div>

        <p v-if="errorMessage" class="error" role="alert">
          {{ errorMessage }}
        </p>

        <button type="submit" :disabled="isLoading">
          {{ isLoading ? 'Выполняется вход...' : 'Войти' }}
        </button>
      </form>

      <p class="register-link">
        Ещё нет аккаунта?
        <RouterLink to="/register">Зарегистрироваться</RouterLink>
      </p>
    </div>
  </main>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 24px;
}

.login-card {
  width: 100%;
  max-width: 440px;
  padding: 40px;
  background: white;
  border: 1px solid #e9eaf0;
  border-radius: 20px;
}

.back-link {
  color: #6554bc;
  font-size: 14px;
  text-decoration: none;
}

h1 {
  margin: 30px 0 12px;
  font-size: 30px;
}

.description {
  color: #737b8e;
  line-height: 1.6;
  margin-bottom: 30px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 20px;
}

label {
  font-size: 14px;
  font-weight: 600;
}

input {
  padding: 13px 14px;
  border: 1px solid #dcdfe8;
  border-radius: 10px;
  font: inherit;
  outline: none;
}

input:focus {
  border-color: #6554bc;
}

button {
  width: 100%;
  padding: 14px;
  border: none;
  border-radius: 10px;
  background: #6554bc;
  color: white;
  font: inherit;
  font-weight: 600;
  cursor: pointer;
}

button:hover:not(:disabled) {
  background: #5543af;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #c0392b;
  font-size: 14px;
  line-height: 1.5;
}

@media (max-width: 480px) {
  .login-card {
    padding: 28px 22px;
  }
}
.register-link {
  margin-top: 24px;
  color: #737b8e;
  text-align: center;
  font-size: 14px;
}

.register-link a {
  color: #6554bc;
  text-decoration: none;
}
</style>
