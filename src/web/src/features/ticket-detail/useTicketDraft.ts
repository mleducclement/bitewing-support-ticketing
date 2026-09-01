import { useState } from 'react'

import { TRANSITION_ACTION } from '@/lib/ticketTransitions'
import type { CancellationReason, Ticket, TicketPriority, TicketStatus } from '@/types/ticket'

import type { TicketActions } from './useTicketAction'

// Every field an agent can stage on the detail page before saving. Extend this
// (and save() below) for notes once that lands - same pattern, one more
// optional field plus one more branch in save().
export interface TicketDraft {
  status?: TicketStatus
  cancellationReason?: CancellationReason
  priority?: TicketPriority
  priorityReason?: string
}

export interface TicketDraftState {
  draft: TicketDraft
  hasChanges: boolean
  setStatus: (status: TicketStatus | undefined, cancellationReason?: CancellationReason) => void
  setPriority: (priority: TicketPriority | undefined, priorityReason?: string) => void
  discard: () => void
  /** Applies every staged field via its own endpoint. Fields that fail stay staged for retry. */
  save: () => Promise<boolean>
}

export function useTicketDraft(ticket: Ticket | null, actions: TicketActions): TicketDraftState {
  const [draft, setDraft] = useState<TicketDraft>({})

  const hasChanges = draft.status !== undefined || draft.priority !== undefined

  function setStatus(status: TicketStatus | undefined, cancellationReason?: CancellationReason) {
    setDraft((d) => ({ ...d, status, cancellationReason }))
  }

  function setPriority(priority: TicketPriority | undefined, priorityReason?: string) {
    setDraft((d) => ({ ...d, priority, priorityReason }))
  }

  function discard() {
    setDraft({})
  }

  async function save(): Promise<boolean> {
    if (!ticket) return false
    let allOk = true

    if (draft.priority !== undefined) {
      const ok = await actions.changePriority(draft.priority, draft.priorityReason)
      if (ok) setDraft((d) => ({ ...d, priority: undefined, priorityReason: undefined }))
      else allOk = false
    }

    if (draft.status !== undefined) {
      if (draft.status === 'Cancelled') {
        const ok = draft.cancellationReason ? await actions.cancel(draft.cancellationReason) : false
        if (ok) setDraft((d) => ({ ...d, status: undefined, cancellationReason: undefined }))
        else allOk = false
      } else {
        const actionName = TRANSITION_ACTION[ticket.status]?.[draft.status]
        const ok = actionName ? await actions[actionName]() : false
        if (ok) setDraft((d) => ({ ...d, status: undefined }))
        else allOk = false
      }
    }

    return allOk
  }

  return { draft, hasChanges, setStatus, setPriority, discard, save }
}
