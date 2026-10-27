
import { apiRequest } from './http'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  userId: string
  name: string
  email: string
}

export interface RegisterRequest {
  name: string
  email: string
  password: string
  confirmPassword: string
}

export interface RegisterResponse {
  id: string
  name: string
  email: string
  createdAt: string
}

export interface MeResponse {
  id: string
  name: string
  email: string
  createdAt: string
}

export function login(data: LoginRequest) {
  return apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export function register(data: RegisterRequest) {
  return apiRequest<RegisterResponse>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export function getMe(token: string) {
  return apiRequest<MeResponse>('/auth/me', {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  })
}
