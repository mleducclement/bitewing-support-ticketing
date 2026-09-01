import { useCallback, useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { Ticket } from '@/types/ticket'

export interface TicketResult {
  ticket: Ticket | null
  loading: boolean
  error: boolean
  refetch: () => void
}

// `reference` is the CS-{n} display id (or a raw GUID); it goes straight into the
// request path. Callers should mount this under a `key={reference}` so a route
// change to another ticket gets fresh state rather than briefly showing the
// previous one.
export function useTicket(reference: string): TicketResult {
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  const refetch = useCallback(() => {
    setReloadKey((key) => key + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    apiFetch<Ticket>(`/api/tickets/${reference}`)
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
  }, [reference, reloadKey])

  return { ticket, loading, error, refetch }
}
