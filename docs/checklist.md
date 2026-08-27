# Bitewing v1 — Definition of Done

Derived from `docs/spec.md` §3–§8 and `docs/decision-log.md`. Update as work lands; this is a working checklist, not a spec — spec.md remains the source of truth for behavior.

## Foundation

- [x] Entities: Ticket, Classification, Note, TicketEvent, SpotCheck, Settings, ApplicationUser
- [x] Migrations applied locally and on Render
- [x] Identity: cookie auth, AgentAccess/TeamLeadOnly policies, config-driven seeding
- [x] Ticket creation endpoint (`POST /api/tickets`) — validated, integration-tested against real Postgres
- [x] CI running tests on push (GitHub Actions)
- [x] CD deploying on push (Render)

## Ticket lifecycle (spec §3)

Transition logic lives in `TicketService`, validated centrally via `TicketTransitions.IsLegal`.

- [x] Claim: Open → InProgress (tested; concurrency race accepted as v1 risk; writes a `StatusChanged` TicketEvent)
- [ ] Release: InProgress/Blocked → Open (sets `handoff_flag`, clears `assignee_id`)
- [ ] Block: InProgress → Blocked (sets `blocked_since`)
- [ ] Unblock: Blocked → InProgress (manual, customer replied outside the app)
- [ ] Resolve: InProgress → Resolved
- [ ] Cancel: Open/InProgress/Blocked → Cancelled (manual — duplicate/spam/withdrawn)
- [ ] Auto-cancel: Blocked → Cancelled once `blocked_since` exceeds `Settings.blocked_expiry_days` (background job)
- [ ] TicketEvent written on every transition above — pattern established on Claim (`TicketService.AddEvent`, one combined `StatusChanged` row per transition); remaining transitions still need it

## Ticket operations (spec §5)

- [x] Queue endpoint (`GET /api/tickets`) — filterable by status/priority/assignee/handoff_flag, sorted priority-then-age; area filter deferred until classification worker exists
- [ ] Ticket detail endpoint — full ticket + notes + event history + classification
- [ ] Priority change endpoint — any agent, any ticket including unowned; downgrade requires a reason; writes event
- [ ] Bulk unassign endpoint — TeamLead only, returns tickets to Open with `handoff_flag` set, priority untouched
- [ ] Add note endpoint

## AI classification (spec §6)

- [ ] Work table + background worker (async, retryable, capped attempts)
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

- [ ] Everything — no React work started yet. Login/logout, queue view, ticket detail, intake form, trend dashboard.

## Infrastructure follow-ups

- [ ] CI/CD gating — tests currently run but don't block Render deploy; wire via Render deploy hook
- [ ] Decision log: concurrency risk on Claim accepted for v1 — not yet written up
- [ ] Decision log: AuthController vs MapIdentityApi — still outstanding from last week

## Explicitly out of scope for v1 (spec §8)

Not tracked as "remaining" — listed so nothing here gets mistaken for missing work:
email intake, in-app messaging, bulk reassign-to-named-agent, AI-suggested replies, topic clustering, adaptive sampling, file attachments, SLA tracking, multi-tenancy, canned responses, tagging, customer-facing portal, multi-agent ownership, notifications, load-balancing assignment.
