import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { formatAge, formatDateTime } from '@/lib/ticketFormat'

import { useAddNote } from './useAddNote'
import { useTicketNotes } from './useTicketNotes'

// Internal notes: agent-authored, never shown to customers, append-only (spec §4).
// Owns its own data wiring - adding a note touches neither the ticket nor its
// event history, so it stays out of the detail page's refetchAll.
export function NotesSection({ ticketId }: { ticketId: string }) {
  const { notes, loading, error, refetch } = useTicketNotes(ticketId)
  const { addNote, pending } = useAddNote(ticketId, refetch)
  const [body, setBody] = useState('')

  async function submit() {
    const ok = await addNote(body.trim())
    if (ok) setBody('')
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Textarea
          value={body}
          onChange={(e) => setBody(e.target.value)}
          placeholder="Add an internal note. Only agents see this."
          rows={3}
          disabled={pending}
        />
        <Button size="sm" disabled={pending || body.trim().length === 0} onClick={() => void submit()}>
          {pending ? 'Adding…' : 'Add note'}
        </Button>
      </div>

      {error ? (
        <p className="text-sm text-muted-foreground">Could not load notes.</p>
      ) : loading ? (
        <p className="text-sm text-muted-foreground">Loading notes…</p>
      ) : notes.length === 0 ? (
        <p className="text-sm text-muted-foreground">No notes yet.</p>
      ) : (
        <ol className="space-y-4">
          {notes.map((note) => (
            <li key={note.id} className="rounded-lg border bg-background p-3">
              <p className="whitespace-pre-wrap text-sm leading-relaxed">{note.body}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                <span className="font-medium text-foreground/75">{note.authorName}</span>
                <time className="ml-2 tabular-nums" dateTime={note.createdAt} title={formatDateTime(note.createdAt)}>
                  {formatAge(note.createdAt)} ago
                </time>
              </p>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}