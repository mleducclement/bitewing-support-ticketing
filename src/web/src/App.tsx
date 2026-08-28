import { AppHeader } from '@/components/AppHeader'
import { LoginPage } from '@/features/auth/LoginPage'
import { useAuth } from '@/features/auth/useAuth'
import { QueuePage } from '@/features/queue/QueuePage'

function App() {
  const { status, user, signIn, signOut } = useAuth()

  if (status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-sm text-muted-foreground">
        Loading…
      </div>
    )
  }

  if (status === 'anonymous' || !user) {
    return <LoginPage onSignIn={signIn} />
  }

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <AppHeader user={user} onSignOut={signOut} />
      <QueuePage />
    </div>
  )
}

export default App