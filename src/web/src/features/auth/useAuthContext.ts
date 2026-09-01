import { createContext, useContext } from 'react'

import type { Auth } from './useAuth'

export const AuthContext = createContext<Auth | null>(null)

// Only rendered inside the authenticated branch of App.tsx, so `user` is
// always non-null here even though Auth.user is nullable in general.
export function useAuthContext(): Auth {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthContext must be used within an AuthProvider')
  return ctx
}