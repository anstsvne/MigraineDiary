import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import { setUnauthorizedHandler } from '@/api/http'
import { useAuthStore } from '@/stores/auth'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

setUnauthorizedHandler(() => {
  const authStore = useAuthStore(pinia)

  authStore.signOut()

  const currentRoute = router.currentRoute.value

  if (currentRoute.name === 'login') {
    return
  }

  const redirect = currentRoute.meta.requiresAuth ? currentRoute.fullPath : undefined

  void router.replace({
    name: 'login',
    query: redirect ? { redirect } : {},
  })
})

app.mount('#app')
