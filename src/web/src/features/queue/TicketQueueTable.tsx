import { ArrowLeftRight, ChevronRight } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { Ticket } from '@/types/ticket'
import {
  PRIORITY_LABELS,
  STATUS_LABELS,
  formatAge,
  priorityBadgeVariant,
  statusBadgeClass,
} from './ticketFormat'

interface TicketQueueTableProps {
  tickets: Ticket[]
  emptyMessage: string
}

export function TicketQueueTable({ tickets, emptyMessage }: TicketQueueTableProps) {
  const navigate = useNavigate()

  return (
    <div className="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/50">
            <TableHead>Ticket</TableHead>
            <TableHead>Subject</TableHead>
            <TableHead>Clinic</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Priority</TableHead>
            <TableHead>Assignee</TableHead>
            <TableHead className="text-right">Age</TableHead>
            <TableHead className="w-0" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {tickets.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={8}
                className="h-24 text-center text-muted-foreground"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            tickets.map((ticket) => (
              <TableRow
                key={ticket.id}
                onClick={() => navigate(`/tickets/${ticket.id}`)}
                className="cursor-pointer hover:bg-muted/50"
              >
                <TableCell className="font-medium">
                  <span className="inline-flex items-center gap-1.5">
                    {ticket.displayId}
                    {ticket.handoffFlag && (
                      <ArrowLeftRight
                        className="size-3.5 text-amber-600 dark:text-amber-500"
                        aria-label="Handed off"
                      />
                    )}
                  </span>
                </TableCell>
                <TableCell className="max-w-[22rem] truncate">
                  {ticket.subject}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {ticket.clinicName}
                </TableCell>
                <TableCell>
                  <Badge
                    variant="outline"
                    className={statusBadgeClass(ticket.status)}
                  >
                    {STATUS_LABELS[ticket.status]}
                  </Badge>
                </TableCell>
                <TableCell>
                  <Badge variant={priorityBadgeVariant(ticket.priority)}>
                    {PRIORITY_LABELS[ticket.priority]}
                  </Badge>
                </TableCell>
                <TableCell
                  className={
                    ticket.assigneeName
                      ? undefined
                      : 'text-muted-foreground italic'
                  }
                >
                  {ticket.assigneeName ?? 'Unassigned'}
                </TableCell>
                <TableCell className="text-right tabular-nums text-muted-foreground">
                  {formatAge(ticket.createdAt)}
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    asChild
                    variant="ghost"
                    size="sm"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <Link to={`/tickets/${ticket.id}`}>
                      View
                      <ChevronRight className="size-4" />
                    </Link>
                  </Button>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
