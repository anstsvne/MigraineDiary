export type PainLocation =
  'LeftSide' | 'RightSide' | 'BothSides' | 'Forehead' | 'Temples' | 'Occiput' | 'Other'

export interface AttackSymptomRequest {
  symptomId: string
  description?: string | null
}

export interface AttackTriggerRequest {
  triggerId: string
  description?: string | null
}

export interface AttackMedicationRequest {
  medicationId: string
  dosage?: string | null
  takenAt?: string | null
}

export interface SaveMigraineAttackRequest {
  startAt: string
  endAt: string
  intensity: number
  painLocation: PainLocation
  painLocationDescription?: string | null
  notes?: string | null
  symptoms?: AttackSymptomRequest[]
  triggers?: AttackTriggerRequest[]
  medications?: AttackMedicationRequest[]
}

export interface AttackSymptomResponse {
  symptomId: string
  name: string
  description: string | null
}

export interface AttackTriggerResponse {
  triggerId: string
  name: string
  description: string | null
}

export interface AttackMedicationResponse {
  id: string
  medicationId: string
  name: string
  dosage: string | null
  takenAt: string | null
}

export interface MigraineAttack {
  id: string
  startAt: string
  endAt: string
  durationMinutes: number
  intensity: number
  painLocation: PainLocation
  painLocationDescription: string | null
  notes: string | null
  createdAt: string
  symptoms: AttackSymptomResponse[]
  triggers: AttackTriggerResponse[]
  medications: AttackMedicationResponse[]
}

export interface MigraineAttackFilter {
  page?: number
  pageSize?: number
  from?: string
  to?: string
  minIntensity?: number
  maxIntensity?: number
  painLocation?: PainLocation
  symptomId?: string
  triggerId?: string
  medicationId?: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface StatisticsItem {
  id: string
  name: string
  count: number
}

export interface MigraineAttackStatistics {
  attackCount: number
  averageIntensity: number
  averageDurationMinutes: number
  symptoms: StatisticsItem[]
  triggers: StatisticsItem[]
  medications: StatisticsItem[]
}
