import { useCallback, useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { TicketEvent } from '@/types/ticket'

export interface TicketEventsResult {
  events: TicketEvent[]
  loading: boolean
  error: boolean
  refetch: () => void
}

// Loads a ticket's event history (newest-first, as the API returns it). Pass the
// ticket's GUID; an empty string skips the fetch, so the detail page can call
// this before `useTicket` has resolved. Mirrors useTicket's fetch shape.
export function useTicketEvents(ticketId: string): TicketEventsResult {
  const [events, setEvents] = useState<TicketEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  const refetch = useCallback(() => {
    setReloadKey((key) => key + 1)
  }, [])

  useEffect(() => {
    if (ticketId === '') return

    let cancelled = false

    apiFetch<TicketEvent[]>(`/api/tickets/${ticketId}/events`)
      .then((data) => {
        if (cancelled) return
        setEvents(data)
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

  return { events, loading, error, refetch }
}