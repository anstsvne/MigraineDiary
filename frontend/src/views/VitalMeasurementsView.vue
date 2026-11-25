<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { deleteVitalMeasurement, getVitalMeasurements } from '@/api/vitalMeasurements'

import type { VitalMeasurement } from '@/types/vitalMeasurement'

// История измерений
const measurements = ref<VitalMeasurement[]>([])

// Состояние загрузки
const loading = ref(false)
const errorMessage = ref('')
const actionError = ref('')
const deletingId = ref<string | null>(null)

// Пагинация
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

// Защита от устаревших ответов API
let latestRequestId = 0

// Форматирование даты и времени
function formatDate(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

// Загрузка истории измерений
async function loadMeasurements() {
  const requestId = ++latestRequestId

  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getVitalMeasurements({
      page: currentPage.value,
      pageSize: pageSize.value,
    })

    if (requestId !== latestRequestId) {
      return
    }

    measurements.value = response.items
    totalCount.value = response.totalCount
    totalPages.value = response.totalPages
  } catch (error) {
    if (requestId !== latestRequestId) {
      return
    }

    measurements.value = []
    totalCount.value = 0
    totalPages.value = 0

    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось загрузить историю измерений.'
  } finally {
    if (requestId === latestRequestId) {
      loading.value = false
    }
  }
}

// Переключение страниц
function changePage(newPage: number) {
  if (loading.value || newPage < 1 || newPage > totalPages.value || newPage === currentPage.value) {
    return
  }

  currentPage.value = newPage
  void loadMeasurements()
}

// Изменение количества записей
function changePageSize() {
  currentPage.value = 1
  void loadMeasurements()
}

// Удаление с подтверждением и обновлением списка
async function removeMeasurement(id: string) {
  if (deletingId.value !== null || loading.value) return

  const confirmed = window.confirm(
    'Удалить это измерение давления и пульса? Восстановить его будет невозможно.',
  )
  if (!confirmed) return

  deletingId.value = id
  actionError.value = ''

  try {
    await deleteVitalMeasurement(id)

    // После удаления последней карточки на странице
    // возвращаемся на предыдущую страницу.
    if (measurements.value.length === 1 && currentPage.value > 1) {
      currentPage.value -= 1
    }

    await loadMeasurements()
  } catch (error) {
    actionError.value = error instanceof Error ? error.message : 'Не удалось удалить измерение.'
  } finally {
    deletingId.value = null
  }
}

onMounted(() => {
  void loadMeasurements()
})
</script>

<template>
  <div class="measurements-page">
    <!-- Заголовок -->
    <header class="page-header">
      <div>
        <span class="eyebrow">ДНЕВНИК ЗДОРОВЬЯ</span>

        <h1>Давление и пульс</h1>

        <p>Ведите историю измерений артериального давления и частоты сердечных сокращений.</p>
      </div>

      <RouterLink to="/vital-measurements/new" class="add-button">
        + Добавить измерение
      </RouterLink>
    </header>

    <!-- Общая информация -->
    <div class="summary-row">
      <div class="summary-card">
        <span>Всего измерений</span>

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

    <!-- Ошибка выполнения действия -->
    <div v-if="actionError" class="action-error" role="alert">
      {{ actionError }}
    </div>

    <!-- Загрузка -->
    <section v-if="loading" class="state-panel" role="status">
      <div class="spinner"></div>

      <h2>Загрузка измерений</h2>

      <p>Получаем данные из вашего дневника здоровья...</p>
    </section>

    <!-- Ошибка -->
    <section v-else-if="errorMessage" class="state-panel" role="alert">
      <h2>Не удалось загрузить измерения</h2>

      <p>{{ errorMessage }}</p>

      <button type="button" class="secondary-button" @click="loadMeasurements">
        Повторить попытку
      </button>
    </section>

    <!-- Нет измерений -->
    <section v-else-if="measurements.length === 0" class="state-panel">
      <span class="empty-icon" aria-hidden="true">♡</span>

      <h2>Измерений пока нет</h2>

      <p>
        Здесь появятся ваши показатели артериального давления и пульса после добавления первой
        записи.
      </p>
    </section>

    <!-- История измерений -->
    <template v-else>
      <div class="measurements-list">
        <article v-for="measurement in measurements" :key="measurement.id" class="measurement-card">
          <div class="card-heading">
            <div>
              <span class="card-label"> ДАТА ИЗМЕРЕНИЯ </span>

              <h2>
                {{ formatDate(measurement.measuredAt) }}
              </h2>
            </div>

            <div class="card-actions">
              <RouterLink
                :to="{ name: 'vital-measurement-edit', params: { id: measurement.id } }"
                class="edit-button"
              >
                Редактировать
              </RouterLink>

              <button
                type="button"
                class="delete-button"
                :disabled="deletingId !== null || loading"
                @click="removeMeasurement(measurement.id)"
              >
                {{ deletingId === measurement.id ? 'Удаление...' : 'Удалить' }}
              </button>
            </div>
          </div>

          <div class="measurements-grid">
            <!-- Давление -->
            <div class="measurement-item">
              <span class="item-label"> Артериальное давление </span>

              <div class="measurement-value">
                <strong>
                  {{ measurement.systolicPressure }}
                  /
                  {{ measurement.diastolicPressure }}
                </strong>

                <span>мм рт. ст.</span>
              </div>

              <p>Систолическое / диастолическое</p>
            </div>

            <!-- Пульс -->
            <div class="measurement-item">
              <span class="item-label"> Пульс </span>

              <div class="measurement-value">
                <strong>
                  {{ measurement.pulse }}
                </strong>

                <span>уд/мин</span>
              </div>

              <p>Частота сердечных сокращений</p>
            </div>
          </div>

          <!-- Заметки -->
          <div v-if="measurement.notes" class="notes">
            <span>Заметки</span>

            <p>{{ measurement.notes }}</p>
          </div>
        </article>
      </div>

      <!-- Пагинация -->
      <nav v-if="totalPages > 1" class="pagination" aria-label="Страницы истории измерений">
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
.measurements-page {
  width: 100%;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 24px;
  margin-bottom: 32px;
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
  max-width: 650px;
  color: #737b8e;
  line-height: 1.6;
}

.add-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  padding: 13px 20px;
  border-radius: 10px;
  background: #6554bc;
  color: #fff;
  font: inherit;
  font-weight: 600;
  text-decoration: none;
  white-space: nowrap;
}

.add-button:hover {
  background: #5140a8;
}

/* Общая информация */

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 26px;
}

.summary-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 30px;
  min-width: 250px;
  padding: 20px 24px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 14px;
}

.summary-card span {
  color: #737b8e;
}

.summary-card strong {
  color: #44368e;
  font-size: 28px;
}

.page-size-field {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-size-field label {
  color: #596377;
  font-size: 13px;
  font-weight: 600;
}

.page-size-field select {
  width: 85px;
  padding: 11px;
  border: 1px solid #d9ddea;
  border-radius: 10px;
  background: #fff;
  color: #252e45;
  font: inherit;
}

/* История измерений */

.measurements-list {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.measurement-card {
  padding: 28px;
  background: #fff;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
}

.card-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 18px;
  margin-bottom: 26px;
}

.card-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.edit-button,
.delete-button {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  min-height: 42px;
  padding: 10px 15px;
  border-radius: 10px;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}

.edit-button {
  border: 1px solid #ded9f5;
  background: #f2effd;
  color: #6554bc;
}

.edit-button:hover {
  background: #e7e1fc;
}

.delete-button {
  border: 1px solid #f1c1c1;
  background: white;
  color: #ba3232;
}

.delete-button:hover:not(:disabled) {
  background: #fff1f1;
}

.delete-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.action-error {
  margin-bottom: 20px;
  padding: 14px 18px;
  border: 1px solid #f1c1c1;
  border-radius: 10px;
  background: #fff1f1;
  color: #ba3232;
  line-height: 1.5;
}

.card-label {
  color: #8a90a2;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.4px;
}

.card-heading h2 {
  margin: 10px 0 0;
  color: #252e45;
  font-size: 20px;
}

.measurements-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 22px;
}

.measurement-item {
  min-width: 0;
  padding: 22px;
  border: 1px solid #eeebfc;
  border-radius: 12px;
  background: #f9f8fe;
}

.item-label {
  color: #737b8e;
  font-size: 14px;
  font-weight: 600;
}

.measurement-value {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 14px;
}

.measurement-value strong {
  color: #44368e;
  font-size: clamp(24px, 3vw, 30px);
  font-weight: 700;
}

.measurement-value span {
  color: #737b8e;
  font-size: 14px;
}

.measurement-item p {
  margin: 10px 0 0;
  color: #8a90a2;
  font-size: 12px;
}

.notes {
  margin-top: 22px;
  padding: 18px;
  border-radius: 10px;
  background: #f7f7fb;
}

.notes span {
  color: #596377;
  font-size: 13px;
  font-weight: 700;
}

.notes p {
  margin: 8px 0 0;
  color: #596377;
  line-height: 1.6;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

/* Загрузка и пустой список */

.state-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 64px 24px;
  border: 1px solid #e9eaf0;
  border-radius: 16px;
  background: #fff;
  text-align: center;
}

.state-panel h2 {
  margin: 14px 0 8px;
  color: #252e45;
  font-size: 22px;
}

.state-panel p {
  max-width: 460px;
  color: #737b8e;
  line-height: 1.6;
}

.empty-icon {
  color: #6554bc;
  font-size: 42px;
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

/* Кнопки и пагинация */

.secondary-button {
  padding: 11px 16px;
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
  opacity: 0.5;
  cursor: not-allowed;
}

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
}

/* Мобильная версия */

@media (max-width: 800px) {
  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .add-button {
    align-self: flex-start;
    max-width: 100%;
    white-space: normal;
    text-align: center;
  }

  .summary-row {
    flex-direction: column;
    align-items: stretch;
  }

  .page-size-field {
    justify-content: space-between;
  }

  .measurements-grid {
    grid-template-columns: 1fr;
  }

  .measurement-card {
    padding: 20px;
  }

  .measurement-item {
    padding: 18px;
  }

  .pagination {
    gap: 10px;
  }
}
</style>
