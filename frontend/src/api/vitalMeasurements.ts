import { apiRequest } from './http'

import type { PagedResponse } from '@/types/migraineAttack'

import type {
  VitalMeasurement,
  SaveVitalMeasurementRequest,
  VitalMeasurementPagination,
} from '@/types/vitalMeasurement'

const endpoint = '/vital-measurements'

// История измерений с пагинацией
export function getVitalMeasurements(pagination: VitalMeasurementPagination = {}) {
  const params = new URLSearchParams()

  if (pagination.page !== undefined) {
    params.set('page', String(pagination.page))
  }

  if (pagination.pageSize !== undefined) {
    params.set('pageSize', String(pagination.pageSize))
  }

  const query = params.toString()

  return apiRequest<PagedResponse<VitalMeasurement>>(`${endpoint}${query ? `?${query}` : ''}`)
}

// Одно измерение по идентификатору
export function getVitalMeasurementById(id: string) {
  return apiRequest<VitalMeasurement>(`${endpoint}/${encodeURIComponent(id)}`)
}

// Создание измерения
export function createVitalMeasurement(data: SaveVitalMeasurementRequest) {
  return apiRequest<VitalMeasurement>(endpoint, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

// Редактирование измерения
export function updateVitalMeasurement(id: string, data: SaveVitalMeasurementRequest) {
  return apiRequest<VitalMeasurement>(`${endpoint}/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

// Удаление измерения
export function deleteVitalMeasurement(id: string) {
  return apiRequest<void>(`${endpoint}/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
}
