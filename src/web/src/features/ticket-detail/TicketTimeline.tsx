import { ArrowLeftRight, ArrowRight, Flag, Tag, UserCog } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { Badge } from '@/components/ui/badge'
import {
  formatAge,
  formatDateTime,
  PRIORITY_LABELS,
  priorityBadgeVariant,
  STATUS_LABELS,
  statusBadgeClass,
} from '@/lib/ticketFormat'
import type { TicketEvent, TicketEventType } from '@/types/ticket'

interface TicketTimelineProps {
  events: TicketEvent[]
  loading: boolean
  error: boolean
}

// icon: sits in the rail dot. fallbackLabel: full text for event types that have
// no before/after badges yet (assignment, classification); StatusChanged and
// PriorityChanged render a caption + badge pair instead.
const EVENT_META: Record<TicketEventType, { icon: LucideIcon; fallbackLabel: string }> = {
  StatusChanged: { icon: ArrowLeftRight, fallbackLabel: 'Status changed' },
  PriorityChanged: { icon: Flag, fallbackLabel: 'Priority changed' },
  AssignmentChanged: { icon: UserCog, fallbackLabel: 'Reassigned' },
  ClassificationCorrected: { icon: Tag, fallbackLabel: 'Classification corrected' },
}

const CAPTION_CLASS = 'text-xs font-semibold uppercase tracking-wide text-muted-foreground'

// The before -> after, rendered as the two real status/priority badges so the
// change is scannable by color rather than as a grey sentence.
function EventChange({ event }: { event: TicketEvent }) {
  if (event.eventType === 'StatusChanged' && event.fromStatus && event.toStatus) {
    return (
      <span className="inline-flex items-center gap-1.5">
        <span className={CAPTION_CLASS}>Status</span>
        <Badge variant="outline" className={statusBadgeClass(event.fromStatus)}>
          {STATUS_LABELS[event.fromStatus]}
        </Badge>
        <ArrowRight className="size-3 shrink-0 text-muted-foreground" aria-hidden />
        <Badge variant="outline" className={statusBadgeClass(event.toStatus)}>
          {STATUS_LABELS[event.toStatus]}
        </Badge>
      </span>
    )
  }

  if (event.eventType === 'PriorityChanged' && event.fromPriority && event.toPriority) {
    return (
      <span className="inline-flex items-center gap-1.5">
        <span className={CAPTION_CLASS}>Priority</span>
        <Badge variant={priorityBadgeVariant(event.fromPriority)}>
          {PRIORITY_LABELS[event.fromPriority]}
        </Badge>
        <ArrowRight className="size-3 shrink-0 text-muted-foreground" aria-hidden />
        <Badge variant={priorityBadgeVariant(event.toPriority)}>
          {PRIORITY_LABELS[event.toPriority]}
        </Badge>
      </span>
    )
  }

  return <span className="text-sm font-medium">{EVENT_META[event.eventType].fallbackLabel}</span>
}

export function TicketTimeline({ events, loading, error }: TicketTimelineProps) {
  if (error) {
    return <p className="text-sm text-muted-foreground">Could not load history.</p>
  }

  if (loading) {
    return <p className="text-sm text-muted-foreground">Loading history…</p>
  }

  if (events.length === 0) {
    return <p className="text-sm text-muted-foreground">No activity recorded yet.</p>
  }

  return (
    <ol className="relative space-y-6 border-l border-border pl-7">
      {events.map((event) => {
        const Icon = EVENT_META[event.eventType].icon

        return (
          <li key={event.id} className="relative">
            <span className="absolute -left-7 top-0 flex size-6 -translate-x-1/2 items-center justify-center rounded-full border bg-card text-muted-foreground">
              <Icon className="size-3.5" aria-hidden />
            </span>

            <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1">
              <EventChange event={event} />
              <time
                className="shrink-0 text-xs tabular-nums text-muted-foreground"
                dateTime={event.occurredAt}
                title={formatDateTime(event.occurredAt)}
              >
                {formatAge(event.occurredAt)} ago
              </time>
            </div>

            <p className="mt-1 text-xs text-muted-foreground">
              <span className="font-medium text-foreground/75">{event.actorName ?? 'System'}</span>
              {event.reason && <span className="italic"> · {event.reason}</span>}
            </p>
          </li>
        )
      })}
    </ol>
  )
}