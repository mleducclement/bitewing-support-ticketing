import { useCallback, useEffect, useState } from 'react'

import { apiFetch } from '@/lib/api'
import type { Ticket } from '@/types/ticket'

export interface TicketsResult {
  tickets: Ticket[]
  loading: boolean
  error: boolean
  refetch: () => void
}

export function useTickets(query: string): TicketsResult {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  const refetch = useCallback(() => {
    setLoading(true)
    setError(false)
    setReloadKey((key) => key + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    const path = query ? `/api/tickets?${query}` : '/api/tickets'

    apiFetch<Ticket[]>(path)
      .then((data) => {
        if (cancelled) return
        setTickets(data)
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
  }, [query, reloadKey])

  return { tickets, loading, error, refetch }
}