import { useCallback, useState } from 'react'

import { apiFetch } from '@/lib/api'

export interface AddNoteState {
  addNote: (body: string) => Promise<boolean>
  pending: boolean
}

// POSTs a single note to /api/tickets/{ticketId}/notes and calls onAdded (a
// refetch) on success. apiFetch already toasts failures, so callers only need
// the returned boolean to decide whether to clear the compose box.
export function useAddNote(ticketId: string, onAdded: () => void): AddNoteState {
  const [pending, setPending] = useState(false)

  const addNote = useCallback(
    async (body: string): Promise<boolean> => {
      setPending(true)
      try {
        await apiFetch(`/api/tickets/${ticketId}/notes`, { method: 'POST', body: { body } })
        onAdded()
        return true
      } catch {
        return false
      } finally {
        setPending(false)
      }
    },
    [ticketId, onAdded],
  )

  return { addNote, pending }
}