import { useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { CurrentUser } from '@/types/auth'

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

export interface Auth {
  status: AuthStatus
  user: CurrentUser | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => Promise<void>
}

export function useAuth(): Auth {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<CurrentUser | null>(null)

  // One-time session check on mount: GET /api/auth/me returns the user if the
  // cookie is valid, 401 otherwise.
  useEffect(() => {
    let cancelled = false

    apiFetch<CurrentUser>('/api/auth/me')
      .then((me) => {
        if (cancelled) return
        setUser(me)
        setStatus('authenticated')
      })
      .catch(() => {
        // 401, or the API being unreachable: either way, show the login screen.
        if (!cancelled) setStatus('anonymous')
      })

    return () => {
      cancelled = true
    }
  }, [])

  async function signIn(email: string, password: string) {
    await apiFetch('/api/auth/login', { method: 'POST', body: { email, password } })
    // Login succeeded, so /me will too; pull the profile for the header.
    const me = await apiFetch<CurrentUser>('/api/auth/me')
    setUser(me)
    setStatus('authenticated')
  }

  async function signOut() {
    await apiFetch('/api/auth/logout', { method: 'POST' })
    setUser(null)
    setStatus('anonymous')
  }

  return { status, user, signIn, signOut }
}