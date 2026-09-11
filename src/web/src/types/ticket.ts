// Mirrors bitewing.Dtos.Tickets.TicketResponse. Enums are serialized as their
// string names by the API's default JSON options.

export type TicketStatus =
  | 'Open'
  | 'InProgress'
  | 'Blocked'
  | 'Resolved'
  | 'Cancelled'

export type TicketPriority = 'Low' | 'Normal' | 'Urgent'

export type CancellationReason =
  | 'Duplicate'
  | 'Spam'
  | 'Withdrawn'
  | 'Expired'

// Mirrors bitewing.Data.TicketArea / TicketType / ClassificationSource.
export type TicketArea =
  | 'Claims'
  | 'BookingAndCalendar'
  | 'Reminders'
  | 'AccessAndAccounts'
  | 'SubscriptionAndInvoicing'
  | 'Other'

export type TicketType = 'Broken' | 'HowTo' | 'FeatureRequest'

export type ClassificationSource = 'Model' | 'Human'

// Mirrors bitewing.Data.TicketEventType. Only StatusChanged and PriorityChanged
// are emitted today; the other two are defined server-side for later slices.
export type TicketEventType =
  | 'StatusChanged'
  | 'AssignmentChanged'
  | 'PriorityChanged'
  | 'ClassificationCorrected'

// Mirrors bitewing.Dtos.Tickets.TicketEventResponse. The API flattens the actor
// to a display name; actorName is null for system-triggered events (auto-cancel).
export interface TicketEvent {
  id: string
  eventType: TicketEventType
  actorName: string | null
  fromStatus: TicketStatus | null
  toStatus: TicketStatus | null
  fromPriority: TicketPriority | null
  toPriority: TicketPriority | null
  reason: string | null
  occurredAt: string
}

// Mirrors bitewing.Dtos.Tickets.NoteResponse. Internal, agent-authored, never
// shown to customers (spec §4). Append-only: no edit, no delete.
export interface Note {
  id: string
  authorName: string
  body: string
  createdAt: string
}

export interface Ticket {
  id: string
  displayId: string
  subject: string
  body: string
  customerName: string
  customerEmail: string
  clinicName: string
  status: TicketStatus
  priority: TicketPriority
  createdAt: string
  updatedAt: string
  assigneeId: string | null
  assigneeName: string | null
  handoffFlag: boolean
  blockedSince: string | null
  cancellationReason: CancellationReason | null
  area: TicketArea | null
  areaSource: ClassificationSource | null
  type: TicketType | null
  typeSource: ClassificationSource | null
}