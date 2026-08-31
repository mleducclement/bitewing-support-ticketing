import { toast } from 'sonner'

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
  /**
   * Skip the automatic error toast. Pass this from call sites that render the
   * failure themselves (a full-screen state, an inline form message).
   */
  suppressErrorToast?: boolean
}

// A request that never settles leaves the caller stuck (e.g. a spinner forever).
// The dev proxy in particular hangs when the API is mid-restart: the socket is
// accepted but no response comes. Cap every request so a stall becomes an error.
const REQUEST_TIMEOUT_MS = 10_000

/** status 0 (couldn't reach the server) or any 5xx. */
export function isServerError(err: unknown): boolean {
  return err instanceof ApiError && (err.status === 0 || err.status >= 500)
}

// Fallback message when the response has no { message } body.
function defaultMessage(status: number): string {
  if (status === 0 || status >= 500) {
    return "Bitewing isn't responding. Try again in a moment."
  }
  switch (status) {
    case 403:
      return "You're not authorized to do that."
    case 404:
      return 'That item no longer exists.'
    default:
      return 'That request could not be completed.'
  }
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const text = await response.text()
    if (text) {
      const parsed = JSON.parse(text) as { message?: unknown; detail?: unknown }
      const message = parsed.message ?? parsed.detail
      if (typeof message === 'string' && message.length > 0) return message
    }
  } catch {
    // Non-JSON or empty body: fall through to the per-status default.
  }
  return defaultMessage(response.status)
}

// Thin wrapper over fetch for the same-origin API. Cookies flow automatically
// (prod: same origin; dev: through Vite's /api proxy). Throws ApiError on any
// non-2xx and on network failure (status 0), so callers can branch on status.
// Every failure except 401 also raises a toast unless suppressErrorToast is set;
// 401 is the normal logged-out signal and is left to useAuth.
export async function apiFetch<T>(
  path: string,
  { method = 'GET', body, signal, suppressErrorToast = false }: ApiFetchOptions = {},
): Promise<T> {
  const timeout = AbortSignal.timeout(REQUEST_TIMEOUT_MS)
  const abort = signal ? AbortSignal.any([signal, timeout]) : timeout

  let response: Response
  try {
    response = await fetch(path, {
      method,
      signal: abort,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (err) {
    // The caller aborted (unmount, superseded request): propagate untouched so
    // callers can ignore it. A timeout or network failure is a real "couldn't
    // reach the server", same as status 0.
    if (err instanceof DOMException && err.name === 'AbortError') throw err
    throw raise(new ApiError(0, defaultMessage(0)), suppressErrorToast)
  }

  if (!response.ok) {
    throw raise(new ApiError(response.status, await readErrorMessage(response)), suppressErrorToast)
  }

  // Several endpoints reply 200 with an empty body (login, logout).
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

function raise(error: ApiError, suppressErrorToast: boolean): ApiError {
  if (!suppressErrorToast && error.status !== 401) {
    // Stable id per status+message so StrictMode's double-invoke and repeated
    // identical failures collapse into one toast instead of stacking.
    toast.error(error.message, { id: `api-${error.status}-${error.message}` })
  }
  return error
}