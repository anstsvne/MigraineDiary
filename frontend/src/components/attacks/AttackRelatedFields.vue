<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getSymptomLabel, getTriggerLabel } from '@/utils/referenceLabels'

import { getSymptoms, getTriggers, getMedications, createMedication } from '@/api/references'

import type { Symptom, Trigger, Medication } from '@/types/reference'

import type {
  AttackSymptomRequest,
  AttackTriggerRequest,
  AttackMedicationRequest,
} from '@/types/migraineAttack'

const props = defineProps<{
  symptoms: AttackSymptomRequest[]
  triggers: AttackTriggerRequest[]
  medications: AttackMedicationRequest[]
  disabled?: boolean
}>()

const emit = defineEmits<{
  'update:symptoms': [value: AttackSymptomRequest[]]
  'update:triggers': [value: AttackTriggerRequest[]]
  'update:medications': [value: AttackMedicationRequest[]]
}>()

const symptomOptions = ref<Symptom[]>([])
const triggerOptions = ref<Trigger[]>([])
const medicationOptions = ref<Medication[]>([])

const symptomsLoading = ref(false)
const triggersLoading = ref(false)
const medicationsLoading = ref(false)

const symptomsError = ref('')
const triggersError = ref('')
const medicationsError = ref('')

const newMedicationName = ref('')
const creatingMedication = ref(false)
const createMedicationError = ref('')

async function loadSymptoms() {
  symptomsLoading.value = true
  symptomsError.value = ''

  try {
    symptomOptions.value = await getSymptoms()
  } catch (error) {
    symptomsError.value = error instanceof Error ? error.message : 'Не удалось загрузить симптомы.'
  } finally {
    symptomsLoading.value = false
  }
}

async function loadTriggers() {
  triggersLoading.value = true
  triggersError.value = ''

  try {
    triggerOptions.value = await getTriggers()
  } catch (error) {
    triggersError.value = error instanceof Error ? error.message : 'Не удалось загрузить триггеры.'
  } finally {
    triggersLoading.value = false
  }
}

async function loadMedications() {
  medicationsLoading.value = true
  medicationsError.value = ''

  try {
    medicationOptions.value = await getMedications()
  } catch (error) {
    medicationsError.value =
      error instanceof Error ? error.message : 'Не удалось загрузить препараты.'
  } finally {
    medicationsLoading.value = false
  }
}

function readValue(event: Event): string {
  return (event.target as HTMLInputElement).value
}

// Симптомы

function isSymptomSelected(id: string): boolean {
  return props.symptoms.some((item) => item.symptomId === id)
}

function toggleSymptom(id: string) {
  if (props.disabled) return

  if (isSymptomSelected(id)) {
    emit(
      'update:symptoms',
      props.symptoms.filter((item) => item.symptomId !== id),
    )
    return
  }

  emit('update:symptoms', [
    ...props.symptoms,
    {
      symptomId: id,
      description: null,
    },
  ])
}

function symptomDescription(id: string): string {
  return props.symptoms.find((item) => item.symptomId === id)?.description ?? ''
}

function updateSymptomDescription(id: string, event: Event) {
  const description = readValue(event).trim()

  emit(
    'update:symptoms',
    props.symptoms.map((item) =>
      item.symptomId === id ? { ...item, description: description || null } : item,
    ),
  )
}

// Триггеры

function isTriggerSelected(id: string): boolean {
  return props.triggers.some((item) => item.triggerId === id)
}

function toggleTrigger(id: string) {
  if (props.disabled) return

  if (isTriggerSelected(id)) {
    emit(
      'update:triggers',
      props.triggers.filter((item) => item.triggerId !== id),
    )
    return
  }

  emit('update:triggers', [
    ...props.triggers,
    {
      triggerId: id,
      description: null,
    },
  ])
}

function triggerDescription(id: string): string {
  return props.triggers.find((item) => item.triggerId === id)?.description ?? ''
}

function updateTriggerDescription(id: string, event: Event) {
  const description = readValue(event).trim()

  emit(
    'update:triggers',
    props.triggers.map((item) =>
      item.triggerId === id ? { ...item, description: description || null } : item,
    ),
  )
}

// Препараты

function toLocalDateTime(value?: string | null): string {
  if (!value) return ''

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return ''

  const pad = (number: number) => String(number).padStart(2, '0')

  return (
    `${date.getFullYear()}-` +
    `${pad(date.getMonth() + 1)}-` +
    `${pad(date.getDate())}T` +
    `${pad(date.getHours())}:` +
    `${pad(date.getMinutes())}`
  )
}

function addMedication() {
  if (props.disabled || medicationOptions.value.length === 0) {
    return
  }

  const medication = medicationOptions.value[0]

  if (!medication) return

  emit('update:medications', [
    ...props.medications,
    {
      medicationId: medication.id,
      dosage: null,
      takenAt: null,
    },
  ])
}

function removeMedication(index: number) {
  if (props.disabled) return

  emit(
    'update:medications',
    props.medications.filter((_, itemIndex) => itemIndex !== index),
  )
}

function updateMedication(index: number, changes: Partial<AttackMedicationRequest>) {
  emit(
    'update:medications',
    props.medications.map((item, itemIndex) =>
      itemIndex === index ? { ...item, ...changes } : item,
    ),
  )
}

function updateMedicationDate(index: number, event: Event) {
  const value = readValue(event)

  if (!value) {
    updateMedication(index, { takenAt: null })
    return
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return

  updateMedication(index, {
    takenAt: date.toISOString(),
  })
}

async function saveNewMedication() {
  const name = newMedicationName.value.trim()

  if (!name || creatingMedication.value || props.disabled) {
    return
  }

  creatingMedication.value = true
  createMedicationError.value = ''

  try {
    const medication = await createMedication({ name })

    medicationOptions.value = [
      ...medicationOptions.value.filter((item) => item.id !== medication.id),
      medication,
    ].sort((a, b) => a.name.localeCompare(b.name, 'ru'))

    newMedicationName.value = ''

    emit('update:medications', [
      ...props.medications,
      {
        medicationId: medication.id,
        dosage: null,
        takenAt: null,
      },
    ])
  } catch (error) {
    createMedicationError.value =
      error instanceof Error ? error.message : 'Не удалось создать препарат.'
  } finally {
    creatingMedication.value = false
  }
}

onMounted(() => {
  void loadSymptoms()
  void loadTriggers()
  void loadMedications()
})
</script>

<template>
  <div class="related-fields">
    <!-- Симптомы -->
    <section class="related-section">
      <h2>Симптомы</h2>

      <p class="section-description">
        Отметьте симптомы, которые сопровождали приступ. Можно выбрать несколько вариантов.
      </p>

      <p v-if="symptomsLoading" class="state-text">Загрузка симптомов...</p>

      <div v-else-if="symptomsError" class="error-message">
        <p>{{ symptomsError }}</p>

        <button type="button" class="retry-button" @click="loadSymptoms">Повторить</button>
      </div>

      <p v-else-if="symptomOptions.length === 0" class="state-text">
        Справочник симптомов пока пуст.
      </p>

      <div v-else class="options-list">
        <div v-for="symptom in symptomOptions" :key="symptom.id" class="option-item">
          <label class="checkbox-label">
            <input
              type="checkbox"
              :checked="isSymptomSelected(symptom.id)"
              :disabled="disabled"
              @change="toggleSymptom(symptom.id)"
            />

            <span>{{ getSymptomLabel(symptom.name) }}</span>
          </label>

          <div v-if="isSymptomSelected(symptom.id)" class="description-field">
            <label :for="`symptom-${symptom.id}`"> Дополнительное описание </label>

            <input
              :id="`symptom-${symptom.id}`"
              type="text"
              :value="symptomDescription(symptom.id)"
              maxlength="500"
              placeholder="Уточнение к симптому (необязательно)"
              :disabled="disabled"
              @input="updateSymptomDescription(symptom.id, $event)"
            />
          </div>
        </div>
      </div>
    </section>

    <!-- Триггеры -->
    <section class="related-section">
      <h2>Возможные триггеры</h2>

      <p class="section-description">
        Укажите факторы, которые могли повлиять на возникновение приступа.
      </p>

      <p v-if="triggersLoading" class="state-text">Загрузка триггеров...</p>

      <div v-else-if="triggersError" class="error-message">
        <p>{{ triggersError }}</p>

        <button type="button" class="retry-button" @click="loadTriggers">Повторить</button>
      </div>

      <p v-else-if="triggerOptions.length === 0" class="state-text">
        Справочник триггеров пока пуст.
      </p>

      <div v-else class="options-list">
        <div v-for="trigger in triggerOptions" :key="trigger.id" class="option-item">
          <label class="checkbox-label">
            <input
              type="checkbox"
              :checked="isTriggerSelected(trigger.id)"
              :disabled="disabled"
              @change="toggleTrigger(trigger.id)"
            />

            <span>{{ getTriggerLabel(trigger.name) }}</span>
          </label>

          <div v-if="isTriggerSelected(trigger.id)" class="description-field">
            <label :for="`trigger-${trigger.id}`"> Дополнительное описание </label>

            <input
              :id="`trigger-${trigger.id}`"
              type="text"
              :value="triggerDescription(trigger.id)"
              maxlength="500"
              placeholder="Уточнение к триггеру (необязательно)"
              :disabled="disabled"
              @input="updateTriggerDescription(trigger.id, $event)"
            />
          </div>
        </div>
      </div>
    </section>

    <!-- Препараты -->
    <section class="related-section">
      <h2>Принятые препараты</h2>

      <p class="section-description">
        Укажите препараты, которые вы принимали в связи с этим приступом, их дозировку и время
        приёма.
      </p>

      <p v-if="medicationsLoading" class="state-text">Загрузка препаратов...</p>

      <div v-if="medicationsError" class="error-message">
        <p>{{ medicationsError }}</p>

        <button type="button" class="retry-button" @click="loadMedications">Повторить</button>
      </div>

      <div v-for="(medication, index) in props.medications" :key="index" class="medication-entry">
        <div class="medication-heading">
          <h3>Препарат {{ index + 1 }}</h3>

          <button
            type="button"
            class="remove-button"
            :disabled="disabled"
            @click="removeMedication(index)"
          >
            Убрать
          </button>
        </div>

        <div class="form-field">
          <label :for="`medication-${index}`"> Препарат </label>

          <select
            :id="`medication-${index}`"
            :value="medication.medicationId"
            :disabled="disabled"
            @change="
              updateMedication(index, {
                medicationId: readValue($event),
              })
            "
          >
            <option v-for="option in medicationOptions" :key="option.id" :value="option.id">
              {{ option.name }}
            </option>
          </select>
        </div>

        <div class="medication-grid">
          <div class="form-field">
            <label :for="`dosage-${index}`"> Дозировка </label>

            <input
              :id="`dosage-${index}`"
              type="text"
              :value="medication.dosage ?? ''"
              maxlength="100"
              placeholder="Например, 50 мг"
              :disabled="disabled"
              @input="
                updateMedication(index, {
                  dosage: readValue($event) || null,
                })
              "
            />
          </div>

          <div class="form-field">
            <label :for="`taken-${index}`"> Время приёма </label>

            <input
              :id="`taken-${index}`"
              type="datetime-local"
              :value="toLocalDateTime(medication.takenAt)"
              :disabled="disabled"
              @change="updateMedicationDate(index, $event)"
            />
          </div>
        </div>
      </div>

      <button
        type="button"
        class="secondary-button"
        :disabled="disabled || medicationsLoading || medicationOptions.length === 0"
        @click="addMedication"
      >
        + Добавить препарат из списка
      </button>

      <div class="new-medication">
        <h3>Нет нужного препарата?</h3>

        <p>
          Добавьте название препарата в личный список. После создания он будет выбран для этого
          приступа.
        </p>

        <div class="new-medication-controls">
          <input
            v-model="newMedicationName"
            type="text"
            maxlength="200"
            placeholder="Название препарата"
            :disabled="disabled || creatingMedication"
            @keydown.enter.prevent="saveNewMedication"
          />

          <button
            type="button"
            class="primary-button"
            :disabled="disabled || creatingMedication || !newMedicationName.trim()"
            @click="saveNewMedication"
          >
            {{ creatingMedication ? 'Добавление...' : 'Создать препарат' }}
          </button>
        </div>

        <p v-if="createMedicationError" class="error-text" role="alert">
          {{ createMedicationError }}
        </p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.related-fields {
  display: flex;
  flex-direction: column;
  gap: 22px;
}

.related-section {
  padding: 30px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.related-section h2 {
  margin: 0 0 14px;
  color: #252e45;
  font-size: 21px;
}

.section-description {
  margin: 0 0 24px;
  color: #737b8e;
  line-height: 1.6;
}

.state-text {
  color: #8a90a2;
  font-size: 14px;
}

.options-list {
  display: grid;
  gap: 12px;
}

.option-item {
  padding: 14px 16px;
  background: #f8f8fc;
  border: 1px solid #eeeef4;
  border-radius: 10px;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 12px;
  color: #303950;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
}

.checkbox-label input {
  width: 17px;
  height: 17px;
  accent-color: #6554bc;
}

.description-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 15px;
  margin-left: 29px;
}

.description-field label,
.form-field label {
  color: #596377;
  font-size: 13px;
  font-weight: 600;
}

input[type='text'],
input[type='datetime-local'],
select {
  width: 100%;
  min-width: 0;
  padding: 12px 14px;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: white;
  color: #252e45;
  font: inherit;
}

input:focus,
select:focus {
  outline: 2px solid #c5baf9;
  outline-offset: 1px;
}

input:disabled,
select:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.medication-entry {
  margin-bottom: 18px;
  padding: 20px;
  border: 1px solid #ded9f5;
  border-radius: 12px;
  background: #faf9ff;
}

.medication-heading {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.medication-heading h3 {
  margin: 0;
  font-size: 16px;
}

.medication-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-top: 16px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 9px;
}

.secondary-button,
.primary-button,
.retry-button,
.remove-button {
  border-radius: 10px;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.secondary-button {
  padding: 12px 18px;
  color: #6554bc;
  background: white;
  border: 1px solid #ded9f5;
}

.secondary-button:hover:not(:disabled) {
  background: #f0edfc;
}

.primary-button {
  padding: 12px 18px;
  color: white;
  background: #6554bc;
  border: 1px solid #6554bc;
  white-space: nowrap;
}

.primary-button:hover:not(:disabled) {
  background: #5140a8;
}

.remove-button {
  padding: 8px 12px;
  color: #b33b3b;
  background: white;
  border: 1px solid #e8b5b5;
}

.new-medication {
  margin-top: 26px;
  padding-top: 24px;
  border-top: 1px solid #e9eaf0;
}

.new-medication h3 {
  margin: 0 0 10px;
  font-size: 16px;
}

.new-medication p {
  color: #737b8e;
  line-height: 1.6;
  font-size: 14px;
}

.new-medication-controls {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

.new-medication-controls input {
  flex: 1;
}

.error-message {
  padding: 14px 18px;
  color: #ba3232;
  background: #fff1f1;
  border-radius: 10px;
}

.error-message p {
  margin: 0 0 12px;
}

.error-text {
  color: #ba3232 !important;
}

.retry-button {
  padding: 8px 14px;
  color: #b33b3b;
  background: white;
  border: 1px solid #e8b5b5;
}

button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

@media (max-width: 800px) {
  .related-section {
    padding: 22px;
  }

  .medication-grid {
    grid-template-columns: 1fr;
  }

  .new-medication-controls {
    flex-direction: column;
  }

  .description-field {
    margin-left: 0;
  }
}
</style>
