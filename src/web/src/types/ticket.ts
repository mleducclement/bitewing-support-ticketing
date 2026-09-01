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
}