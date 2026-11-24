<script setup lang="ts">
defineProps<{
  isOpen: boolean
}>()

const emit = defineEmits<{
  close: []
}>()
</script>

<template>
  <aside class="sidebar" :class="{ 'sidebar--open': isOpen }">
    <div class="brand">
      <RouterLink to="/dashboard" @click="emit('close')"> MigraineDiary </RouterLink>
      <span>Дневник здоровья</span>
    </div>

    <nav class="navigation" aria-label="Основная навигация">
      <p class="nav-title">ОСНОВНОЕ</p>

      <RouterLink to="/dashboard" class="nav-link" active-class="active" @click="emit('close')">
        Обзор
      </RouterLink>

      <span class="nav-link disabled">
        Дневник приступов
        <small>Скоро</small>
      </span>

      <span class="nav-link disabled">
        Давление и пульс
        <small>Скоро</small>
      </span>

      <span class="nav-link disabled">
        Препараты
        <small>Скоро</small>
      </span>

      <span class="nav-link disabled">
        Статистика
        <small>Скоро</small>
      </span>

      <p class="nav-title">АККАУНТ</p>

      <RouterLink to="/profile" class="nav-link" active-class="active" @click="emit('close')">
        Мой профиль
      </RouterLink>
    </nav>
  </aside>
</template>

<style scoped>
.sidebar {
  width: 250px;
  height: 100vh;
  position: fixed;
  top: 0;
  left: 0;
  z-index: 20;
  padding: 28px 18px;
  background: #fff;
  border-right: 1px solid #e9eaf0;
  overflow-y: auto;
}

.brand {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 0 12px 36px;
}

.brand a {
  color: #6554bc;
  font-size: 22px;
  font-weight: 750;
  text-decoration: none;
}

.brand span {
  color: #8a90a2;
  font-size: 13px;
}

.navigation {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.nav-title {
  margin: 24px 12px 10px;
  color: #969cad;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1px;
}

.nav-link {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 13px 14px;
  border-radius: 10px;
  color: #596377;
  font-size: 14px;
  text-decoration: none;
}

a.nav-link:hover {
  background: #f5f3fd;
  color: #6554bc;
}

.nav-link.active {
  background: #eeebfc;
  color: #6554bc;
  font-weight: 600;
}

.disabled {
  color: #a5aab7;
  cursor: default;
}

.disabled small {
  font-size: 11px;
  color: #a5aab7;
}

@media (max-width: 800px) {
  .sidebar {
    transform: translateX(-100%);
    transition: transform 0.2s ease;
  }

  .sidebar--open {
    transform: translateX(0);
  }
}
</style>
