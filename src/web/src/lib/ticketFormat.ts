import type { VariantProps } from 'class-variance-authority'

import type { badgeVariants } from '@/components/ui/badge'
import type { TicketPriority, TicketStatus } from '@/types/ticket'

type BadgeVariant = NonNullable<VariantProps<typeof badgeVariants>['variant']>

export const STATUS_LABELS: Record<TicketStatus, string> = {
  Open: 'Open',
  InProgress: 'In progress',
  Blocked: 'Blocked',
  Resolved: 'Resolved',
  Cancelled: 'Cancelled',
}

export const PRIORITY_LABELS: Record<TicketPriority, string> = {
  Low: 'Low',
  Normal: 'Normal',
  Urgent: 'Urgent',
}

// Priority-then-age is the queue's sort order (spec §5). Higher number sorts first.
const PRIORITY_RANK: Record<TicketPriority, number> = {
  Urgent: 3,
  Normal: 2,
  Low: 1,
}

// Status uses a fixed color language rather than the neutral badge variants:
// blue = being worked, yellow = waiting, green = done, red = closed unresolved.
// Open keeps the plain outline look. Each class set is tuned for light and dark.
const STATUS_BADGE_CLASSES: Record<TicketStatus, string> = {
  Open: '',
  InProgress:
    'border-transparent bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300',
  Blocked:
    'border-transparent bg-yellow-100 text-yellow-800 dark:bg-yellow-950 dark:text-yellow-300',
  Resolved:
    'border-transparent bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-300',
  Cancelled:
    'border-transparent bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300',
}

export function statusBadgeClass(status: TicketStatus): string {
  return STATUS_BADGE_CLASSES[status]
}

export function priorityBadgeVariant(priority: TicketPriority): BadgeVariant {
  switch (priority) {
    case 'Urgent':
      return 'destructive'
    case 'Normal':
      return 'secondary'
    case 'Low':
      return 'outline'
  }
}

export function comparePriorityThenAge(
  a: { priority: TicketPriority; createdAt: string },
  b: { priority: TicketPriority; createdAt: string },
): number {
  const byPriority = PRIORITY_RANK[b.priority] - PRIORITY_RANK[a.priority]
  if (byPriority !== 0) return byPriority
  // Older ticket first.
  return Date.parse(a.createdAt) - Date.parse(b.createdAt)
}

export function formatAge(createdAt: string, now: number = Date.now()): string {
  const minutes = Math.max(0, Math.floor((now - Date.parse(createdAt)) / 60_000))
  if (minutes < 60) return `${minutes}m`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h`
  const days = Math.floor(hours / 24)
  return `${days}d`
}