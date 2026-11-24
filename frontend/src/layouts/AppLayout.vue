<script setup lang="ts">
import { RouterView } from 'vue-router'
import { ref } from 'vue'

import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppHeader from '@/components/layout/AppHeader.vue'

const isMenuOpen = ref(false)

function closeMenu() {
  isMenuOpen.value = false
}

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value
}
</script>

<template>
  <div class="app-layout">
    <button
      v-if="isMenuOpen"
      class="menu-overlay"
      type="button"
      aria-label="Закрыть меню"
      @click="closeMenu"
    />

    <AppSidebar :is-open="isMenuOpen" @close="closeMenu" />

    <div class="workspace">
      <AppHeader @toggle-menu="toggleMenu" />

      <main class="workspace-content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  background: #f7f8fc;
}

.workspace {
  min-height: 100vh;
  margin-left: 250px;
}

.workspace-content {
  max-width: 1440px;
  padding: 36px;
  margin: 0 auto;
}

.menu-overlay {
  display: none;
}

@media (max-width: 800px) {
  .workspace {
    margin-left: 0;
  }

  .workspace-content {
    padding: 24px 16px;
  }

  .menu-overlay {
    display: block;
    position: fixed;
    inset: 0;
    z-index: 15;
    width: 100%;
    height: 100%;
    padding: 0;
    border: 0;
    background: rgba(20, 27, 45, 0.4);
    cursor: pointer;
  }
}
</style>
