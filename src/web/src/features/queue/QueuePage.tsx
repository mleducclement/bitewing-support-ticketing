import { useMemo, useState } from 'react'

import { Button } from '@/components/ui/button'
import { QueueFilters, type Assignee } from './QueueFilters'
import {
  ACTIVE,
  buildQueueQuery,
  EMPTY_FILTERS,
  type QueueFilterState,
} from './queueFilterState'
import { TicketQueueTable } from './TicketQueueTable'
import { useTickets } from './useTickets'

export function QueuePage() {
  const [filters, setFilters] = useState<QueueFilterState>(EMPTY_FILTERS)
  const query = useMemo(() => buildQueueQuery(filters), [filters])
  const { tickets, loading, error, refetch } = useTickets(query)

  const isDefaultFilters =
    filters.status === ACTIVE &&
    filters.priority === 'all' &&
    filters.assignee === 'all' &&
    filters.area === 'all'

  const assignees = useMemo<Assignee[]>(() => {
    const byId = new Map<string, string>()
    for (const ticket of tickets) {
      if (ticket.assigneeId && ticket.assigneeName) {
        byId.set(ticket.assigneeId, ticket.assigneeName)
      }
    }
    return [...byId.entries()]
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name))
  }, [tickets])

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
            isDefaultFilters
              ? 'The queue is empty.'
              : 'No tickets match these filters.'
          }
        />
      )}
    </main>
  )
}
