import { useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { Ticket } from '@/types/ticket'

export interface TicketResult {
  ticket: Ticket | null
  loading: boolean
  error: boolean
}

// Callers should mount this under a `key={id}` so a route change to another
// ticket gets fresh state rather than briefly showing the previous one.
export function useTicket(id: string): TicketResult {
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false

    apiFetch<Ticket>(`/api/tickets/${id}`)
      .then((data) => {
        if (cancelled) return
        setTicket(data)
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
  }, [id])

  return { ticket, loading, error }
}
