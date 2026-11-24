const symptomLabels: Record<string, string> = {
  Dizziness: 'Головокружение',
  LightSensitivity: 'Чувствительность к свету',
  Nausea: 'Тошнота',
  Other: 'Другое',
  SoundSensitivity: 'Чувствительность к звуку',
  VisualDisturbance: 'Зрительные нарушения',
  Weakness: 'Слабость',
}

const triggerLabels: Record<string, string> = {
  BrightLight: 'Яркий свет',
  CertainFoods: 'Определённые продукты',
  Hunger: 'Голод',
  LackOfSleep: 'Недостаток сна',
  LoudNoise: 'Громкий шум',
  Other: 'Другое',
  PhysicalExertion: 'Физическая нагрузка',
  Stress: 'Стресс',
  WeatherChange: 'Изменение погоды',
}

export function getSymptomLabel(name: string): string {
  return symptomLabels[name] ?? name
}

export function getTriggerLabel(name: string): string {
  return triggerLabels[name] ?? name
}
