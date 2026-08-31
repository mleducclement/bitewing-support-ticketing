import { useState, type FormEvent } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ApiError, isServerError } from '@/lib/api'

interface LoginPageProps {
  onSignIn: (email: string, password: string) => Promise<void>
}

function messageFor(err: unknown): string {
  if (isServerError(err)) return "Bitewing isn't responding. Try again in a moment."
  if (err instanceof ApiError && err.status === 401) return 'Incorrect email or password.'
  if (err instanceof ApiError) return err.message
  return 'Could not sign in. Try again.'
}

export function LoginPage({ onSignIn }: LoginPageProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setPending(true)
    try {
      await onSignIn(email, password)
    } catch (err) {
      setError(messageFor(err))
      setPending(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm rounded-lg border p-6 shadow-sm"
      >
        <h1 className="text-lg font-semibold">Sign in to Bitewing</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Use your agent account.
        </p>

        <div className="mt-6 space-y-4">
          <div className="space-y-1.5">
            <label htmlFor="email" className="text-sm font-medium">
              Email
            </label>
            <Input
              id="email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="password" className="text-sm font-medium">
              Password
            </label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <Button type="submit" className="w-full" disabled={pending}>
            {pending ? 'Signing in…' : 'Sign in'}
          </Button>
        </div>
      </form>
    </div>
  )
}