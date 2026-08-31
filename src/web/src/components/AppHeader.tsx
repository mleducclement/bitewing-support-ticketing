import { Link } from 'react-router-dom'

import { Button } from '@/components/ui/button'
import type { CurrentUser } from '@/types/auth'

interface AppHeaderProps {
  user: CurrentUser
  onSignOut: () => void
}

export function AppHeader({ user, onSignOut }: AppHeaderProps) {
  return (
    <header className="border-b">
      <div className="mx-auto flex h-14 w-full max-w-6xl items-center justify-between px-6">
        <div className="flex items-center gap-6">
          <span className="text-sm font-semibold tracking-tight">Bitewing</span>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/" className="font-medium text-foreground hover:text-foreground/80">
              Queue
            </Link>
          </nav>
        </div>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted-foreground">
            {user.firstName} {user.lastName}
          </span>
          <Button variant="ghost" size="sm" onClick={onSignOut}>
            Sign out
          </Button>
        </div>
      </div>
    </header>
  )
}