import { apiRequest } from './http'

import type {
  MigraineAttack,
  MigraineAttackFilter,
  MigraineAttackStatistics,
  PagedResponse,
  SaveMigraineAttackRequest,
} from '@/types/migraineAttack'

function buildQueryString(params: object): string {
  const searchParams = new URLSearchParams()

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.set(key, String(value))
    }
  }

  const query = searchParams.toString()

  return query ? `?${query}` : ''
}

// Получение списка приступов с фильтрацией и пагинацией
export function getMigraineAttacks(filter: MigraineAttackFilter = {}) {
  return apiRequest<PagedResponse<MigraineAttack>>(`/migraine-attacks${buildQueryString(filter)}`)
}

// Получение приступа по идентификатору
export function getMigraineAttackById(id: string) {
  return apiRequest<MigraineAttack>(`/migraine-attacks/${encodeURIComponent(id)}`)
}

// Создание нового приступа
export function createMigraineAttack(data: SaveMigraineAttackRequest) {
  return apiRequest<MigraineAttack>('/migraine-attacks', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

// Редактирование существующего приступа
export function updateMigraineAttack(id: string, data: SaveMigraineAttackRequest) {
  return apiRequest<MigraineAttack>(`/migraine-attacks/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

// Удаление приступа
export function deleteMigraineAttack(id: string) {
  return apiRequest<void>(`/migraine-attacks/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
}

// Получение статистики приступов
export function getMigraineAttackStatistics(
  period: Pick<MigraineAttackFilter, 'from' | 'to'> = {},
) {
  return apiRequest<MigraineAttackStatistics>(
    `/migraine-attacks/statistics${buildQueryString(period)}`,
  )
}
