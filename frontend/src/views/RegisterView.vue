
<script setup lang="ts">
import { ref } from 'vue'
import { register } from '@/api/auth'

const name = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')

const isLoading = ref(false)
const isRegistered = ref(false)
const errorMessage = ref('')

async function handleRegister() {
  if (isLoading.value) return

  errorMessage.value = ''

  if (password.value.length < 8) {
    errorMessage.value = 'Пароль должен содержать минимум 8 символов.'
    return
  }

  if (password.value !== confirmPassword.value) {
    errorMessage.value = 'Пароли не совпадают.'
    return
  }

  isLoading.value = true

  try {
    await register({
      name: name.value.trim(),
      email: email.value.trim(),
      password: password.value,
      confirmPassword: confirmPassword.value,
    })

    isRegistered.value = true
  } catch (error) {
    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Не удалось зарегистрироваться.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <main class="register-page">
    <div class="register-card">
      <RouterLink to="/" class="back-link">
        ← На главную
      </RouterLink>

      <template v-if="isRegistered">
        <h1>Регистрация завершена!</h1>

        <p class="description">
          Аккаунт успешно создан. Теперь вы можете войти
          в MigraineDiary.
        </p>

        <RouterLink to="/login" class="submit-button">
          Перейти ко входу
        </RouterLink>
      </template>

      <template v-else>
        <h1>Создать аккаунт</h1>

        <p class="description">
          Зарегистрируйтесь, чтобы вести личный
          дневник мигрени.
        </p>

        <form @submit.prevent="handleRegister">
          <div class="form-group">
            <label for="name">Имя</label>
            <input id="name"
                   v-model.trim="name"
                   type="text"
                   placeholder="Введите имя"
                   autocomplete="name"
                   maxlength="100"
                   required />
          </div>

          <div class="form-group">
            <label for="email">Электронная почта</label>
            <input id="email"
                   v-model.trim="email"
                   type="email"
                   placeholder="example@mail.ru"
                   autocomplete="email"
                   maxlength="320"
                   required />
          </div>

          <div class="form-group">
            <label for="password">Пароль</label>
            <input id="password"
                   v-model="password"
                   type="password"
                   placeholder="Минимум 8 символов"
                   autocomplete="new-password"
                   minlength="8"
                   required />
          </div>

          <div class="form-group">
            <label for="confirmPassword">Подтвердите пароль</label>
            <input id="confirmPassword"
                   v-model="confirmPassword"
                   type="password"
                   placeholder="Повторите пароль"
                   autocomplete="new-password"
                   required />
          </div>

          <p v-if="errorMessage" class="error" role="alert">
            {{ errorMessage }}
          </p>

          <button type="submit" :disabled="isLoading">
            {{ isLoading ? 'Регистрация...' : 'Зарегистрироваться' }}
          </button>
        </form>

        <p class="login-link">
          Уже есть аккаунт?
          <RouterLink to="/login">Войти</RouterLink>
        </p>
      </template>
    </div>
  </main>
</template>

<style scoped>
  .register-page {
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 24px;
  }

  .register-card {
    width: 100%;
    max-width: 440px;
    padding: 40px;
    background: white;
    border: 1px solid #e9eaf0;
    border-radius: 20px;
  }

  .back-link,
  .login-link a {
    color: #6554bc;
    text-decoration: none;
  }

  .back-link {
    font-size: 14px;
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
    width: 100%;
  }

    input:focus {
      border-color: #6554bc;
    }

  button,
  .submit-button {
    display: block;
    width: 100%;
    padding: 14px;
    border: none;
    border-radius: 10px;
    background: #6554bc;
    color: white;
    font: inherit;
    font-weight: 600;
    text-align: center;
    text-decoration: none;
    cursor: pointer;
  }

    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    button:hover:not(:disabled),
    .submit-button:hover {
      background: #5543af;
    }

  .login-link {
    margin-top: 24px;
    color: #737b8e;
    text-align: center;
    font-size: 14px;
  }

  .error {
    color: #c0392b;
    font-size: 14px;
    line-height: 1.5;
  }

  @media (max-width: 480px) {
    .register-card {
      padding: 28px 22px;
    }
  }
</style>
