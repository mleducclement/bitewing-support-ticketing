import { useCallback, useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { Note } from '@/types/ticket'

export interface TicketNotesResult {
  notes: Note[]
  loading: boolean
  error: boolean
  refetch: () => void
}

// Loads a ticket's notes (newest-first, as the API returns them). Pass the
// ticket's GUID; an empty string skips the fetch, so the detail page can call
// this before `useTicket` has resolved. Mirrors useTicketEvents.
export function useTicketNotes(ticketId: string): TicketNotesResult {
  const [notes, setNotes] = useState<Note[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  const refetch = useCallback(() => {
    setReloadKey((key) => key + 1)
  }, [])

  useEffect(() => {
    if (ticketId === '') return

    let cancelled = false

    apiFetch<Note[]>(`/api/tickets/${ticketId}/notes`)
      .then((data) => {
        if (cancelled) return
        setNotes(data)
        setError(false)
        setLoading(false)
      })
      .catch(() => {
        if (cancelled) return
        setError(true)
        setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [ticketId, reloadKey])

  return { notes, loading, error, refetch }
}