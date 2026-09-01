import { useState } from 'react'

import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useAuthContext } from '@/features/auth/useAuthContext'
import { STATUS_LABELS, statusBadgeClass } from '@/lib/ticketFormat'
import { legalNextStatuses } from '@/lib/ticketTransitions'
import type { CancellationReason, Ticket, TicketStatus } from '@/types/ticket'

import { CancelDialog } from './CancelDialog'

interface StatusControlProps {
  ticket: Ticket
  /** The staged status, if any - undefined means no pending change. */
  draftStatus: TicketStatus | undefined
  /** Stages a status change (and, for Cancelled, its reason) - no API call happens here. */
  onChange: (status: TicketStatus | undefined, cancellationReason?: CancellationReason) => void
  disabled?: boolean
}

export function StatusControl({ ticket, draftStatus, onChange, disabled }: StatusControlProps) {
  const { user } = useAuthContext()
  const [cancelOpen, setCancelOpen] = useState(false)

  if (ticket.status === 'Resolved' || ticket.status === 'Cancelled') {
    return (
      <Badge variant="outline" className={statusBadgeClass(ticket.status)}>
        {STATUS_LABELS[ticket.status]}
      </Badge>
    )
  }

  if (!user) return null

  const isOwnerOrLead = user.roles.includes('TeamLead') || ticket.assigneeId === user.id
  const nextStatuses = legalNextStatuses(ticket.status, isOwnerOrLead)

  function handleValueChange(value: string) {
    const target = value as TicketStatus
    if (target === ticket.status) {
      onChange(undefined)
      return
    }

    if (target === 'Cancelled') {
      setCancelOpen(true)
      return
    }

    onChange(target)
  }

  return (
    <>
      <Select value={draftStatus ?? ticket.status} onValueChange={handleValueChange} disabled={disabled}>
        <SelectTrigger size="sm" className="w-fit">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={ticket.status}>{STATUS_LABELS[ticket.status]}</SelectItem>
          {nextStatuses.map((status) => (
            <SelectItem key={status} value={status}>
              {STATUS_LABELS[status]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <CancelDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        onConfirm={(reason) => onChange('Cancelled', reason)}
      />
    </>
  )
}