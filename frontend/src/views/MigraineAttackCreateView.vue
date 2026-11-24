<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { createMigraineAttack } from '@/api/migraineAttacks'
import AttackRelatedFields from '@/components/attacks/AttackRelatedFields.vue'

import type {
  PainLocation,
  SaveMigraineAttackRequest,
  AttackSymptomRequest,
  AttackTriggerRequest,
  AttackMedicationRequest,
} from '@/types/migraineAttack'

const router = useRouter()

// Основные сведения о приступе
const startAt = ref('')
const endAt = ref('')
const intensity = ref(5)
const painLocation = ref<PainLocation | ''>('')
const painLocationDescription = ref('')
const notes = ref('')

// Связанные данные
const symptoms = ref<AttackSymptomRequest[]>([])
const triggers = ref<AttackTriggerRequest[]>([])
const medications = ref<AttackMedicationRequest[]>([])

// Состояние формы
const saving = ref(false)
const errorMessage = ref('')

const painLocations: { value: PainLocation; label: string }[] = [
  { value: 'LeftSide', label: 'Левая сторона головы' },
  { value: 'RightSide', label: 'Правая сторона головы' },
  { value: 'BothSides', label: 'Обе стороны головы' },
  { value: 'Forehead', label: 'Лоб' },
  { value: 'Temples', label: 'Виски' },
  { value: 'Occiput', label: 'Затылок' },
  { value: 'Other', label: 'Другое' },
]

async function submitForm() {
  if (saving.value) return

  errorMessage.value = ''

  // Проверка обязательных полей
  if (!startAt.value || !endAt.value || !painLocation.value) {
    errorMessage.value = 'Заполните обязательные поля.'
    return
  }

  const startDate = new Date(startAt.value)
  const endDate = new Date(endAt.value)

  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
    errorMessage.value = 'Укажите корректные дату и время.'
    return
  }

  if (endDate < startDate) {
    errorMessage.value = 'Время окончания не может быть раньше времени начала.'
    return
  }

  if (!Number.isInteger(intensity.value) || intensity.value < 1 || intensity.value > 10) {
    errorMessage.value = 'Интенсивность должна быть от 1 до 10.'
    return
  }

  // Подготовка запроса для ASP.NET Core API
  const data: SaveMigraineAttackRequest = {
    startAt: startDate.toISOString(),
    endAt: endDate.toISOString(),
    intensity: intensity.value,
    painLocation: painLocation.value,

    painLocationDescription: painLocationDescription.value.trim() || null,

    notes: notes.value.trim() || null,

    // Данные из общего компонента
    symptoms: symptoms.value,
    triggers: triggers.value,
    medications: medications.value,
  }

  saving.value = true

  try {
    await createMigraineAttack(data)

    await router.push({
      name: 'migraine-attacks',
    })
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Не удалось сохранить приступ.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="create-page">
    <header class="page-header">
      <RouterLink to="/migraine-attacks" class="back-link"> ← К дневнику приступов </RouterLink>

      <h1>Новый приступ мигрени</h1>

      <p>Укажите сведения о приступе. После сохранения запись появится в вашем дневнике.</p>
    </header>

    <form class="attack-form" @submit.prevent="submitForm">
      <!-- Время приступа -->
      <section class="form-section">
        <h2>Время приступа</h2>

        <p class="section-description">Укажите, когда приступ начался и закончился.</p>

        <div class="form-grid">
          <div class="form-field">
            <label for="startAt"> Начало приступа * </label>

            <input
              id="startAt"
              v-model="startAt"
              type="datetime-local"
              :disabled="saving"
              required
            />
          </div>

          <div class="form-field">
            <label for="endAt"> Окончание приступа * </label>

            <input
              id="endAt"
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
          <label for="intensity">
            Интенсивность боли *
            <strong>{{ intensity }} / 10</strong>
          </label>

          <input
            id="intensity"
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
          <label for="painLocation"> Локализация боли * </label>

          <select id="painLocation" v-model="painLocation" :disabled="saving" required>
            <option value="" disabled>Выберите локализацию</option>

            <option v-for="location in painLocations" :key="location.value" :value="location.value">
              {{ location.label }}
            </option>
          </select>
        </div>

        <div class="form-field">
          <label for="locationDescription"> Дополнительное описание боли </label>

          <textarea
            id="locationDescription"
            v-model="painLocationDescription"
            rows="3"
            maxlength="500"
            :disabled="saving"
            placeholder="Например: пульсирующая боль в левом виске"
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

      <!-- Дополнительные заметки -->
      <section class="form-section">
        <h2>Дополнительные заметки</h2>

        <div class="form-field">
          <label for="notes"> Комментарий к приступу </label>

          <textarea
            id="notes"
            v-model="notes"
            rows="5"
            maxlength="4000"
            :disabled="saving"
            placeholder="Опишите обстоятельства приступа, самочувствие и другие наблюдения..."
          ></textarea>
        </div>
      </section>

      <!-- Ошибка сохранения -->
      <div v-if="errorMessage" class="error-message" role="alert">
        {{ errorMessage }}
      </div>

      <!-- Действия -->
      <div class="form-actions">
        <RouterLink to="/migraine-attacks" class="cancel-button"> Отмена </RouterLink>

        <button type="submit" class="save-button" :disabled="saving">
          {{ saving ? 'Сохранение...' : 'Сохранить приступ' }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.create-page {
  width: 100%;
  max-width: 950px;
}

.page-header {
  margin-bottom: 30px;
}

.back-link {
  display: inline-block;
  margin-bottom: 20px;
  color: #6554bc;
  text-decoration: none;
  font-weight: 500;
}

.back-link:hover {
  text-decoration: underline;
}

h1 {
  margin: 0 0 12px;
  color: #252e45;
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.25;
}

.page-header p,
.section-description {
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
  background: white;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.form-section h2 {
  margin: 0 0 18px;
  color: #252e45;
  font-size: 21px;
}

.section-description {
  margin: -5px 0 24px;
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

.error-message {
  padding: 16px 20px;
  border: 1px solid #f3c3c3;
  border-radius: 10px;
  background: #fff1f1;
  color: #ba3232;
  line-height: 1.5;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 16px;
  margin-bottom: 30px;
}

.cancel-button,
.save-button {
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

.cancel-button {
  color: #6554bc;
  background: white;
  border: 1px solid #ded9f5;
}

.cancel-button:hover {
  background: #f0edfc;
}

.save-button {
  color: white;
  background: #6554bc;
  border: 1px solid #6554bc;
}

.save-button:hover:not(:disabled) {
  background: #5140a8;
}

.save-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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

  .cancel-button,
  .save-button {
    width: 100%;
  }
}
</style>
