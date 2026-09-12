import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Button } from '@/components/ui/button'
import type { TicketArea, TicketPriority, TicketStatus } from '@/types/ticket'
import { AREA_LABELS, PRIORITY_LABELS, STATUS_LABELS } from '../../lib/ticketFormat.ts'
import {
  ACTIVE,
  EMPTY_FILTERS,
  UNASSIGNED,
  type QueueFilterState,
} from './queueFilterState'

const STATUS_ORDER: TicketStatus[] = [
  'Open',
  'InProgress',
  'Blocked',
  'Resolved',
  'Cancelled',
]

const PRIORITY_ORDER: TicketPriority[] = ['Urgent', 'Normal', 'Low']

const AREA_ORDER: TicketArea[] = [
  'Claims',
  'BookingAndCalendar',
  'Reminders',
  'AccessAndAccounts',
  'SubscriptionAndInvoicing',
  'Other',
]

export interface Assignee {
  id: string
  name: string
}

interface QueueFiltersProps {
  value: QueueFilterState
  /** Distinct agents present in the queue, for the assignee dropdown. */
  assignees: Assignee[]
  onChange: (next: QueueFilterState) => void
}

export function QueueFilters({ value, assignees, onChange }: QueueFiltersProps) {
  const isDirty =
    value.status !== ACTIVE ||
    value.priority !== 'all' ||
    value.assignee !== 'all' ||
    value.area !== 'all'

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select
        value={value.status}
        onValueChange={(status) =>
          onChange({ ...value, status: status as QueueFilterState['status'] })
        }
      >
        <SelectTrigger className="w-40" aria-label="Filter by status">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={ACTIVE}>Open queue</SelectItem>
          <SelectItem value="all">All statuses</SelectItem>
          {STATUS_ORDER.map((status) => (
            <SelectItem key={status} value={status}>
              {STATUS_LABELS[status]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={value.priority}
        onValueChange={(priority) =>
          onChange({
            ...value,
            priority: priority as QueueFilterState['priority'],
          })
        }
      >
        <SelectTrigger className="w-40" aria-label="Filter by priority">
          <SelectValue placeholder="Priority" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All priorities</SelectItem>
          {PRIORITY_ORDER.map((priority) => (
            <SelectItem key={priority} value={priority}>
              {PRIORITY_LABELS[priority]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={value.assignee}
        onValueChange={(assignee) => onChange({ ...value, assignee })}
      >
        <SelectTrigger className="w-48" aria-label="Filter by assignee">
          <SelectValue placeholder="Assignee" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">Any assignee</SelectItem>
          <SelectItem value={UNASSIGNED}>Unassigned</SelectItem>
          {assignees.map((assignee) => (
            <SelectItem key={assignee.id} value={assignee.id}>
              {assignee.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={value.area}
        onValueChange={(area) =>
          onChange({ ...value, area: area as QueueFilterState['area'] })
        }
      >
        <SelectTrigger className="w-48" aria-label="Filter by area">
          <SelectValue placeholder="Area" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All areas</SelectItem>
          {AREA_ORDER.map((area) => (
            <SelectItem key={area} value={area}>
              {AREA_LABELS[area]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {isDirty && (
        <Button variant="ghost" size="sm" onClick={() => onChange(EMPTY_FILTERS)}>
          Clear
        </Button>
      )}
    </div>
  )
}