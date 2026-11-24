<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

import { apiRequest } from '@/api/http'

import { getSymptoms, getTriggers, getMedications } from '@/api/references'

import { getSymptomLabel, getTriggerLabel } from '@/utils/referenceLabels'

import type { MigraineAttack, PainLocation } from '@/types/migraineAttack'

import type { Symptom, Trigger, Medication } from '@/types/reference'

// Тип ответа сервера
interface PagedAttacksResponse {
  items: MigraineAttack[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Параметры поиска приступов
interface AttackSearchFilters {
  from?: string
  to?: string
  minIntensity?: number
  maxIntensity?: number
  painLocation?: PainLocation
  symptomId?: string
  triggerId?: string
  medicationId?: string
}

// История приступов
const attacks = ref<MigraineAttack[]>([])
const loading = ref(false)
const errorMessage = ref('')

// Пагинация
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

// Справочники
const symptomOptions = ref<Symptom[]>([])
const triggerOptions = ref<Trigger[]>([])
const medicationOptions = ref<Medication[]>([])

const referencesLoading = ref(false)
const referencesError = ref('')

// Поля формы фильтрации
const filterDraft = reactive({
  from: '',
  to: '',
  minIntensity: '' as number | '',
  maxIntensity: '' as number | '',
  painLocation: '' as PainLocation | '',
  symptomId: '',
  triggerId: '',
  medicationId: '',
})

// Применённые фильтры
const activeFilters = ref<AttackSearchFilters>({})
const filterError = ref('')

const painLocations: Record<PainLocation, string> = {
  LeftSide: 'Левая сторона',
  RightSide: 'Правая сторона',
  BothSides: 'Обе стороны',
  Forehead: 'Лоб',
  Temples: 'Виски',
  Occiput: 'Затылок',
  Other: 'Другое',
}

const hasActiveFilters = computed(() => Object.keys(activeFilters.value).length > 0)

// Сортировка справочников по русским названиям
const sortedSymptoms = computed(() =>
  [...symptomOptions.value].sort((a, b) =>
    getSymptomLabel(a.name).localeCompare(getSymptomLabel(b.name), 'ru'),
  ),
)

const sortedTriggers = computed(() =>
  [...triggerOptions.value].sort((a, b) =>
    getTriggerLabel(a.name).localeCompare(getTriggerLabel(b.name), 'ru'),
  ),
)

const sortedMedications = computed(() =>
  [...medicationOptions.value].sort((a, b) => a.name.localeCompare(b.name, 'ru')),
)

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
  if (intensity <= 3) return 'intensity-low'
  if (intensity <= 6) return 'intensity-medium'
  return 'intensity-high'
}

// Начало и конец выбранного дня
function startOfDay(value: string): string {
  return new Date(`${value}T00:00:00`).toISOString()
}

function endOfDay(value: string): string {
  return new Date(`${value}T23:59:59.999`).toISOString()
}

// Загрузка справочников
async function loadReferences() {
  referencesLoading.value = true
  referencesError.value = ''

  try {
    const [symptoms, triggers, medications] = await Promise.all([
      getSymptoms(),
      getTriggers(),
      getMedications(),
    ])

    symptomOptions.value = symptoms
    triggerOptions.value = triggers
    medicationOptions.value = medications
  } catch (error) {
    referencesError.value =
      error instanceof Error ? error.message : 'Не удалось загрузить справочники.'
  } finally {
    referencesLoading.value = false
  }
}

// Подготовка фильтров
function buildFilters(): AttackSearchFilters {
  const filters: AttackSearchFilters = {}

  if (filterDraft.from) {
    filters.from = startOfDay(filterDraft.from)
  }

  if (filterDraft.to) {
    filters.to = endOfDay(filterDraft.to)
  }

  if (typeof filterDraft.minIntensity === 'number') {
    filters.minIntensity = filterDraft.minIntensity
  }

  if (typeof filterDraft.maxIntensity === 'number') {
    filters.maxIntensity = filterDraft.maxIntensity
  }

  if (filterDraft.painLocation) {
    filters.painLocation = filterDraft.painLocation
  }

  if (filterDraft.symptomId) {
    filters.symptomId = filterDraft.symptomId
  }

  if (filterDraft.triggerId) {
    filters.triggerId = filterDraft.triggerId
  }

  if (filterDraft.medicationId) {
    filters.medicationId = filterDraft.medicationId
  }

  return filters
}

// Все параметры запроса явно передаются серверу
async function fetchAttacks(
  filters: AttackSearchFilters,
  page: number,
  size: number,
): Promise<PagedAttacksResponse> {
  const params = new URLSearchParams()

  params.set('page', String(page))
  params.set('pageSize', String(size))

  if (filters.from) {
    params.set('from', filters.from)
  }

  if (filters.to) {
    params.set('to', filters.to)
  }

  if (filters.minIntensity !== undefined) {
    params.set('minIntensity', String(filters.minIntensity))
  }

  if (filters.maxIntensity !== undefined) {
    params.set('maxIntensity', String(filters.maxIntensity))
  }

  if (filters.painLocation) {
    params.set('painLocation', filters.painLocation)
  }

  if (filters.symptomId) {
    params.set('symptomId', filters.symptomId)
  }

  if (filters.triggerId) {
    params.set('triggerId', filters.triggerId)
  }

  if (filters.medicationId) {
    params.set('medicationId', filters.medicationId)
  }

  return apiRequest<PagedAttacksResponse>(`/migraine-attacks?${params.toString()}`)
}

let latestRequestId = 0

// Загрузка приступов
async function loadAttacks() {
  const requestId = ++latestRequestId

  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetchAttacks(activeFilters.value, currentPage.value, pageSize.value)

    if (requestId !== latestRequestId) return

    attacks.value = response.items
    totalCount.value = response.totalCount
    totalPages.value = response.totalPages
  } catch (error) {
    if (requestId !== latestRequestId) return

    attacks.value = []
    totalCount.value = 0
    totalPages.value = 0

    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось загрузить историю приступов.'
  } finally {
    if (requestId === latestRequestId) {
      loading.value = false
    }
  }
}

// Применение фильтров
function applyFilters() {
  filterError.value = ''

  if (filterDraft.from && filterDraft.to && filterDraft.from > filterDraft.to) {
    filterError.value = 'Начало периода не может быть позже его окончания.'
    return
  }

  if (
    typeof filterDraft.minIntensity === 'number' &&
    typeof filterDraft.maxIntensity === 'number' &&
    filterDraft.minIntensity > filterDraft.maxIntensity
  ) {
    filterError.value = 'Минимальная интенсивность не может быть больше максимальной.'
    return
  }

  activeFilters.value = buildFilters()
  currentPage.value = 1

  void loadAttacks()
}

// Сброс фильтров
function resetFilters() {
  filterDraft.from = ''
  filterDraft.to = ''
  filterDraft.minIntensity = ''
  filterDraft.maxIntensity = ''
  filterDraft.painLocation = ''
  filterDraft.symptomId = ''
  filterDraft.triggerId = ''
  filterDraft.medicationId = ''

  activeFilters.value = {}
  filterError.value = ''
  currentPage.value = 1

  void loadAttacks()
}

// Переход между страницами
function changePage(page: number) {
  if (loading.value || page < 1 || page > totalPages.value) {
    return
  }

  currentPage.value = page
  void loadAttacks()
}

function changePageSize() {
  currentPage.value = 1
  void loadAttacks()
}

onMounted(() => {
  void loadReferences()
  void loadAttacks()
})
</script>

<template>
  <div class="attacks-page">
    <header class="page-header">
      <div>
        <span class="eyebrow">ДНЕВНИК ЗДОРОВЬЯ</span>

        <h1>Дневник приступов</h1>

        <p>
          История приступов мигрени, их продолжительность, интенсивность и сопутствующие симптомы.
        </p>
      </div>

      <RouterLink to="/migraine-attacks/new" class="add-button"> + Добавить приступ </RouterLink>
    </header>

    <!-- Фильтры -->
    <section class="filters-panel">
      <div class="filters-heading">
        <h2>Фильтры</h2>

        <p>Выберите параметры для поиска приступов.</p>
      </div>

      <form @submit.prevent="applyFilters">
        <div class="filters-grid">
          <div class="filter-field">
            <label for="filter-from">Дата с</label>

            <input id="filter-from" v-model="filterDraft.from" type="date" />
          </div>

          <div class="filter-field">
            <label for="filter-to">Дата по</label>

            <input id="filter-to" v-model="filterDraft.to" type="date" />
          </div>

          <div class="filter-field">
            <label for="filter-min-intensity"> Интенсивность от </label>

            <select id="filter-min-intensity" v-model.number="filterDraft.minIntensity">
              <option value="">Любая</option>

              <option v-for="level in 10" :key="level" :value="level">
                {{ level }}
              </option>
            </select>
          </div>

          <div class="filter-field">
            <label for="filter-max-intensity"> Интенсивность до </label>

            <select id="filter-max-intensity" v-model.number="filterDraft.maxIntensity">
              <option value="">Любая</option>

              <option v-for="level in 10" :key="level" :value="level">
                {{ level }}
              </option>
            </select>
          </div>

          <div class="filter-field location-filter">
            <label for="filter-location"> Локализация боли </label>

            <select id="filter-location" v-model="filterDraft.painLocation">
              <option value="">Любая локализация</option>

              <option v-for="(label, value) in painLocations" :key="value" :value="value">
                {{ label }}
              </option>
            </select>
          </div>

          <!-- Новый фильтр: симптомы -->
          <div class="filter-field">
            <label for="filter-symptom"> Симптом </label>

            <select
              id="filter-symptom"
              v-model="filterDraft.symptomId"
              :disabled="referencesLoading || !!referencesError"
            >
              <option value="">Любой симптом</option>

              <option v-for="symptom in sortedSymptoms" :key="symptom.id" :value="symptom.id">
                {{ getSymptomLabel(symptom.name) }}
              </option>
            </select>
          </div>

          <!-- Новый фильтр: триггеры -->
          <div class="filter-field">
            <label for="filter-trigger"> Триггер </label>

            <select
              id="filter-trigger"
              v-model="filterDraft.triggerId"
              :disabled="referencesLoading || !!referencesError"
            >
              <option value="">Любой триггер</option>

              <option v-for="trigger in sortedTriggers" :key="trigger.id" :value="trigger.id">
                {{ getTriggerLabel(trigger.name) }}
              </option>
            </select>
          </div>

          <!-- Новый фильтр: препараты -->
          <div class="filter-field">
            <label for="filter-medication"> Препарат </label>

            <select
              id="filter-medication"
              v-model="filterDraft.medicationId"
              :disabled="referencesLoading || !!referencesError"
            >
              <option value="">Любой препарат</option>

              <option
                v-for="medication in sortedMedications"
                :key="medication.id"
                :value="medication.id"
              >
                {{ medication.name }}
              </option>
            </select>
          </div>
        </div>

        <p v-if="referencesLoading" class="reference-state" role="status">
          Загрузка справочников...
        </p>

        <div v-if="referencesError" class="filter-error" role="alert">
          <p>{{ referencesError }}</p>

          <button
            type="button"
            class="secondary-button"
            :disabled="referencesLoading"
            @click="loadReferences"
          >
            Повторить загрузку справочников
          </button>
        </div>

        <div v-if="filterError" class="filter-error" role="alert">
          {{ filterError }}
        </div>

        <div class="filter-actions">
          <button type="button" class="reset-button" @click="resetFilters">Сбросить</button>

          <button type="submit" class="apply-button">Применить фильтры</button>
        </div>
      </form>
    </section>

    <!-- Сводка -->
    <div class="list-toolbar">
      <div class="summary">
        <span>
          {{ hasActiveFilters ? 'Найдено записей' : 'Всего записей' }}
        </span>

        <strong>{{ totalCount }}</strong>
      </div>

      <div class="page-size-field">
        <label for="page-size"> Записей на странице </label>

        <select
          id="page-size"
          v-model.number="pageSize"
          :disabled="loading"
          @change="changePageSize"
        >
          <option :value="5">5</option>
          <option :value="10">10</option>
          <option :value="20">20</option>
        </select>
      </div>
    </div>

    <!-- Загрузка -->
    <div v-if="loading" class="state-panel" role="status">
      <div class="spinner"></div>

      <h2>Загрузка приступов</h2>

      <p>Получаем данные из дневника...</p>
    </div>

    <!-- Ошибка -->
    <div v-else-if="errorMessage" class="state-panel error-panel" role="alert">
      <h2>Не удалось загрузить записи</h2>

      <p>{{ errorMessage }}</p>

      <button type="button" class="secondary-button" @click="loadAttacks">Повторить попытку</button>
    </div>

    <!-- Пустой список -->
    <div v-else-if="attacks.length === 0" class="state-panel">
      <div class="empty-icon">✦</div>

      <template v-if="hasActiveFilters">
        <h2>Приступы не найдены</h2>

        <p>По выбранным фильтрам записей нет. Попробуйте изменить условия поиска.</p>

        <button type="button" class="secondary-button" @click="resetFilters">
          Сбросить фильтры
        </button>
      </template>

      <template v-else>
        <h2>Записей пока нет</h2>

        <p>Здесь появятся ваши приступы мигрени после добавления первой записи.</p>
      </template>
    </div>

    <!-- Карточки приступов -->
    <template v-else>
      <div class="attacks-list">
        <article v-for="attack in attacks" :key="attack.id" class="attack-card">
          <div class="attack-top">
            <div>
              <span class="card-label"> НАЧАЛО ПРИСТУПА </span>

              <h2>{{ formatDate(attack.startAt) }}</h2>
            </div>

            <span class="intensity-badge" :class="getIntensityClass(attack.intensity)">
              Интенсивность {{ attack.intensity }}/10
            </span>
          </div>

          <div class="attack-details">
            <div class="detail">
              <span>Окончание</span>

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

          <p v-if="attack.painLocationDescription" class="description">
            {{ attack.painLocationDescription }}
          </p>

          <p v-if="attack.notes" class="notes">
            {{ attack.notes }}
          </p>

          <!-- Связанные данные -->
          <div
            v-if="attack.symptoms.length || attack.triggers.length || attack.medications.length"
            class="additional-info"
          >
            <div v-if="attack.symptoms.length">
              <span class="info-label">Симптомы</span>

              <p>
                {{ attack.symptoms.map((item) => getSymptomLabel(item.name)).join(', ') }}
              </p>
            </div>

            <div v-if="attack.triggers.length">
              <span class="info-label">Триггеры</span>

              <p>
                {{ attack.triggers.map((item) => getTriggerLabel(item.name)).join(', ') }}
              </p>
            </div>

            <div v-if="attack.medications.length">
              <span class="info-label">Препараты</span>

              <p>
                {{ attack.medications.map((item) => item.name).join(', ') }}
              </p>
            </div>
          </div>

          <div class="card-footer">
            <RouterLink
              :to="{
                name: 'migraine-attack-details',
                params: { id: attack.id },
              }"
              class="details-link"
            >
              Подробнее о приступе
              <span aria-hidden="true">→</span>
            </RouterLink>
          </div>
        </article>
      </div>

      <!-- Пагинация -->
      <nav v-if="totalPages > 1" class="pagination" aria-label="Страницы истории приступов">
        <button
          type="button"
          class="secondary-button"
          :disabled="currentPage === 1 || loading"
          @click="changePage(currentPage - 1)"
        >
          ← Назад
        </button>

        <span> Страница {{ currentPage }} из {{ totalPages }} </span>

        <button
          type="button"
          class="secondary-button"
          :disabled="currentPage === totalPages || loading"
          @click="changePage(currentPage + 1)"
        >
          Вперёд →
        </button>
      </nav>
    </template>
  </div>
</template>

<style scoped>
.attacks-page {
  width: 100%;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 24px;
  margin-bottom: 28px;
}

.eyebrow {
  color: #6554bc;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

h1 {
  margin: 12px 0;
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.25;
}

.page-header p {
  max-width: 650px;
  color: #737b8e;
  line-height: 1.65;
}

.add-button {
  display: inline-block;
  padding: 13px 20px;
  color: white;
  background: #6554bc;
  border: none;
  border-radius: 10px;
  font: inherit;
  font-weight: 600;
  text-decoration: none;
  white-space: nowrap;
}

.add-button:hover {
  background: #5140a8;
}

/* Фильтры */

.filters-panel {
  margin-bottom: 24px;
  padding: 28px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.filters-heading {
  margin-bottom: 24px;
}

.filters-heading h2 {
  margin: 0 0 8px;
  color: #252e45;
  font-size: 21px;
}

.filters-heading p {
  margin: 0;
  color: #737b8e;
  line-height: 1.6;
}

.filters-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 18px;
}

.filter-field {
  display: flex;
  flex-direction: column;
  gap: 9px;
}

.filter-field label,
.page-size-field label {
  color: #596377;
  font-size: 13px;
  font-weight: 600;
}

.filter-field input,
.filter-field select,
.page-size-field select {
  width: 100%;
  min-width: 0;
  padding: 12px 14px;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: #fff;
  color: #252e45;
  font: inherit;
}

.filter-field input:focus,
.filter-field select:focus,
.page-size-field select:focus {
  outline: 2px solid #c5baf9;
  outline-offset: 1px;
}

.filter-field select:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.location-filter {
  grid-column: span 2;
}

.reference-state {
  margin-top: 18px;
  color: #737b8e;
  font-size: 14px;
}

.filter-error {
  margin-top: 18px;
  padding: 14px 18px;
  border: 1px solid #f3c3c3;
  border-radius: 10px;
  background: #fff1f1;
  color: #ba3232;
  line-height: 1.5;
}

.filter-error p {
  margin: 0 0 12px;
}

.filter-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
}

.apply-button,
.reset-button {
  padding: 12px 18px;
  border-radius: 10px;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.apply-button {
  color: white;
  background: #6554bc;
  border: 1px solid #6554bc;
}

.apply-button:hover {
  background: #5140a8;
}

.reset-button {
  color: #6554bc;
  background: white;
  border: 1px solid #ded9f5;
}

.reset-button:hover {
  background: #f0edfc;
}

/* Сводка */

.list-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 24px;
}

.summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 28px;
  min-width: 240px;
  padding: 20px 24px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 14px;
}

.summary span {
  color: #737b8e;
}

.summary strong {
  color: #44368e;
  font-size: 28px;
}

.page-size-field {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-size-field select {
  width: 85px;
}

/* Карточки */

.attacks-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.attack-card {
  padding: 26px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.attack-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
  margin-bottom: 24px;
}

.card-label,
.info-label {
  color: #8a90a2;
  font-size: 12px;
  font-weight: 600;
}

.attack-card h2 {
  margin: 8px 0 0;
  font-size: 20px;
}

.intensity-badge {
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 12px;
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

.attack-details {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
}

.detail {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail span {
  color: #8a90a2;
  font-size: 13px;
}

.detail strong {
  color: #252e45;
  font-size: 14px;
  font-weight: 600;
}

.description,
.notes {
  margin: 20px 0 0;
  color: #596377;
  line-height: 1.65;
}

.notes {
  padding: 16px;
  background: #f7f7fb;
  border-radius: 10px;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.additional-info {
  display: grid;
  gap: 14px;
  margin-top: 22px;
  padding-top: 20px;
  border-top: 1px solid #e9eaf0;
}

.additional-info p {
  margin: 6px 0 0;
  color: #596377;
  line-height: 1.5;
}

.card-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 24px;
  padding-top: 18px;
  border-top: 1px solid #e9eaf0;
}

.details-link {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: #6554bc;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
}

.details-link:hover {
  text-decoration: underline;
}

.details-link span {
  font-size: 19px;
}

/* Состояния */

.state-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 64px 24px;
  text-align: center;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.state-panel h2 {
  margin: 14px 0 8px;
  font-size: 22px;
}

.state-panel p {
  max-width: 440px;
  color: #737b8e;
  line-height: 1.6;
}

.empty-icon {
  color: #6554bc;
  font-size: 44px;
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

/* Пагинация */

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 20px;
  margin-top: 28px;
}

.pagination span {
  color: #737b8e;
  font-size: 14px;
  text-align: center;
}

.secondary-button {
  padding: 10px 16px;
  border: 1px solid #ded9f5;
  border-radius: 10px;
  background: #fff;
  color: #6554bc;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.secondary-button:hover:not(:disabled) {
  background: #f0edfc;
}

.secondary-button:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

/* Адаптивность */

@media (max-width: 1100px) {
  .filters-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 800px) {
  .page-header,
  .attack-top {
    flex-direction: column;
  }

  .filters-panel {
    padding: 22px;
  }

  .filters-grid,
  .attack-details {
    grid-template-columns: 1fr;
  }

  .location-filter {
    grid-column: auto;
  }

  .filter-actions {
    flex-direction: column-reverse;
  }

  .filter-actions button {
    width: 100%;
  }

  .attack-card {
    padding: 20px;
  }

  .card-footer {
    justify-content: flex-start;
  }

  .pagination {
    gap: 10px;
  }

  .list-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .page-size-field {
    justify-content: space-between;
  }
}
</style>
