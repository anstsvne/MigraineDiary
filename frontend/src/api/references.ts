import { apiRequest } from './http'

import type { Symptom, Trigger, Medication, SaveMedicationRequest } from '@/types/reference'

// Получение общего справочника симптомов
export function getSymptoms(): Promise<Symptom[]> {
  return apiRequest<Symptom[]>('/symptoms')
}

// Получение общего справочника триггеров
export function getTriggers(): Promise<Trigger[]> {
  return apiRequest<Trigger[]>('/triggers')
}

// Получение препаратов текущего пользователя
export function getMedications(): Promise<Medication[]> {
  return apiRequest<Medication[]>('/medications')
}

// Получение препарата по идентификатору
export function getMedicationById(id: string): Promise<Medication> {
  return apiRequest<Medication>(`/medications/${encodeURIComponent(id)}`)
}

// Создание пользовательского препарата
export function createMedication(data: SaveMedicationRequest): Promise<Medication> {
  return apiRequest<Medication>('/medications', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

// Редактирование названия препарата
export function updateMedication(id: string, data: SaveMedicationRequest): Promise<Medication> {
  return apiRequest<Medication>(`/medications/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

// Удаление пользовательского препарата
export function deleteMedication(id: string): Promise<void> {
  return apiRequest<void>(`/medications/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
}
