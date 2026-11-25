<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { createVitalMeasurement } from '@/api/vitalMeasurements'

import type { SaveVitalMeasurementRequest } from '@/types/vitalMeasurement'

const router = useRouter()

// Текущая дата и время в локальном формате
function toLocalDateTime(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')

  return (
    `${date.getFullYear()}-` +
    `${pad(date.getMonth() + 1)}-` +
    `${pad(date.getDate())}T` +
    `${pad(date.getHours())}:` +
    `${pad(date.getMinutes())}`
  )
}

// Поля формы
const systolicPressure = ref<number | ''>('')
const diastolicPressure = ref<number | ''>('')
const pulse = ref<number | ''>('')

const measuredAt = ref(toLocalDateTime(new Date()))
const notes = ref('')

// Состояние сохранения
const saving = ref(false)
const errorMessage = ref('')

function validateForm(): boolean {
  errorMessage.value = ''

  const systolic = Number(systolicPressure.value)
  const diastolic = Number(diastolicPressure.value)
  const heartRate = Number(pulse.value)

  if (!systolicPressure.value || !diastolicPressure.value || !pulse.value || !measuredAt.value) {
    errorMessage.value = 'Заполните все обязательные поля.'
    return false
  }

  if (!Number.isInteger(systolic) || systolic < 1 || systolic > 300) {
    errorMessage.value = 'Систолическое давление должно быть целым числом от 1 до 300.'
    return false
  }

  if (!Number.isInteger(diastolic) || diastolic < 1 || diastolic > 200) {
    errorMessage.value = 'Диастолическое давление должно быть целым числом от 1 до 200.'
    return false
  }

  if (systolic <= diastolic) {
    errorMessage.value = 'Систолическое давление должно быть выше диастолического.'
    return false
  }

  if (!Number.isInteger(heartRate) || heartRate < 1 || heartRate > 300) {
    errorMessage.value = 'Пульс должен быть целым числом от 1 до 300.'
    return false
  }

  const date = new Date(measuredAt.value)

  if (Number.isNaN(date.getTime())) {
    errorMessage.value = 'Укажите корректную дату и время измерения.'
    return false
  }

  if (notes.value.length > 1000) {
    errorMessage.value = 'Заметки не должны превышать 1000 символов.'
    return false
  }

  return true
}

async function submitForm() {
  if (saving.value || !validateForm()) {
    return
  }

  const data: SaveVitalMeasurementRequest = {
    systolicPressure: Number(systolicPressure.value),
    diastolicPressure: Number(diastolicPressure.value),
    pulse: Number(pulse.value),
    measuredAt: new Date(measuredAt.value).toISOString(),
    notes: notes.value.trim() || null,
  }

  saving.value = true
  errorMessage.value = ''

  try {
    await createVitalMeasurement(data)

    await router.push({
      name: 'vital-measurements',
    })
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Не удалось сохранить измерение.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="create-page">
    <header class="page-header">
      <RouterLink to="/vital-measurements" class="back-link"> ← К истории измерений </RouterLink>

      <span class="eyebrow"> ДНЕВНИК ЗДОРОВЬЯ </span>

      <h1>Новое измерение</h1>

      <p>
        Запишите артериальное давление и пульс. После сохранения данные появятся в истории
        измерений.
      </p>
    </header>

    <form class="measurement-form" @submit.prevent="submitForm">
      <!-- Артериальное давление -->
      <section class="form-section">
        <h2>Артериальное давление</h2>

        <p class="section-description">
          Укажите верхнее и нижнее давление в миллиметрах ртутного столба.
        </p>

        <div class="form-grid">
          <div class="form-field">
            <label for="systolic"> Систолическое (верхнее) * </label>

            <div class="input-wrapper">
              <input
                id="systolic"
                v-model.number="systolicPressure"
                type="number"
                min="1"
                max="300"
                step="1"
                placeholder="Например, 120"
                :disabled="saving"
                required
              />

              <span class="unit"> мм рт. ст. </span>
            </div>
          </div>

          <div class="form-field">
            <label for="diastolic"> Диастолическое (нижнее) * </label>

            <div class="input-wrapper">
              <input
                id="diastolic"
                v-model.number="diastolicPressure"
                type="number"
                min="1"
                max="200"
                step="1"
                placeholder="Например, 80"
                :disabled="saving"
                required
              />

              <span class="unit"> мм рт. ст. </span>
            </div>
          </div>
        </div>
      </section>

      <!-- Пульс -->
      <section class="form-section">
        <h2>Частота сердечных сокращений</h2>

        <p class="section-description">Укажите количество ударов сердца в минуту.</p>

        <div class="form-field">
          <label for="pulse"> Пульс * </label>

          <div class="input-wrapper">
            <input
              id="pulse"
              v-model.number="pulse"
              type="number"
              min="1"
              max="300"
              step="1"
              placeholder="Например, 72"
              :disabled="saving"
              required
            />

            <span class="unit"> уд/мин </span>
          </div>
        </div>
      </section>

      <!-- Дата и время -->
      <section class="form-section">
        <h2>Дата и время измерения</h2>

        <p class="section-description">Укажите, когда были измерены показатели.</p>

        <div class="form-field">
          <label for="measuredAt"> Дата и время * </label>

          <input
            id="measuredAt"
            v-model="measuredAt"
            type="datetime-local"
            :disabled="saving"
            required
          />
        </div>
      </section>

      <!-- Заметки -->
      <section class="form-section">
        <h2>Дополнительные заметки</h2>

        <div class="form-field">
          <label for="notes"> Комментарий </label>

          <textarea
            id="notes"
            v-model="notes"
            rows="5"
            maxlength="1000"
            :disabled="saving"
            placeholder="Например: измерение после отдыха, самочувствие, обстоятельства измерения..."
          ></textarea>

          <span class="character-count"> {{ notes.length }} / 1000 </span>
        </div>
      </section>

      <!-- Ошибка -->
      <div v-if="errorMessage" class="error-message" role="alert">
        {{ errorMessage }}
      </div>

      <!-- Действия -->
      <div class="form-actions">
        <RouterLink to="/vital-measurements" class="cancel-button"> Отмена </RouterLink>

        <button type="submit" class="save-button" :disabled="saving">
          {{ saving ? 'Сохранение...' : 'Сохранить измерение' }}
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
  margin-bottom: 24px;
  color: #6554bc;
  font-weight: 500;
  text-decoration: none;
}

.back-link:hover {
  text-decoration: underline;
}

.eyebrow {
  display: block;
  margin-bottom: 12px;
  color: #6554bc;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

h1 {
  margin: 0 0 12px;
  color: #252e45;
  font-size: clamp(26px, 3vw, 36px);
}

.page-header p,
.section-description {
  color: #737b8e;
  line-height: 1.6;
}

.measurement-form {
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
  margin: 0 0 16px;
  color: #252e45;
  font-size: 21px;
}

.section-description {
  margin: 0 0 24px;
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
}

.form-field label {
  color: #303950;
  font-size: 14px;
  font-weight: 600;
}

.input-wrapper {
  display: flex;
  align-items: center;
  min-width: 0;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: white;
  overflow: hidden;
}

.input-wrapper input {
  flex: 1;
  width: 100%;
  min-width: 0;
  padding: 13px 15px;
  border: none;
  outline: none;
  background: transparent;
  color: #252e45;
  font: inherit;
}

.input-wrapper:focus-within {
  outline: 2px solid #c5baf9;
  outline-offset: 1px;
}

.unit {
  padding: 0 15px;
  color: #8a90a2;
  font-size: 12px;
  white-space: nowrap;
}

input[type='datetime-local'],
textarea {
  width: 100%;
  min-width: 0;
  padding: 13px 15px;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: white;
  color: #252e45;
  font: inherit;
}

input[type='datetime-local']:focus,
textarea:focus {
  outline: 2px solid #c5baf9;
  outline-offset: 1px;
}

textarea {
  resize: vertical;
  line-height: 1.6;
}

.character-count {
  align-self: flex-end;
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
  align-items: center;
  justify-content: center;
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
