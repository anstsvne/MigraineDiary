<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { getMigraineAttackById, updateMigraineAttack } from '@/api/migraineAttacks'

import AttackRelatedFields from '@/components/attacks/AttackRelatedFields.vue'

import type {
  MigraineAttack,
  PainLocation,
  SaveMigraineAttackRequest,
  AttackSymptomRequest,
  AttackTriggerRequest,
  AttackMedicationRequest,
} from '@/types/migraineAttack'

const route = useRoute()
const router = useRouter()

const attack = ref<MigraineAttack | null>(null)

// Состояние страницы
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const saveError = ref('')

// Основные сведения
const startAt = ref('')
const endAt = ref('')
const intensity = ref(5)
const painLocation = ref<PainLocation | ''>('')
const painLocationDescription = ref('')
const notes = ref('')

// Связанные сведения
const symptoms = ref<AttackSymptomRequest[]>([])
const triggers = ref<AttackTriggerRequest[]>([])
const medications = ref<AttackMedicationRequest[]>([])

const painLocations: {
  value: PainLocation
  label: string
}[] = [
  {
    value: 'LeftSide',
    label: 'Левая сторона головы',
  },
  {
    value: 'RightSide',
    label: 'Правая сторона головы',
  },
  {
    value: 'BothSides',
    label: 'Обе стороны головы',
  },
  {
    value: 'Forehead',
    label: 'Лоб',
  },
  {
    value: 'Temples',
    label: 'Виски',
  },
  {
    value: 'Occiput',
    label: 'Затылок',
  },
  {
    value: 'Other',
    label: 'Другое',
  },
]

// ISO 8601 -> значение datetime-local
function toLocalDateTime(value: string): string {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const pad = (number: number) => String(number).padStart(2, '0')

  return (
    `${date.getFullYear()}-` +
    `${pad(date.getMonth() + 1)}-` +
    `${pad(date.getDate())}T` +
    `${pad(date.getHours())}:` +
    `${pad(date.getMinutes())}`
  )
}

let latestRequestId = 0

// Загрузка приступа и заполнение формы
async function loadAttack() {
  const requestId = ++latestRequestId
  const id = route.params.id

  loading.value = true
  errorMessage.value = ''
  saveError.value = ''
  attack.value = null

  symptoms.value = []
  triggers.value = []
  medications.value = []

  if (typeof id !== 'string' || !id) {
    errorMessage.value = 'Некорректный идентификатор приступа.'
    loading.value = false
    return
  }

  try {
    const result = await getMigraineAttackById(id)

    if (requestId !== latestRequestId) {
      return
    }

    attack.value = result

    // Основные поля
    startAt.value = toLocalDateTime(result.startAt)
    endAt.value = toLocalDateTime(result.endAt)

    intensity.value = result.intensity
    painLocation.value = result.painLocation

    painLocationDescription.value = result.painLocationDescription ?? ''

    notes.value = result.notes ?? ''

    // Ранее выбранные симптомы
    symptoms.value = result.symptoms.map((item) => ({
      symptomId: item.symptomId,
      description: item.description,
    }))

    // Ранее выбранные триггеры
    triggers.value = result.triggers.map((item) => ({
      triggerId: item.triggerId,
      description: item.description,
    }))

    // Ранее принятые препараты
    medications.value = result.medications.map((item) => ({
      medicationId: item.medicationId,
      dosage: item.dosage,
      takenAt: item.takenAt,
    }))
  } catch (error) {
    if (requestId !== latestRequestId) {
      return
    }

    errorMessage.value = error instanceof Error ? error.message : 'Не удалось загрузить приступ.'
  } finally {
    if (requestId === latestRequestId) {
      loading.value = false
    }
  }
}

// Сохранение отредактированного приступа
async function submitForm() {
  const currentAttack = attack.value
  const selectedLocation = painLocation.value

  if (!currentAttack || saving.value) {
    return
  }

  saveError.value = ''

  // Обязательные поля
  if (!startAt.value || !endAt.value || !selectedLocation) {
    saveError.value = 'Заполните обязательные поля.'
    return
  }

  const startDate = new Date(startAt.value)
  const endDate = new Date(endAt.value)

  // Проверка дат
  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
    saveError.value = 'Укажите корректные дату и время.'
    return
  }

  if (endDate <= startDate) {
    saveError.value = 'Время окончания должно быть позже времени начала.'
    return
  }

  // Проверка интенсивности
  if (!Number.isInteger(intensity.value) || intensity.value < 1 || intensity.value > 10) {
    saveError.value = 'Интенсивность должна быть от 1 до 10.'
    return
  }

  // Полный запрос обновления
  const data: SaveMigraineAttackRequest = {
    startAt: startDate.toISOString(),
    endAt: endDate.toISOString(),

    intensity: intensity.value,
    painLocation: selectedLocation,

    painLocationDescription: painLocationDescription.value.trim() || null,

    notes: notes.value.trim() || null,

    // Актуальные данные из общего компонента
    symptoms: symptoms.value.map((item) => ({
      symptomId: item.symptomId,
      description: item.description?.trim() || null,
    })),

    triggers: triggers.value.map((item) => ({
      triggerId: item.triggerId,
      description: item.description?.trim() || null,
    })),

    medications: medications.value.map((item) => ({
      medicationId: item.medicationId,
      dosage: item.dosage?.trim() || null,
      takenAt: item.takenAt || null,
    })),
  }

  saving.value = true

  try {
    await updateMigraineAttack(currentAttack.id, data)

    await router.push({
      name: 'migraine-attack-details',
      params: {
        id: currentAttack.id,
      },
    })
  } catch (error) {
    saveError.value = error instanceof Error ? error.message : 'Не удалось сохранить изменения.'
  } finally {
    saving.value = false
  }
}

// Перезагрузка при изменении идентификатора
watch(
  () => route.params.id,
  () => void loadAttack(),
  { immediate: true },
)
</script>

<template>
  <div class="edit-page">
    <RouterLink
      :to="
        attack
          ? {
              name: 'migraine-attack-details',
              params: { id: attack.id },
            }
          : '/migraine-attacks'
      "
      class="back-link"
    >
      ← Назад
    </RouterLink>

    <header class="page-header">
      <span class="eyebrow">ДНЕВНИК ЗДОРОВЬЯ</span>

      <h1>Редактирование приступа</h1>

      <p>
        Измените сведения о сохранённом приступе мигрени, сопутствующих симптомах, триггерах и
        препаратах.
      </p>
    </header>

    <!-- Загрузка -->
    <div v-if="loading" class="state-panel" role="status">Загрузка данных приступа...</div>

    <!-- Ошибка загрузки -->
    <div v-else-if="errorMessage" class="state-panel" role="alert">
      <h2>Не удалось загрузить запись</h2>

      <p>{{ errorMessage }}</p>

      <button type="button" class="secondary-button" @click="loadAttack">Повторить</button>
    </div>

    <!-- Форма -->
    <form v-else-if="attack" class="attack-form" @submit.prevent="submitForm">
      <!-- Время приступа -->
      <section class="form-section">
        <h2>Время приступа</h2>

        <div class="form-grid">
          <div class="form-field">
            <label for="edit-start"> Начало приступа * </label>

            <input
              id="edit-start"
              v-model="startAt"
              type="datetime-local"
              :disabled="saving"
              required
            />
          </div>

          <div class="form-field">
            <label for="edit-end"> Окончание приступа * </label>

            <input
              id="edit-end"
              v-model="endAt"
              type="datetime-local"
              :min="startAt || undefined"
              :disabled="saving"
              required
            />
          </div>
        </div>
      </section>

      <!-- Характеристики боли -->
      <section class="form-section">
        <h2>Характеристики боли</h2>

        <div class="form-field">
          <label for="edit-intensity">
            Интенсивность боли *
            <strong>{{ intensity }}/10</strong>
          </label>

          <input
            id="edit-intensity"
            v-model.number="intensity"
            type="range"
            min="1"
            max="10"
            step="1"
            :disabled="saving"
          />

          <div class="range-labels">
            <span>Слабая</span>
            <span>Очень сильная</span>
          </div>
        </div>

        <div class="form-field">
          <label for="edit-location"> Локализация боли * </label>

          <select id="edit-location" v-model="painLocation" :disabled="saving" required>
            <option value="" disabled>Выберите локализацию</option>

            <option v-for="location in painLocations" :key="location.value" :value="location.value">
              {{ location.label }}
            </option>
          </select>
        </div>

        <div class="form-field">
          <label for="edit-description"> Дополнительное описание боли </label>

          <textarea
            id="edit-description"
            v-model="painLocationDescription"
            rows="3"
            maxlength="500"
            :disabled="saving"
            placeholder="Опишите характер и локализацию боли"
          ></textarea>
        </div>
      </section>

      <!-- Симптомы, триггеры и препараты -->
      <AttackRelatedFields
        v-model:symptoms="symptoms"
        v-model:triggers="triggers"
        v-model:medications="medications"
        :disabled="saving"
      />

      <!-- Заметки -->
      <section class="form-section">
        <h2>Дополнительные заметки</h2>

        <div class="form-field">
          <label for="edit-notes"> Комментарий к приступу </label>

          <textarea
            id="edit-notes"
            v-model="notes"
            rows="5"
            maxlength="4000"
            :disabled="saving"
            placeholder="Дополнительные наблюдения о приступе"
          ></textarea>
        </div>
      </section>

      <!-- Ошибка сохранения -->
      <div v-if="saveError" class="error-message" role="alert">
        {{ saveError }}
      </div>

      <!-- Управление формой -->
      <div class="form-actions">
        <RouterLink
          :to="{
            name: 'migraine-attack-details',
            params: { id: attack.id },
          }"
          class="secondary-button"
        >
          Отмена
        </RouterLink>

        <button type="submit" class="primary-button" :disabled="saving">
          {{ saving ? 'Сохранение...' : 'Сохранить изменения' }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.edit-page {
  width: 100%;
  max-width: 950px;
}

.back-link {
  display: inline-block;
  margin-bottom: 22px;
  color: #6554bc;
  text-decoration: none;
}

.back-link:hover {
  text-decoration: underline;
}

.page-header {
  margin-bottom: 30px;
}

.eyebrow {
  color: #6554bc;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

h1 {
  margin: 12px 0;
  color: #252e45;
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.25;
}

.page-header p {
  color: #737b8e;
  line-height: 1.6;
}

.attack-form {
  display: flex;
  flex-direction: column;
  gap: 22px;
}

.form-section {
  padding: 30px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.form-section h2 {
  margin: 0 0 24px;
  color: #252e45;
  font-size: 21px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 22px;
}

.form-field:last-child {
  margin-bottom: 0;
}

.form-field label {
  display: flex;
  justify-content: space-between;
  color: #303950;
  font-size: 14px;
  font-weight: 600;
}

.form-field label strong {
  color: #6554bc;
}

input[type='datetime-local'],
select,
textarea {
  width: 100%;
  min-width: 0;
  padding: 13px 15px;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: #fff;
  color: #252e45;
  font: inherit;
}

input:focus,
select:focus,
textarea:focus {
  outline: 2px solid #c5baf9;
  outline-offset: 1px;
}

textarea {
  resize: vertical;
  line-height: 1.6;
}

input[type='range'] {
  width: 100%;
  accent-color: #6554bc;
  cursor: pointer;
}

.range-labels {
  display: flex;
  justify-content: space-between;
  color: #8a90a2;
  font-size: 12px;
}

.primary-button,
.secondary-button {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  padding: 13px 22px;
  border-radius: 10px;
  font: inherit;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}

.primary-button {
  color: #fff;
  background: #6554bc;
  border: 1px solid #6554bc;
}

.primary-button:hover:not(:disabled) {
  background: #5140a8;
}

.primary-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.secondary-button {
  color: #6554bc;
  background: #fff;
  border: 1px solid #ded9f5;
}

.secondary-button:hover {
  background: #f0edfc;
}

.error-message {
  padding: 16px 20px;
  border: 1px solid #f3c3c3;
  border-radius: 10px;
  background: #fff1f1;
  color: #ba3232;
  line-height: 1.5;
}

.state-panel {
  padding: 45px;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
  background: #fff;
}

.state-panel p {
  color: #737b8e;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 16px;
  margin-bottom: 30px;
}

@media (max-width: 800px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-section {
    padding: 22px;
  }

  .form-actions {
    flex-direction: column-reverse;
    align-items: stretch;
  }

  .primary-button,
  .secondary-button {
    width: 100%;
  }
}
</style>
