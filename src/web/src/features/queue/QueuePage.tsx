import { useMemo, useState } from 'react'

import { Button } from '@/components/ui/button'
import { QueueFilters } from './QueueFilters'
import {
  EMPTY_FILTERS,
  UNASSIGNED,
  type QueueFilterState,
} from './queueFilterState'
import { TicketQueueTable } from './TicketQueueTable'
import { comparePriorityThenAge } from './ticketFormat'
import { useTickets } from './useTickets'

export function QueuePage() {
  const { tickets: allTickets, loading, error, refetch } = useTickets()
  const [filters, setFilters] = useState<QueueFilterState>(EMPTY_FILTERS)

  const assignees = useMemo(
    () =>
      [
        ...new Set(
          allTickets
            .map((ticket) => ticket.assigneeName)
            .filter((name): name is string => name !== null),
        ),
      ].sort((a, b) => a.localeCompare(b)),
    [allTickets],
  )

  const tickets = useMemo(() => {
    return allTickets
      .filter((ticket) => {
        if (filters.status !== 'all' && ticket.status !== filters.status) {
          return false
        }
        if (
          filters.priority !== 'all' &&
          ticket.priority !== filters.priority
        ) {
          return false
        }
        if (filters.assignee === UNASSIGNED && ticket.assigneeName !== null) {
          return false
        }
        if (
          filters.assignee !== 'all' &&
          filters.assignee !== UNASSIGNED &&
          ticket.assigneeName !== filters.assignee
        ) {
          return false
        }
        return true
      })
      .sort(comparePriorityThenAge)
  }, [allTickets, filters])

  return (
    <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h1 className="text-xl font-semibold">Ticket queue</h1>
        <span className="text-sm text-muted-foreground">
          {tickets.length} {tickets.length === 1 ? 'ticket' : 'tickets'}
        </span>
      </div>

      <div className="mb-4">
        <QueueFilters
          value={filters}
          assignees={assignees}
          onChange={setFilters}
        />
      </div>

      {error ? (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          <p>Could not load the queue.</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={refetch}>
            Retry
          </Button>
        </div>
      ) : loading ? (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Loading tickets…
        </div>
      ) : (
        <TicketQueueTable
          tickets={tickets}
          emptyMessage={
            allTickets.length === 0
              ? 'The queue is empty.'
              : 'No tickets match these filters.'
          }
        />
      )}
    </main>
  )
}