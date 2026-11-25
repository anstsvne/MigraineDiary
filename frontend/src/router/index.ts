import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

import AppLayout from '@/layouts/AppLayout.vue'

import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'

import DashboardView from '@/views/DashboardView.vue'
import ProfileView from '@/views/ProfileView.vue'

import MigraineAttacksView from '@/views/MigraineAttacksView.vue'
import MigraineAttackCreateView from '@/views/MigraineAttackCreateView.vue'
import MigraineAttackDetailsView from '@/views/MigraineAttackDetailsView.vue'
import MigraineAttackEditView from '@/views/MigraineAttackEditView.vue'

import VitalMeasurementsView from '@/views/VitalMeasurementsView.vue'
import VitalMeasurementCreateView from '@/views/VitalMeasurementCreateView.vue'
import VitalMeasurementEditView from '@/views/VitalMeasurementEditView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
    },
    {
      path: '/register',
      name: 'register',
      component: RegisterView,
    },
    {
      path: '/app',
      component: AppLayout,
      redirect: '/dashboard',
      meta: {
        requiresAuth: true,
      },
      children: [
        {
          path: '/dashboard',
          name: 'dashboard',
          component: DashboardView,
        },
        {
          path: '/profile',
          name: 'profile',
          component: ProfileView,
        },
        {
          path: '/migraine-attacks',
          name: 'migraine-attacks',
          component: MigraineAttacksView,
        },
        {
          path: '/migraine-attacks/new',
          name: 'migraine-attack-create',
          component: MigraineAttackCreateView,
        },
        {
          path: '/migraine-attacks/:id/edit',
          name: 'migraine-attack-edit',
          component: MigraineAttackEditView,
        },
        {
          path: '/migraine-attacks/:id',
          name: 'migraine-attack-details',
          component: MigraineAttackDetailsView,
        },
        {
          path: '/vital-measurements',
          name: 'vital-measurements',
          component: VitalMeasurementsView,
        },
        {
          path: '/vital-measurements/new',
          name: 'vital-measurement-create',
          component: VitalMeasurementCreateView,
        },
        {
          path: '/vital-measurements/:id/edit',
          name: 'vital-measurement-edit',
          component: VitalMeasurementEditView,
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  if (!to.meta.requiresAuth) {
    return true
  }

  const authStore = useAuthStore()

  if (authStore.hasValidToken()) {
    return true
  }

  authStore.signOut()

  return {
    name: 'login',
    query: {
      redirect: to.fullPath,
    },
  }
})

export default router
