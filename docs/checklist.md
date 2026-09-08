# Bitewing v1 — Definition of Done

Derived from `docs/spec.md` §3–§8 and `docs/decision-log.md`. Update as work lands; this is a working checklist, not a spec — spec.md remains the source of truth for behavior.

## Foundation

- [x] Entities: Ticket, Classification, Note, TicketEvent, SpotCheck, Settings, ApplicationUser
- [x] Migrations applied locally and on Render
- [x] Identity: cookie auth, AgentAccess/TeamLeadOnly policies, config-driven seeding
- [x] Demo ticket seeding: ~12 tickets across every status/priority, guarded by an empty-table check, fixed GUIDs, integration-tested for idempotency
- [x] API responses serialize enums as names via `JsonStringEnumConverter`
- [x] Ticket creation endpoint (`POST /api/tickets`) — validated, integration-tested against real Postgres
- [x] CI running on push (GitHub Actions): `dotnet` test job plus a `web` job (npm lint + Vite build)
- [x] CD deploying on push (Render), gated on CI: Render's "wait for CI to pass" setting holds the deploy until the commit's checks are green

## Ticket lifecycle (spec §3)

Transition logic lives in `TicketService`. Most transitions are validated via `TicketTransitions.IsLegal`; Claim and Unblock additionally guard their source state directly, since both target `InProgress` and the table alone can't tell the two actions apart.

- [x] Claim: Open → InProgress (tested; concurrency race accepted as v1 risk; writes a `StatusChanged` TicketEvent)
- [x] Release: InProgress/Blocked → Open (tested from both source states; sets `handoff_flag`, clears `assignee_id`; also clears `blocked_since` when releasing from Blocked; restricted to the ticket's assignee or a TeamLead; writes a `StatusChanged` TicketEvent with from/to status)
- [x] Block: InProgress → Blocked (sets `blocked_since`; restricted to the ticket's assignee or a TeamLead; writes a `StatusChanged` TicketEvent with from/to status)
- [x] Unblock: Blocked → InProgress (manual, customer replied outside the app; restricted to the ticket's assignee or a TeamLead; writes a `StatusChanged` TicketEvent with from/to status)
- [x] Resolve: InProgress → Resolved (restricted to the ticket's assignee or a TeamLead; writes a `StatusChanged` TicketEvent with from/to status)
- [x] Cancel: Open/InProgress/Blocked → Cancelled (manual — duplicate/spam/withdrawn; ownership enforced except when cancelling an unowned Open ticket; clears handoff_flag and blocked_since; writes a `StatusChanged` TicketEvent with from/to status and the reason)
- [x] Auto-cancel: Blocked → Cancelled once `blocked_since` exceeds `Settings.blocked_expiry_days` (background job, checked hourly by default — configurable via `AutoCancel:CheckIntervalMinutes`; `actor_id` null on the event, per spec §4)
- [x] TicketEvent written on every transition above — pattern established on Claim (`TicketService.AddEvent`, one combined `StatusChanged` row per transition)

## Ticket operations (spec §5)

- [x] Queue endpoint (`GET /api/tickets`) — filterable by status/priority/assignee/handoff_flag, sorted priority-then-age; area filter deferred until classification worker exists
- [~] Ticket detail endpoint (`GET /api/tickets/{id}`) — returns the flat `TicketResponse`, now including `updatedAt`; event history exposed via `GET /api/tickets/{id}/events` and notes via `GET /api/tickets/{id}/notes` (both newest-first, actor/author flattened to a name); classification still to be added as a later slice
- [x] Priority change endpoint (`POST /api/tickets/{id}/priority`) — any agent, any ticket including unowned; downgrade requires a reason; writes a `PriorityChanged` event with from/to
- [ ] Bulk unassign endpoint — TeamLead only, returns tickets to Open with `handoff_flag` set, priority untouched
- [x] Add note endpoint (`POST /api/tickets/{id}/notes`, any agent, any ticket; `GET /api/tickets/{id}/notes` newest-first, author flattened to a name) — append-only, writes no TicketEvent

## AI classification (spec §6)

- [~] Work table + background worker (async, retryable, capped attempts) — `ClassificationJob` table and the enqueue-on-create are done (one Pending row per ticket, written in the ticket-creation transaction); the polling worker is slice 2
- [ ] `IClassifier` seam in place (`ClassifyTicket` + `ClassificationResult`); `ClaudeClassifier` implementation is slice 2
- [ ] LLM integration for area + type classification
- [ ] `area_source` / `type_source` tracked, `prompt_version` recorded
- [ ] Spot-check confirmation flow (fixed random probability, agent confirms on resolution)
- [ ] Trend dashboard — area/type breakdowns, unclassified count, live agreement rate, last controlled accuracy figure

## Evaluation (spec §6)

- [ ] Generated corpus (~300 tickets) — working set + sealed holdout, script version-controlled
- [ ] Hand-written holdout (~50 tickets, deliberately messy)
- [ ] Controlled accuracy measurement against both holdouts, per prompt version
- [ ] Results recorded in README (currently placeholders)

## Frontend

React + Vite + Tailwind v4 + shadcn/ui. Feature-folder layout under `src/web/src/features`.

- [x] App shell, Tailwind/shadcn toolchain
- [x] API failure handling: `apiFetch` normalizes network errors and 4xx/5xx bodies, times out stalled requests (15s), auto-toasts every failure except 401 (sonner), full-screen "can't reach Bitewing" state when the session check can't reach the server, root error boundary
- [x] Login / logout: session check on load (`GET /api/auth/me`), login form, sign-out in the header
- [x] Client-side routing (`react-router-dom`): `/` queue, `/tickets/CS-{n}` detail (friendly reference, not the GUID; backend `GET /api/tickets/CS-{number}` alias); queue rows are clickable and carry a "View" link
- [~] Queue view: live `GET /api/tickets`, colored status badges, priority, age, handoff marker, client-side status/priority/assignee filters. Server-side filtering and a card layout option are still open.
- [~] Ticket detail: sectioned into Status / Customer / Activity / Message / History cards. Read-only view, status controls (legal transitions only, greyed out when the viewer is neither owner nor TeamLead), priority editing, the Notes section (compose box + newest-first list, internal only), and the History timeline (newest-first, captioned status/priority badge pairs, actor + relative time) are done. Classification is a later slice.
- [ ] Public intake form
- [ ] Trend dashboard
- [ ] Frontend visual pass — type scale, spacing, and card treatment applied consistently across queue + detail + intake form + dashboard (detail page had a light readability pass; the rest is deferred so it's done coherently)

## Infrastructure follow-ups

- [x] CI/CD gating — Render's "wait for CI to pass" setting blocks the deploy unless the commit's GitHub checks (`test` + `web`) are green
- [ ] Decision log: concurrency risk on Claim accepted for v1 — not yet written up
- [ ] Decision log: AuthController vs MapIdentityApi — still outstanding from last week

## Explicitly out of scope for v1 (spec §8)

Not tracked as "remaining" — listed so nothing here gets mistaken for missing work:
email intake, in-app messaging, bulk reassign-to-named-agent, AI-suggested replies, topic clustering, adaptive sampling, file attachments, SLA tracking, multi-tenancy, canned responses, tagging, customer-facing portal, multi-agent ownership, notifications, load-balancing assignment.
