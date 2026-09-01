import { useState } from 'react'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { isPriorityDowngrade, PRIORITY_LABELS, priorityBadgeVariant } from '@/lib/ticketFormat'
import type { Ticket, TicketPriority } from '@/types/ticket'

const PRIORITY_OPTIONS: TicketPriority[] = ['Urgent', 'Normal', 'Low']

interface PriorityControlProps {
  ticket: Ticket
  /** The staged priority, if any - undefined means no pending change. */
  draftPriority: TicketPriority | undefined
  /** Stages a priority change (and, for downgrades, its reason) - no API call happens here. */
  onChange: (priority: TicketPriority | undefined, reason?: string) => void
  disabled?: boolean
}

export function PriorityControl({ ticket, draftPriority, onChange, disabled }: PriorityControlProps) {
  const [downgradeTarget, setDowngradeTarget] = useState<TicketPriority | null>(null)
  const [reason, setReason] = useState('')

  // Priority no longer affects queue routing once a ticket is closed, so it's
  // read-only from here on - matches the "block it" decision for closed tickets.
  if (ticket.status === 'Resolved' || ticket.status === 'Cancelled') {
    return <Badge variant={priorityBadgeVariant(ticket.priority)}>{PRIORITY_LABELS[ticket.priority]}</Badge>
  }

  function closeDowngradeDialog() {
    setDowngradeTarget(null)
    setReason('')
  }

  function handleValueChange(value: string) {
    const newPriority = value as TicketPriority
    if (newPriority === ticket.priority) {
      onChange(undefined)
      return
    }

    if (isPriorityDowngrade(ticket.priority, newPriority)) {
      setDowngradeTarget(newPriority)
      return
    }

    onChange(newPriority)
  }

  function confirmDowngrade() {
    if (!downgradeTarget || !reason.trim()) return
    onChange(downgradeTarget, reason.trim())
    closeDowngradeDialog()
  }

  return (
    <>
      <Select value={draftPriority ?? ticket.priority} onValueChange={handleValueChange} disabled={disabled}>
        <SelectTrigger size="sm" className="w-fit">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {PRIORITY_OPTIONS.map((priority) => (
            <SelectItem key={priority} value={priority}>
              {PRIORITY_LABELS[priority]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Dialog open={downgradeTarget !== null} onOpenChange={(open) => !open && closeDowngradeDialog()}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              Lower priority to {downgradeTarget ? PRIORITY_LABELS[downgradeTarget] : ''}?
            </DialogTitle>
            <DialogDescription>Downgrades require a reason.</DialogDescription>
          </DialogHeader>

          <div className="space-y-1.5">
            <Label htmlFor="priority-downgrade-reason">Reason</Label>
            <Input
              id="priority-downgrade-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Why is this less urgent?"
              autoFocus
            />
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={closeDowngradeDialog}>
              Back
            </Button>
            <Button disabled={!reason.trim()} onClick={confirmDowngrade}>
              Set reason
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}