<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div>
        <span class="eyebrow">ОБЗОР</span>
        <h1>Добро пожаловать, {{ authStore.userName }}!</h1>
        <p>Здесь будет собрана информация о ваших приступах мигрени и показателях здоровья.</p>
      </div>

      <RouterLink to="/profile" class="profile-link"> Мой профиль </RouterLink>
    </header>

    <section class="welcome-card">
      <div>
        <h2>Начните вести дневник мигрени</h2>
        <p>
          Записывайте приступы, отмечайте симптомы, возможные триггеры и принимаемые препараты. Это
          поможет анализировать историю наблюдений.
        </p>
      </div>

      <span class="status">Функция в разработке</span>
    </section>

    <h2 class="section-title">Разделы дневника</h2>

    <section class="feature-grid">
      <article class="feature-card">
        <span class="feature-icon">01</span>
        <h3>Приступы мигрени</h3>
        <p>История приступов, интенсивность боли, симптомы и возможные триггеры.</p>
        <span class="coming-soon">Скоро</span>
      </article>

      <article class="feature-card">
        <span class="feature-icon">02</span>
        <h3>Давление и пульс</h3>
        <p>Записи артериального давления, частоты пульса и времени измерений.</p>
        <span class="coming-soon">Скоро</span>
      </article>

      <article class="feature-card">
        <span class="feature-icon">03</span>
        <h3>Препараты</h3>
        <p>Личный список лекарственных средств и история их использования.</p>
        <span class="coming-soon">Скоро</span>
      </article>

      <article class="feature-card">
        <span class="feature-icon">04</span>
        <h3>Статистика</h3>
        <p>Частота приступов, продолжительность, интенсивность и другие показатели.</p>
        <span class="coming-soon">Скоро</span>
      </article>
    </section>
  </div>
</template>

<style scoped>
.dashboard {
  width: 100%;
}

.dashboard-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 32px;
}

.eyebrow {
  color: #6554bc;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

h1 {
  margin: 12px 0;
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.25;
}

.dashboard-header p,
.welcome-card p,
.feature-card p {
  color: #737b8e;
  line-height: 1.65;
}

.profile-link {
  padding: 12px 18px;
  border: 1px solid #ded9f5;
  border-radius: 10px;
  color: #6554bc;
  background: white;
  text-decoration: none;
  white-space: nowrap;
}

.profile-link:hover {
  background: #f0edfc;
}

.welcome-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 24px;
  padding: 32px;
  border-radius: 18px;
  background: #eeebfc;
  margin-bottom: 40px;
}

.welcome-card h2 {
  margin: 0 0 12px;
  color: #44368e;
}

.welcome-card p {
  max-width: 650px;
  margin-bottom: 0;
}

.status {
  color: #6554bc;
  font-size: 13px;
  white-space: nowrap;
}

.section-title {
  margin: 0 0 20px;
  font-size: 23px;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px;
}

.feature-card {
  padding: 28px;
  background: white;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.feature-icon {
  display: inline-flex;
  padding: 12px;
  color: #6554bc;
  background: #f0edfc;
  border-radius: 10px;
  font-weight: 700;
}

.feature-card h3 {
  margin: 20px 0 12px;
  font-size: 19px;
}

.coming-soon {
  display: inline-block;
  margin-top: 16px;
  color: #8c90a0;
  font-size: 13px;
}

@media (max-width: 800px) {
  .dashboard-header,
  .welcome-card {
    flex-direction: column;
  }

  .feature-grid {
    grid-template-columns: 1fr;
  }

  .welcome-card,
  .feature-card {
    padding: 24px;
  }
}
</style>
