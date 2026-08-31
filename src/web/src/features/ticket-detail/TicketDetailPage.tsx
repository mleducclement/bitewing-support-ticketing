import { ArrowLeft } from 'lucide-react'
import { Link, Navigate, useParams } from 'react-router-dom'

import { useTicket } from './useTicket'

export function TicketDetailPage() {
  const { id } = useParams()

  // The /tickets/:id route can't match without a segment, so this is only a
  // type-level guard; bounce to the queue if it ever somehow happens.
  if (!id) return <Navigate to="/" replace />

  return <TicketDetail key={id} ticketId={id} />
}

function TicketDetail({ ticketId }: { ticketId: string }) {
  const { ticket, loading, error } = useTicket(ticketId)

  return (
    <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to queue
      </Link>

      {error ? (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Could not load this ticket.
        </div>
      ) : loading ? (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Loading ticket…
        </div>
      ) : (
        <h1 className="text-xl font-semibold">{ticket!.subject}</h1>
      )}
    </main>
  )
}