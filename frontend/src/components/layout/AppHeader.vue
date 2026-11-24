<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits<{
  toggleMenu: []
}>()

const router = useRouter()
const authStore = useAuthStore()

async function logout() {
  authStore.signOut()
  await router.replace('/')
}
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button
        class="menu-button"
        type="button"
        aria-label="Открыть меню"
        @click="emit('toggleMenu')"
      >
        ☰
      </button>

      <span class="app-label">Личный кабинет</span>
    </div>

    <div class="header-right">
      <span class="user-name">
        {{ authStore.userName }}
      </span>

      <button class="logout-button" type="button" @click="logout">Выйти</button>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  height: 76px;
  padding: 0 34px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  background: #fff;
  border-bottom: 1px solid #e9eaf0;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.app-label {
  font-weight: 600;
  font-size: 16px;
}

.user-name {
  font-size: 14px;
  color: #596377;
}

.logout-button {
  padding: 10px 18px;
  border: 1px solid #e4e0f5;
  border-radius: 9px;
  background: #f7f5fd;
  color: #6554bc;
  font: inherit;
  cursor: pointer;
}

.logout-button:hover {
  background: #eeebfc;
}

.menu-button {
  display: none;
  border: none;
  background: transparent;
  font-size: 24px;
  cursor: pointer;
}

@media (max-width: 800px) {
  .app-header {
    height: 66px;
    padding: 0 18px;
  }

  .menu-button {
    display: block;
  }

  .user-name {
    display: none;
  }

  .header-right {
    gap: 10px;
  }
}
</style>
