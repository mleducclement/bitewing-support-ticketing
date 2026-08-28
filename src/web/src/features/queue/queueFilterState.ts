import type { TicketPriority, TicketStatus } from '@/types/ticket'

/** Sentinel assignee value meaning "tickets with no assignee". */
export const UNASSIGNED = '__unassigned__'

export interface QueueFilterState {
  status: TicketStatus | 'all'
  priority: TicketPriority | 'all'
  assignee: string | 'all'
}

export const EMPTY_FILTERS: QueueFilterState = {
  status: 'all',
  priority: 'all',
  assignee: 'all',
}
