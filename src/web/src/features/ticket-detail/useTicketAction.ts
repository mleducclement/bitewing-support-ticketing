import { useCallback, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { CancellationReason, TicketPriority } from '@/types/ticket'

export type TicketActionName =
  | 'claim'
  | 'release'
  | 'block'
  | 'unblock'
  | 'resolve'
  | 'cancel'
  | 'changePriority'

export interface TicketActions {
  /** The action currently in flight, if any - lets a button disable just itself. */
  pending: TicketActionName | null
  claim: () => Promise<boolean>
  release: () => Promise<boolean>
  block: () => Promise<boolean>
  unblock: () => Promise<boolean>
  resolve: () => Promise<boolean>
  cancel: (reason: CancellationReason) => Promise<boolean>
  changePriority: (newPriority: TicketPriority, reason?: string) => Promise<boolean>
}

// One POST per verb against /api/tickets/{ticketId}/..., refetching the ticket
// on success. apiFetch already toasts failures, so callers only need the
// returned boolean to decide whether to close a confirmation dialog.
export function useTicketAction(ticketId: string, refetch: () => void): TicketActions {
  const [pending, setPending] = useState<TicketActionName | null>(null)

  const run = useCallback(
    async (name: TicketActionName, path: string, body?: unknown): Promise<boolean> => {
      setPending(name)
      try {
        await apiFetch(path, { method: 'POST', body })
        refetch()
        return true
      } catch {
        return false
      } finally {
        setPending(null)
      }
    },
    [refetch],
  )

  return {
    pending,
    claim: () => run('claim', `/api/tickets/${ticketId}/claim`),
    release: () => run('release', `/api/tickets/${ticketId}/release`),
    block: () => run('block', `/api/tickets/${ticketId}/block`),
    unblock: () => run('unblock', `/api/tickets/${ticketId}/unblock`),
    resolve: () => run('resolve', `/api/tickets/${ticketId}/resolve`),
    cancel: (reason) => run('cancel', `/api/tickets/${ticketId}/cancel`, { reason }),
    changePriority: (newPriority, reason) =>
      run('changePriority', `/api/tickets/${ticketId}/priority`, { newPriority, reason: reason ?? null }),
  }
}