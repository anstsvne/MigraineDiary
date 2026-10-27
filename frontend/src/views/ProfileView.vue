<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    const user = await authStore.loadCurrentUser()

    if (!user) {
      await router.replace({
        name: 'login',
        query: { redirect: '/profile' },
      })
    }
  } catch (error) {
    if (!authStore.hasValidToken()) {
      await router.replace({
        name: 'login',
        query: { redirect: '/profile' },
      })
      return
    }

    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось загрузить данные пользователя.'
  } finally {
    isLoading.value = false
  }
})

function logout() {
  authStore.signOut()
  router.push('/')
}
</script>

<template>
  <main class="profile-page">
    <div class="profile-container">
      <header class="profile-header">
        <RouterLink to="/">← На главную</RouterLink>
        <h1>Личный кабинет</h1>
      </header>

      <p v-if="isLoading">Загрузка данных...</p>

      <div v-else-if="errorMessage" class="profile-card">
        <p role="alert">{{ errorMessage }}</p>
        <RouterLink to="/login">Перейти ко входу</RouterLink>
      </div>

      <div v-else-if="authStore.currentUser" class="profile-card">
        <h2>Информация о пользователе</h2>

        <div class="profile-field">
          <span>Имя</span>
          <strong>{{ authStore.currentUser.name }}</strong>
        </div>

        <div class="profile-field">
          <span>Электронная почта</span>
          <strong>{{ authStore.currentUser.email }}</strong>
        </div>

        <div class="profile-field">
          <span>Дата регистрации</span>
          <strong>
            {{ new Date(authStore.currentUser.createdAt).toLocaleDateString('ru-RU') }}
          </strong>
        </div>

        <button @click="logout">Выйти из аккаунта</button>
      </div>

      <p v-else>Необходимо авторизоваться.</p>
    </div>
  </main>
</template>

<style scoped>
.profile-page {
  min-height: 100vh;
  padding: 50px 20px;
}

.profile-container {
  width: 100%;
  max-width: 720px;
  margin: 0 auto;
}

.profile-header a,
.profile-card a {
  color: #6554bc;
  text-decoration: none;
}

h1 {
  margin: 30px 0;
  font-size: 36px;
}

.profile-card {
  padding: 32px;
  background: white;
  border: 1px solid #e9eaf0;
  border-radius: 18px;
}

h2 {
  margin: 0 0 28px;
  font-size: 22px;
}

.profile-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 18px 0;
  border-bottom: 1px solid #e9eaf0;
}

.profile-field span {
  color: #737b8e;
  font-size: 14px;
}

button {
  margin-top: 28px;
  padding: 12px 22px;
  background: #6554bc;
  color: white;
  border: none;
  border-radius: 10px;
  font: inherit;
  cursor: pointer;
}

button:hover {
  background: #5543af;
}
</style>
