export interface VitalMeasurement {
  id: string
  systolicPressure: number
  diastolicPressure: number
  pulse: number
  measuredAt: string
  notes: string | null
  createdAt: string
}

export interface SaveVitalMeasurementRequest {
  systolicPressure: number
  diastolicPressure: number
  pulse: number
  measuredAt: string
  notes?: string | null
}

export interface VitalMeasurementPagination {
  page?: number
  pageSize?: number
}
