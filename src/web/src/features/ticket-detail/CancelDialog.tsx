import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import type { CancellationReason } from '@/types/ticket'

// Expired is system-only (the auto-cancel job), so it's deliberately excluded
// here - matches the backend's [AllowedValues] restriction on CancelTicketRequest.
const REASON_OPTIONS: CancellationReason[] = ['Duplicate', 'Spam', 'Withdrawn']

interface CancelDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Stages the cancellation into the draft - no API call happens here. */
  onConfirm: (reason: CancellationReason) => void
}

export function CancelDialog({ open, onOpenChange, onConfirm }: CancelDialogProps) {
  const [reason, setReason] = useState<CancellationReason>()

  function handleOpenChange(next: boolean) {
    onOpenChange(next)
    if (!next) setReason(undefined)
  }

  function handleConfirm() {
    if (!reason) return
    onConfirm(reason)
    handleOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Cancel ticket</DialogTitle>
          <DialogDescription>Pick the reason this ticket is being cancelled.</DialogDescription>
        </DialogHeader>

        <Select value={reason} onValueChange={(value) => setReason(value as CancellationReason)}>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select a reason" />
          </SelectTrigger>
          <SelectContent>
            {REASON_OPTIONS.map((option) => (
              <SelectItem key={option} value={option}>
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Back
          </Button>
          <Button disabled={!reason} onClick={handleConfirm}>
            Set reason
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}