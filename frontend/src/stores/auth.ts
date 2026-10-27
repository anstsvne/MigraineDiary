import { ApiError } from '@/api/http'
import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

import { login, getMe, type LoginRequest, type MeResponse } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(sessionStorage.getItem('accessToken') ?? '')

  const userName = ref(sessionStorage.getItem('userName') ?? '')

  const expiresAt = ref(sessionStorage.getItem('expiresAt') ?? '')

  const currentUser = ref<MeResponse | null>(null)

  function hasValidToken() {
    return (
      Boolean(accessToken.value) &&
      Number.isFinite(Date.parse(expiresAt.value)) &&
      Date.parse(expiresAt.value) > Date.now()
    )
  }

  const isAuthenticated = computed(() => hasValidToken())

  function signOut() {
    accessToken.value = ''
    userName.value = ''
    expiresAt.value = ''
    currentUser.value = null

    sessionStorage.removeItem('accessToken')
    sessionStorage.removeItem('userName')
    sessionStorage.removeItem('expiresAt')
  }

  async function signIn(data: LoginRequest) {
    const response = await login(data)

    accessToken.value = response.accessToken
    userName.value = response.name
    expiresAt.value = response.expiresAt
    currentUser.value = null

    sessionStorage.setItem('accessToken', response.accessToken)

    sessionStorage.setItem('userName', response.name)

    sessionStorage.setItem('expiresAt', response.expiresAt)
  }

  async function loadCurrentUser() {
    if (!hasValidToken()) {
      signOut()
      return null
    }

    try {
      const user = await getMe(accessToken.value)

      currentUser.value = user
      userName.value = user.name

      sessionStorage.setItem('userName', user.name)

      return user
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        signOut()
      }

      throw error
    }
  }

  if (!hasValidToken()) {
    signOut()
  }

  return {
    accessToken,
    userName,
    currentUser,
    isAuthenticated,
    hasValidToken,
    signIn,
    signOut,
    loadCurrentUser,
  }
})
