
<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <main class="page">
    <div class="container">
      <header class="header">
        <div class="logo">MigraineDiary</div>

        <div class="header-actions">
          <template v-if="authStore.isAuthenticated">

            <RouterLink to="/profile" class="auth-link">
              Личный кабинет
            </RouterLink>

            <span class="header-label">
              {{ authStore.userName }}
            </span>

            <button type="button"
                    class="auth-link"
                    @click="authStore.signOut">
              Выйти
            </button>
          </template>

          <RouterLink v-else to="/login" class="auth-link">
            Войти
          </RouterLink>
        </div>

      </header>

      <section class="welcome">
        <span class="eyebrow">Ваш личный дневник</span>

        <h1>Понимайте свою мигрень лучше</h1>

        <p class="description">
          Записывайте приступы мигрени, отслеживайте
          изменения самочувствия и анализируйте
          историю наблюдений в одном месте.
        </p>

        <div class="notice">
          Приложение находится в разработке.
          Скоро здесь появится личный кабинет.
        </div>
      </section>

      <section class="features">
        <article class="feature">
          <span class="feature-number">01</span>
          <h2>Дневник приступов</h2>
          <p>
            Фиксируйте дату, продолжительность,
            интенсивность и особенности боли.
          </p>
        </article>

        <article class="feature">
          <span class="feature-number">02</span>
          <h2>История наблюдений</h2>
          <p>
            Просматривайте предыдущие записи
            и отслеживайте изменения.
          </p>
        </article>

        <article class="feature">
          <span class="feature-number">03</span>
          <h2>Статистика</h2>
          <p>
            Изучайте частоту приступов
            с помощью графиков и диаграмм.
          </p>
        </article>
      </section>
    </div>
  </main>
</template>

<style scoped>
  .page {
    min-height: 100vh;
  }

  .container {
    width: min(1100px, calc(100% - 40px));
    margin: 0 auto;
  }

  .header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 30px 0;
  }

  .logo {
    color: #6554bc;
    font-size: 24px;
    font-weight: 700;
  }

  .header-label {
    color: #82899a;
    font-size: 14px;
  }

  .welcome {
    max-width: 740px;
    padding: 100px 0 70px;
  }

  .eyebrow {
    color: #6554bc;
    font-size: 15px;
    font-weight: 600;
  }

  h1 {
    font-size: clamp(32px, 5vw, 52px);
    line-height: 1.15;
    margin: 20px 0;
  }

  .description {
    color: #737b8e;
    font-size: 18px;
    line-height: 1.7;
  }

  .notice {
    display: inline-block;
    margin-top: 24px;
    padding: 15px 20px;
    border-radius: 12px;
    background: #eeebfc;
    color: #6554bc;
    font-size: 14px;
  }

  .features {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
    padding-bottom: 60px;
  }

  .feature {
    background: white;
    border: 1px solid #e9eaf0;
    border-radius: 16px;
    padding: 28px;
  }

  .feature-number {
    color: #6554bc;
    font-size: 14px;
    font-weight: 600;
  }

  .feature h2 {
    margin: 20px 0 12px;
    font-size: 20px;
  }

  .feature p {
    color: #737b8e;
    font-size: 15px;
    line-height: 1.6;
  }

  @media (max-width: 768px) {
    .welcome {
      padding: 60px 0 40px;
    }

    .features {
      grid-template-columns: 1fr;
    }
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .auth-link {
    padding: 10px 20px;
    border: 1px solid #6554bc;
    border-radius: 10px;
    background: transparent;
    color: #6554bc;
    font: inherit;
    font-size: 14px;
    text-decoration: none;
    cursor: pointer;
  }

    .auth-link:hover {
      background: #eeebfc;
    }

</style>
