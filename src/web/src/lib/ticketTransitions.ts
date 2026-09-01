import type { TicketActionName } from '@/features/ticket-detail/useTicketAction'
import type { TicketStatus } from '@/types/ticket'

export type TransitionAction = Exclude<TicketActionName, 'cancel' | 'changePriority'>

// Mirrors TicketTransitions.IsLegal (spec §3) plus which verb endpoint fulfills
// each transition. Cancelled isn't listed here - every status but Resolved and
// Cancelled itself can reach it, and it always goes through the reason dialog
// rather than firing directly, so callers handle it separately.
export const TRANSITION_ACTION: Partial<Record<TicketStatus, Partial<Record<TicketStatus, TransitionAction>>>> = {
  Open: { InProgress: 'claim' },
  InProgress: { Blocked: 'block', Open: 'release', Resolved: 'resolve' },
  Blocked: { InProgress: 'unblock', Open: 'release' },
}

// Claim (from Open) and Cancel-from-Open need no ownership; every other
// transition does - mirrors EnsureOwnerOrTeamLead / CancelAsync's conditional
// ownership check.
export function legalNextStatuses(current: TicketStatus, isOwnerOrLead: boolean): TicketStatus[] {
  const targets: TicketStatus[] = []
  const transitions = TRANSITION_ACTION[current]
  if (transitions && (current === 'Open' || isOwnerOrLead)) {
    targets.push(...(Object.keys(transitions) as TicketStatus[]))
  }
  if ((current === 'Open' || isOwnerOrLead) && current !== 'Resolved' && current !== 'Cancelled') {
    targets.push('Cancelled')
  }
  return targets
}
