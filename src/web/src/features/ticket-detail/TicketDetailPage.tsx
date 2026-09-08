import {useCallback, type ReactNode} from 'react';
import {ArrowLeft, ArrowLeftRight, Clock} from 'lucide-react';
import {Link, Navigate, useParams} from 'react-router-dom';

import {Badge} from '@/components/ui/badge';
import {Button} from '@/components/ui/button';
import {
  formatAge,
  formatDateTime,
  formatDuration,
  HANDOFF_BADGE_CLASS,
} from '@/lib/ticketFormat';

import {NotesSection} from './NotesSection';
import {PriorityControl} from './PriorityControl';
import {StatusControl} from './StatusControl';
import {TicketTimeline} from './TicketTimeline';
import {useTicket} from './useTicket';
import {useTicketAction} from './useTicketAction';
import {useTicketDraft} from './useTicketDraft';
import {useTicketEvents} from './useTicketEvents';

// Shared small-caps treatment for section headings and <dl> labels.
const LABEL_CLASS = 'text-sm font-bold uppercase tracking-wide';
const FIELD_CLASS = 'text-xs font-medium uppercase tracking-wide text-muted-foreground';

function Section({title, children}: {title: string; children: ReactNode}) {
  return (
    <section className="rounded-lg border bg-card p-5">
      <h2 className={LABEL_CLASS}>{title}</h2>
      <div className="mt-3">{children}</div>
    </section>
  );
}

// Renders a label/value pair as grid cells; must live inside a <dl> using the
// `grid-cols-[max-content_1fr]` template below.
function Field({label, children}: {label: string; children: ReactNode}) {
  return (
    <>
      <dt className={FIELD_CLASS}>{label}</dt>
      <dd>{children}</dd>
    </>
  );
}

const DL_CLASS = 'grid grid-cols-[max-content_1fr] gap-x-8 gap-y-2.5 text-sm';

// Absolute timestamp with a relative suffix, so the three dates in "Activity"
// don't read as three identical strings.
function DateValue({iso}: {iso: string}) {
  return (
    <span>
      {formatDateTime(iso)}
      <span className="ml-2 text-muted-foreground">({formatAge(iso)} ago)</span>
    </span>
  );
}

export function TicketDetailPage() {
  const {reference} = useParams();

  // The /tickets/:reference route can't match without a segment, so this is only
  // a type-level guard; bounce to the queue if it ever somehow happens.
  if (!reference) return <Navigate to="/" replace/>;

  return <TicketDetail key={reference} reference={reference}/>;
}

function TicketDetail({reference}: { reference: string }) {
  const {ticket, loading, error, refetch} = useTicket(reference);
  const {
    events,
    loading: eventsLoading,
    error: eventsError,
    refetch: refetchEvents,
  } = useTicketEvents(ticket?.id ?? '');

  // A status/priority save changes the ticket and appends an event, so refresh both.
  const refetchAll = useCallback(() => {
    refetch();
    refetchEvents();
  }, [refetch, refetchEvents]);

  const actions = useTicketAction(ticket?.id ?? '', refetchAll);
  const draftState = useTicketDraft(ticket, actions);
  const isSaving = actions.pending !== null;

  return (
    <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-8">
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4"/>
        Back to queue
      </Link>

      {error ? (
        <div className="rounded-lg border bg-card p-8 text-center text-sm text-muted-foreground">
          Could not load this ticket.
        </div>
      ) : loading ? (
        <div className="rounded-lg border bg-card p-8 text-center text-sm text-muted-foreground">
          Loading ticket…
        </div>
      ) : (
        <article className="space-y-4">
          <header className="mb-1">
            <Badge variant="outline" className="font-normal text-muted-foreground">
              {ticket!.displayId}
            </Badge>
            <h1 className="mt-2 text-2xl font-semibold leading-tight">{ticket!.subject}</h1>
          </header>

          <Section title="Status">
            <div className="flex flex-wrap items-center gap-2">
              <StatusControl
                ticket={ticket!}
                draftStatus={draftState.draft.status}
                onChange={draftState.setStatus}
                disabled={isSaving}
              />
              <PriorityControl
                ticket={ticket!}
                draftPriority={draftState.draft.priority}
                onChange={draftState.setPriority}
                disabled={isSaving}
              />
              {ticket!.handoffFlag && (
                <Badge variant="outline" className={HANDOFF_BADGE_CLASS}>
                  <ArrowLeftRight className="size-3" aria-hidden/>
                  Handed off
                </Badge>
              )}
            </div>

            {draftState.hasChanges && (
              <div className="mt-3 flex items-center gap-2">
                <Button size="sm" disabled={isSaving} onClick={() => void draftState.save()}>
                  {isSaving ? 'Saving…' : 'Save changes'}
                </Button>
                <Button size="sm" variant="ghost" disabled={isSaving} onClick={draftState.discard}>
                  Discard changes
                </Button>
              </div>
            )}

            {ticket!.status === 'Blocked' && ticket!.blockedSince && (
              <p className="mt-3 flex items-center gap-1.5 text-sm text-amber-700 dark:text-amber-500">
                <Clock className="size-3.5 shrink-0" aria-hidden/>
                Waiting on the customer for {formatDuration(ticket!.blockedSince)}
                <span className="text-muted-foreground">
                  &middot; since {formatDateTime(ticket!.blockedSince)}
                </span>
              </p>
            )}

            <dl className={`mt-4 ${DL_CLASS}`}>
              <Field label="Assignee">
                {ticket!.assigneeName ?? <span className="text-muted-foreground italic">Unassigned</span>}
              </Field>
              {ticket!.status === 'Cancelled' && ticket!.cancellationReason && (
                <Field label="Reason">{ticket!.cancellationReason}</Field>
              )}
            </dl>
          </Section>

          <Section title="Customer">
            <dl className={DL_CLASS}>
              <Field label="Clinic">{ticket!.clinicName}</Field>
              <Field label="Contact">{ticket!.customerName}</Field>
              <Field label="Email">
                <a
                  href={`mailto:${ticket!.customerEmail}`}
                  className="hover:underline underline-offset-2"
                >
                  {ticket!.customerEmail}
                </a>
              </Field>
            </dl>
          </Section>

          <Section title="Activity">
            <dl className={DL_CLASS}>
              <Field label="Created"><DateValue iso={ticket!.createdAt}/></Field>
              <Field label="Last updated"><DateValue iso={ticket!.updatedAt}/></Field>
            </dl>
          </Section>

          <Section title="Message">
            <div className="whitespace-pre-wrap text-sm leading-relaxed">
              {ticket!.body}
            </div>
          </Section>

          <Section title="Notes">
            <NotesSection ticketId={ticket!.id}/>
          </Section>

          <Section title="History">
            <TicketTimeline events={events} loading={eventsLoading} error={eventsError}/>
          </Section>
        </article>
      )}
    </main>
  );
}