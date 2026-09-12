import type { TicketArea, TicketPriority, TicketStatus } from '@/types/ticket'

/** Sentinel assignee value meaning "tickets with no assignee". */
export const UNASSIGNED = '__unassigned__'

/** Sentinel status value meaning "the default open queue", i.e. everything but closed statuses. */
export const ACTIVE = '__active__'

export interface QueueFilterState {
  status: TicketStatus | 'all' | typeof ACTIVE
  priority: TicketPriority | 'all'
  /** An assignee id, UNASSIGNED, or 'all'. Matches bitewing.Dtos.Tickets.TicketQueueRequest.AssigneeId. */
  assignee: string | 'all'
  area: TicketArea | 'all'
}

export const EMPTY_FILTERS: QueueFilterState = {
  status: ACTIVE,
  priority: 'all',
  assignee: 'all',
  area: 'all',
}

/** Builds the /api/tickets query string for the given filters. Mirrors TicketQueueRequest. */
export function buildQueueQuery(filters: QueueFilterState): string {
  const params = new URLSearchParams()

  if (filters.status === 'all') {
    params.set('allStatuses', 'true')
  } else if (filters.status !== ACTIVE) {
    params.set('status', filters.status)
  }

  if (filters.priority !== 'all') {
    params.set('priority', filters.priority)
  }

  if (filters.area !== 'all') {
    params.set('area', filters.area)
  }

  if (filters.assignee === UNASSIGNED) {
    params.set('unassigned', 'true')
  } else if (filters.assignee !== 'all') {
    params.set('assigneeId', filters.assignee)
  }

  return params.toString()
}
