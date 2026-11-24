<script setup lang="ts">
import { getSymptomLabel, getTriggerLabel } from '@/utils/referenceLabels'
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { getMigraineAttackById, deleteMigraineAttack } from '@/api/migraineAttacks'

import type { MigraineAttack, PainLocation } from '@/types/migraineAttack'

const route = useRoute()
const router = useRouter()

const attack = ref<MigraineAttack | null>(null)

const loading = ref(true)
const errorMessage = ref('')

const deleting = ref(false)
const deleteError = ref('')

const painLocations: Record<PainLocation, string> = {
  LeftSide: 'Левая сторона головы',
  RightSide: 'Правая сторона головы',
  BothSides: 'Обе стороны головы',
  Forehead: 'Лоб',
  Temples: 'Виски',
  Occiput: 'Затылок',
  Other: 'Другое',
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function formatDuration(minutes: number): string {
  const rounded = Math.round(minutes)

  if (rounded < 60) {
    return `${rounded} мин`
  }

  const hours = Math.floor(rounded / 60)
  const remainingMinutes = rounded % 60

  return remainingMinutes ? `${hours} ч ${remainingMinutes} мин` : `${hours} ч`
}

function getIntensityClass(intensity: number): string {
  if (intensity <= 3) {
    return 'intensity-low'
  }

  if (intensity <= 6) {
    return 'intensity-medium'
  }

  return 'intensity-high'
}

let latestRequestId = 0

async function loadAttack() {
  const requestId = ++latestRequestId
  const id = route.params.id

  loading.value = true
  errorMessage.value = ''
  deleteError.value = ''
  attack.value = null

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
  } catch (error) {
    if (requestId !== latestRequestId) {
      return
    }

    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось загрузить информацию о приступе.'
  } finally {
    if (requestId === latestRequestId) {
      loading.value = false
    }
  }
}

async function confirmDelete() {
  const currentAttack = attack.value

  if (!currentAttack || deleting.value || loading.value) {
    return
  }

  const confirmed = window.confirm(
    'Вы действительно хотите удалить запись о приступе мигрени?\n\nЭто действие нельзя отменить.',
  )

  if (!confirmed) {
    return
  }

  deleting.value = true
  deleteError.value = ''

  try {
    await deleteMigraineAttack(currentAttack.id)
  } catch (error) {
    deleteError.value = error instanceof Error ? error.message : 'Не удалось удалить приступ.'

    deleting.value = false
    return
  }

  deleting.value = false

  await router.push({
    name: 'migraine-attacks',
  })
}

watch(
  () => route.params.id,
  () => void loadAttack(),
  { immediate: true },
)
</script>

<template>
  <div class="details-page">
    <RouterLink to="/migraine-attacks" class="back-link"> ← К дневнику приступов </RouterLink>

    <!-- Загрузка -->
    <div v-if="loading" class="state-panel" role="status">
      <div class="spinner"></div>

      <h2>Загрузка приступа</h2>

      <p>Получаем информацию из дневника...</p>
    </div>

    <!-- Ошибка загрузки -->
    <div v-else-if="errorMessage" class="state-panel error-panel" role="alert">
      <h2>Не удалось открыть запись</h2>

      <p>{{ errorMessage }}</p>

      <button type="button" class="secondary-button" @click="loadAttack">Повторить попытку</button>
    </div>

    <!-- Информация о приступе -->
    <template v-else-if="attack">
      <header class="page-header">
        <div>
          <span class="eyebrow"> ДНЕВНИК ЗДОРОВЬЯ </span>

          <h1>Информация о приступе</h1>

          <p>{{ formatDate(attack.startAt) }}</p>
        </div>

        <span class="intensity" :class="getIntensityClass(attack.intensity)">
          Интенсивность {{ attack.intensity }}/10
        </span>
      </header>

      <!-- Основная информация -->
      <section class="details-card">
        <h2>Основная информация</h2>

        <div class="details-grid">
          <div class="detail">
            <span>Начало приступа</span>

            <strong>
              {{ formatDate(attack.startAt) }}
            </strong>
          </div>

          <div class="detail">
            <span>Окончание приступа</span>

            <strong>
              {{ formatDate(attack.endAt) }}
            </strong>
          </div>

          <div class="detail">
            <span>Продолжительность</span>

            <strong>
              {{ formatDuration(attack.durationMinutes) }}
            </strong>
          </div>

          <div class="detail">
            <span>Локализация боли</span>

            <strong>
              {{ painLocations[attack.painLocation] ?? attack.painLocation }}
            </strong>
          </div>
        </div>
      </section>

      <!-- Описание и заметки -->
      <section v-if="attack.painLocationDescription || attack.notes" class="details-card">
        <h2>Описание приступа</h2>

        <div v-if="attack.painLocationDescription" class="description-block">
          <h3>Характер боли</h3>

          <p>
            {{ attack.painLocationDescription }}
          </p>
        </div>

        <div v-if="attack.notes" class="description-block">
          <h3>Заметки</h3>

          <p>
            {{ attack.notes }}
          </p>
        </div>
      </section>

      <!-- Дополнительные сведения -->
      <section
        v-if="attack.symptoms.length || attack.triggers.length || attack.medications.length"
        class="details-card"
      >
        <h2>Дополнительные сведения</h2>

        <!-- Симптомы -->
        <div v-if="attack.symptoms.length" class="info-group">
          <h3>Симптомы</h3>

          <ul>
            <li v-for="item in attack.symptoms" :key="item.symptomId">
              <strong>{{ getSymptomLabel(item.name) }}</strong>

              <span v-if="item.description"> — {{ item.description }} </span>
            </li>
          </ul>
        </div>

        <!-- Триггеры -->
        <div v-if="attack.triggers.length" class="info-group">
          <h3>Триггеры</h3>

          <ul>
            <li v-for="item in attack.triggers" :key="item.triggerId">
              <strong>{{ getTriggerLabel(item.name) }}</strong>

              <span v-if="item.description"> — {{ item.description }} </span>
            </li>
          </ul>
        </div>

        <!-- Препараты -->
        <div v-if="attack.medications.length" class="info-group">
          <h3>Препараты</h3>

          <ul>
            <li v-for="item in attack.medications" :key="item.id">
              <strong>{{ item.name }}</strong>

              <span v-if="item.dosage"> — {{ item.dosage }} </span>

              <span v-if="item.takenAt"> ({{ formatDate(item.takenAt) }}) </span>
            </li>
          </ul>
        </div>
      </section>

      <!-- Действия с записью -->
      <section class="record-actions" aria-label="Действия с приступом">
        <RouterLink
          :to="{
            name: 'migraine-attack-edit',
            params: { id: attack.id },
          }"
          class="edit-button"
        >
          Редактировать приступ
        </RouterLink>

        <button type="button" class="delete-button" :disabled="deleting" @click="confirmDelete">
          {{ deleting ? 'Удаление...' : 'Удалить приступ' }}
        </button>
      </section>

      <!-- Ошибка удаления -->
      <div v-if="deleteError" class="delete-error" role="alert">
        <strong>Не удалось удалить запись</strong>

        <p>{{ deleteError }}</p>
      </div>

      <!-- Нижняя панель -->
      <div class="details-footer">
        <p class="created-at">
          Запись создана:
          {{ formatDate(attack.createdAt) }}
        </p>

        <RouterLink to="/migraine-attacks" class="secondary-button">
          Вернуться к дневнику
        </RouterLink>
      </div>
    </template>
  </div>
</template>

<style scoped>
.details-page {
  width: 100%;
  max-width: 1100px;
}

/* Навигация */

.back-link {
  display: inline-block;
  margin-bottom: 28px;
  color: #6554bc;
  font-weight: 500;
  text-decoration: none;
}

.back-link:hover {
  text-decoration: underline;
}

/* Заголовок */

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
  margin-bottom: 28px;
}

.eyebrow {
  color: #6554bc;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

h1 {
  margin: 10px 0;
  color: #252e45;
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.25;
}

.page-header p {
  color: #737b8e;
}

/* Интенсивность */

.intensity {
  display: inline-block;
  padding: 10px 15px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  white-space: nowrap;
}

.intensity-low {
  background: #e8f6ee;
  color: #268453;
}

.intensity-medium {
  background: #fff3db;
  color: #a96c09;
}

.intensity-high {
  background: #fde9e9;
  color: #c24141;
}

/* Карточки */

.details-card {
  margin-bottom: 20px;
  padding: 30px;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
  background: white;
}

.details-card h2 {
  margin: 0 0 24px;
  color: #252e45;
  font-size: 21px;
}

/* Основные сведения */

.details-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 26px;
}

.detail {
  display: flex;
  flex-direction: column;
  gap: 9px;
}

.detail span {
  color: #8a90a2;
  font-size: 13px;
}

.detail strong {
  color: #252e45;
  font-size: 16px;
}

/* Описание */

.description-block + .description-block {
  margin-top: 24px;
}

h3 {
  margin: 0 0 10px;
  color: #303950;
  font-size: 15px;
}

.description-block p {
  margin: 0;
  color: #596377;
  line-height: 1.7;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

/* Симптомы, триггеры, препараты */

.info-group + .info-group {
  margin-top: 24px;
}

.info-group ul {
  margin: 0;
  padding-left: 20px;
  color: #596377;
  line-height: 1.8;
}

/* Кнопки управления записью */

.record-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 14px;
  margin-top: 26px;
}

.edit-button,
.delete-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 13px 22px;
  border-radius: 10px;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
  transition:
    background 0.2s ease,
    border-color 0.2s ease;
}

/* Редактирование */

.edit-button {
  color: white;
  background: #6554bc;
  border: 1px solid #6554bc;
}

.edit-button:hover {
  background: #5140a8;
  border-color: #5140a8;
}

/* Удаление */

.delete-button {
  color: #b33b3b;
  background: white;
  border: 1px solid #e8b5b5;
}

.delete-button:hover:not(:disabled) {
  background: #fff1f1;
  border-color: #d98989;
}

.delete-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Ошибка удаления */

.delete-error {
  margin-top: 18px;
  padding: 16px 20px;
  border: 1px solid #f3c3c3;
  border-radius: 10px;
  color: #ba3232;
  background: #fff1f1;
}

.delete-error strong {
  display: block;
  margin-bottom: 8px;
  font-size: 14px;
}

.delete-error p {
  margin: 0;
  line-height: 1.5;
}

/* Нижняя панель */

.details-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  margin-top: 24px;
}

.created-at {
  color: #737b8e;
  font-size: 13px;
}

/* Второстепенная кнопка */

.secondary-button {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  padding: 11px 18px;
  border: 1px solid #ded9f5;
  border-radius: 10px;
  background: white;
  color: #6554bc;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}

.secondary-button:hover {
  background: #f0edfc;
}

/* Состояния загрузки и ошибки */

.state-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 64px 24px;
  background: white;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
  text-align: center;
}

.state-panel h2 {
  margin: 16px 0 8px;
  font-size: 22px;
}

.state-panel p {
  max-width: 440px;
  color: #737b8e;
  line-height: 1.6;
}

.error-panel h2 {
  color: #b33b3b;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #eeebfc;
  border-top-color: #6554bc;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Адаптивность */

@media (max-width: 800px) {
  .page-header {
    flex-direction: column;
  }

  .details-grid {
    grid-template-columns: 1fr;
  }

  .details-card {
    padding: 22px;
  }

  .record-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .edit-button,
  .delete-button {
    width: 100%;
  }

  .details-footer {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
