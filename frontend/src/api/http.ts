export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export async function apiRequest<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`/api${endpoint}`, {
    ...options,
    headers,
  })

  if (!response.ok) {
    let message = `Ошибка запроса: ${response.status}`

    try {
      const error = (await response.json()) as {
        message?: string
        title?: string
      }

      message = error.message || error.title || message
    } catch {
      // Ответ сервера не содержит JSON.
    }

    throw new ApiError(response.status, message)
  }

  return (await response.json()) as T
}
