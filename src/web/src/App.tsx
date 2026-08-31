import type { ReactNode } from 'react'
import { Route, Routes } from 'react-router-dom'

import { AppHeader } from '@/components/AppHeader'
import { Button } from '@/components/ui/button'
import { Toaster } from '@/components/ui/sonner'
import { LoginPage } from '@/features/auth/LoginPage'
import { useAuth } from '@/features/auth/useAuth'
import { QueuePage } from '@/features/queue/QueuePage'
import { TicketDetailPage } from '@/features/ticket-detail/TicketDetailPage'

function App() {
  const { status, user, signIn, signOut, retry } = useAuth()

  let screen: ReactNode

  if (status === 'loading') {
    screen = (
      <div className="flex min-h-screen items-center justify-center bg-background text-sm text-muted-foreground">
        Loading…
      </div>
    )
  } else if (status === 'error') {
    screen = (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-sm rounded-lg border p-6 text-center shadow-sm">
          <h1 className="text-lg font-semibold">Can't reach Bitewing</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            The server isn't responding right now.
          </p>
          <Button variant="outline" size="sm" className="mt-4" onClick={retry}>
            Retry
          </Button>
        </div>
      </div>
    )
  } else if (status === 'anonymous' || !user) {
    screen = <LoginPage onSignIn={signIn} />
  } else {
    screen = (
      <div className="flex min-h-screen flex-col bg-background text-foreground">
        <AppHeader user={user} onSignOut={signOut} />
        <Routes>
          <Route path="/" element={<QueuePage />} />
          <Route path="/tickets/:id" element={<TicketDetailPage />} />
        </Routes>
      </div>
    )
  }

  return (
    <>
      <Toaster />
      {screen}
    </>
  )
}

export default App