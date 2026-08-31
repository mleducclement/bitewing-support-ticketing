import { useCallback, useEffect, useState } from 'react'

import { ApiError, apiFetch } from '@/lib/api'
import type { CurrentUser } from '@/types/auth'

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous' | 'error'

export interface Auth {
  status: AuthStatus
  user: CurrentUser | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => Promise<void>
  /** Re-run the session check, for recovering from the 'error' state. */
  retry: () => void
}

export function useAuth(): Auth {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  const retry = useCallback(() => {
    setStatus('loading')
    setReloadKey((key) => key + 1)
  }, [])

  // Session check on mount (and on retry): GET /api/auth/me returns the user if
  // the cookie is valid. A 401 means "not signed in"; anything else (500, server
  // unreachable) is an error we surface rather than a silent bounce to login.
  useEffect(() => {
    let cancelled = false

    apiFetch<CurrentUser>('/api/auth/me', { suppressErrorToast: true })
      .then((me) => {
        if (cancelled) return
        setUser(me)
        setStatus('authenticated')
      })
      .catch((err) => {
        if (cancelled) return
        setStatus(err instanceof ApiError && err.status === 401 ? 'anonymous' : 'error')
      })

    return () => {
      cancelled = true
    }
  }, [reloadKey])

  async function signIn(email: string, password: string) {
    await apiFetch('/api/auth/login', {
      method: 'POST',
      body: { email, password },
      suppressErrorToast: true,
    })
    // Login succeeded, so /me will too; pull the profile for the header.
    const me = await apiFetch<CurrentUser>('/api/auth/me')
    setUser(me)
    setStatus('authenticated')
  }

  async function signOut() {
    // A failed logout still fires an error toast via apiFetch; clear local state
    // regardless so the user isn't stuck on a screen they can't act on.
    try {
      await apiFetch('/api/auth/logout', { method: 'POST' })
    } finally {
      setUser(null)
      setStatus('anonymous')
    }
  }

  return { status, user, signIn, signOut, retry }
}