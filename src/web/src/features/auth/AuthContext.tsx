import type { ReactNode } from 'react'

import { AuthContext } from './useAuthContext'
import type { Auth } from './useAuth'

export function AuthProvider({ value, children }: { value: Auth; children: ReactNode }) {
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}