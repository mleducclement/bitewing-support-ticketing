export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface ApiFetchOptions {
  method?: string
  body?: unknown
  signal?: AbortSignal
}

// Thin wrapper over fetch for the same-origin API. Cookies flow automatically
// (prod: same origin; dev: through Vite's /api proxy). Throws ApiError on any
// non-2xx so callers can branch on status (401 in particular).
export async function apiFetch<T>(
  path: string,
  { method = 'GET', body, signal }: ApiFetchOptions = {},
): Promise<T> {
  const response = await fetch(path, {
    method,
    signal,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    throw new ApiError(response.status, `${method} ${path} failed with ${response.status}`)
  }

  // Several endpoints reply 200 with an empty body (login, logout).
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}