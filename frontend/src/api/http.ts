export interface ApiErrorData {
  status?: number
  code?: string
  message?: string
  title?: string
  errors?: Record<string, string[]>
  traceId?: string
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly code?: string,
    public readonly errors?: Record<string, string[]>,
    public readonly traceId?: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export interface ApiRequestOptions extends RequestInit {
  auth?: boolean
}

let unauthorizedHandler: (() => void) | null = null

export function setUnauthorizedHandler(handler: () => void) {
  unauthorizedHandler = handler
}

async function sendRequest(endpoint: string, options: ApiRequestOptions = {}): Promise<Response> {
  const { auth = true, ...requestOptions } = options

  const headers = new Headers(requestOptions.headers)

  if (typeof requestOptions.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const token = auth ? sessionStorage.getItem('accessToken') : null

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response: Response

  try {
    response = await fetch(`/api${endpoint}`, {
      ...requestOptions,
      headers,
    })
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      throw error
    }

    throw new ApiError(0, 'Не удалось подключиться к серверу.', 'network_error')
  }

  if (!response.ok) {
    let errorData: ApiErrorData = {}

    try {
      const contentType = response.headers.get('Content-Type') ?? ''

      if (contentType.includes('json')) {
        errorData = (await response.json()) as ApiErrorData
      }
    } catch {
      // Не удалось прочитать ответ сервера.
    }

    const error = new ApiError(
      response.status,
      errorData.message ?? errorData.title ?? `Ошибка запроса: ${response.status}`,
      errorData.code,
      errorData.errors,
      errorData.traceId,
    )

    if (response.status === 401 && token && sessionStorage.getItem('accessToken') === token) {
      unauthorizedHandler?.()
    }

    throw error
  }

  return response
}

export async function apiRequest<T>(endpoint: string, options: ApiRequestOptions = {}): Promise<T> {
  const response = await sendRequest(endpoint, options)

  if (response.status === 204 || response.status === 205) {
    return undefined as T
  }

  const text = await response.text()

  if (!text) {
    return undefined as T
  }

  return JSON.parse(text) as T
}

export async function apiDownload(
  endpoint: string,
  options: ApiRequestOptions = {},
): Promise<Blob> {
  const response = await sendRequest(endpoint, options)

  return response.blob()
}
