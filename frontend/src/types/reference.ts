export interface ReferenceItem {
  id: string
  name: string
}

export type Symptom = ReferenceItem

export type Trigger = ReferenceItem

export type Medication = ReferenceItem

export interface SaveMedicationRequest {
  name: string
}
