**Initial entries came from the requirements phase on the 10th of August 2026. Any newer entry will have a date in front of the item**

## Workflow

**A customer replying after expiry or resolution opens a new ticket.** This one is cheap to implement and easy to explain. The accepted cost is losing the thread between the old issue and the new one but it is bought back by `related_ticket_id`.

**Expiry window is configurable, not hardcoded.** The client stated she would want to change it without calling. Correct window is domain-dependent. 14 days is a starting default.

**Single ownership.** Collision on shared tickets is the client's primary complaint. Multiple owners would reintroduce it. Engineer collaboration was raised and the client confirmed it happens internally, outside the tool.

**Priority is agent-set, never customer-set.** Every customer experiences their own issue as urgent. A form field would allow users to mark everything as urgent and the signal would be worthless.

**Priority is editable by any agent on any ticket, including unowned ones.** Restricting it to the assignee would mean priority isn't set until a ticket leaves the queue, which defeats its purpose, since the queue is exactly where ordering matters. An outage would sit indistinguishable from an invoice question until someone happened to open it.

**Downgrades are permitted rather than blocked.** Considered restricting downgrades to the team lead (unmanageable, requires reading every ticket) and making priority one-way (The problem is that the urgent bucket would grow until it distinguished nothing, and mistaken escalations would be permanent). Free editing plus a logged reason makes misuse visible, which the client preferred to preventing it. The event log already answers the objection that a new owner wouldn't know a ticket had been downgraded before handoff.

**Handoff as a flag, not a status.** A released ticket is functionally identical to a new one, unowned and in the queue, so a separate status would duplicate `Open` while adding a transition. What the flag preserves is that the ticket is not untouched, so the next agent doesn't make the customer re-explain.

**Handoff independent of priority.** Considered giving handed-off tickets queue precedence, but bulk-unassigning an absent agent's thirty tickets would place all of them ahead of a clinic outage. Handoff means "not new," not "urgent."

**No automated reassignment on absence.** Considered load-balancing by agent workload. Rejected on two grounds. 1. The team lead has strictly better information about who is available and who handles what, and 2. agents receiving five silently-assigned tickets would resent it. The actual problem was tedium, not decision-making, bulk unassign removes the tedium and leaves the judgment with a human.

**Auto-cancel rather than auto-resolve.** Long-term blocked tickets did not get solved. Counting them as resolved would corrupt the metric the client wants.

27th of August 2026 - **`TicketEvent` records from/to status for `StatusChanged` events.** The original shape (event type, actor, timestamp) couldn't answer what a status change actually was — a ticket with several transitions in its history was unreadable from the event log alone. Added nullable `FromStatus`/`ToStatus`, populated only for `StatusChanged`. Retrofitted onto Claim and Release when Block was added, since all three share the same event-writing code path.

27th of August 2026 - **Status transitions restricted to the ticket's assignee, with a TeamLead override.** Spec §2 says an Agent may "change status of tickets they own," which Claim and Release (already shipped) didn't enforce. Added the check starting with Block and retrofitted it onto Release; Claim is exempt since the ticket has no owner yet at that point. TeamLead bypasses the check per §2's "everything an Agent can do."

27th of August 2026 - **Claim and Unblock don't rely solely on `TicketTransitions.IsLegal`.** Both target `InProgress`, and the table's flat `(from, to)` pairs can't distinguish which action is being attempted from a shared target — `IsLegal(Open, InProgress)` and `IsLegal(Blocked, InProgress)` are both true. Claim explicitly rejects a ticket that's already `InProgress`/`Blocked` (`TicketAlreadyClaimedException`) before consulting the table; Unblock requires `Status == Blocked` directly rather than calling `IsLegal` at all. Every other transition has a unique target status, so this doesn't recur elsewhere in v1.

27th of August 2026 - **Added a human-readable `ticket_number`, displayed as `CS-{n}`.** `id` is a GUID and unusable for the real workflow: an agent references a ticket to a clinic over email, the clinic cites it back weeks later when the issue recurs, and a new agent needs to recognize it as the same case to set `related_ticket_id`. DB-generated sequential integer, unique-indexed; formatting (`CS-` prefix) lives on the entity as a computed `DisplayId` so there's one source of truth. Looking a ticket up *by* its display number is future work — no search/detail UI exists yet to use it.

27th of August 2026 - **Queue filterable by `handoff_flag`, ahead of spec.** Spec §5 lists status/priority/assignee/area as queue filters; `handoff_flag` isn't among them. Added so an agent picking up extra tickets can surface handoffs first, or handoff-only, rather than let them sit in the queue behind other `Open` tickets. Area filter is deferred until the classification worker exists (§6).

## Scope

**Web form intake rather than email ingestion.** Email ingestion requires mailbox polling or webhooks, reply threading, bounce and auto-responder handling, quoted-text stripping, and attachments, probably the entire build budget. The client accepted this with the explicit condition that email remain addable later, which the pluggable-intake design supports. Cost is that her customers have emailed the same address for years and she will run both channels during transition.

**Two roles.** Agents altering the expiry window or bulk-unassigning their own tickets is a permissions failure the client would immediately flag.

12th of August 2026 - **Policies rather than role attributes**. A TeamLead should be able to do everything an Agent can. The alternative was to assign both roles to leads (which runs the risk of a failure if a lead is created without both roles) or writing `[Authorize(Roles = "Agent,TeamLead")]` everywhere, which would have been tedious and risky if the line is omitted on even one protected route. Policies state the hierarchy once: `AgentAccess` passes for either role, `TeamLeadOnly` for leads. Endpoints reference intent rather than role names, and adding a third role later means editing in one place.

12th of August 2026 - **No self-registration**. The spec says accounts come from seeding or a team lead. A support tool with public registration as endpoint would mean anyone who finds the URL could create an account, which is not desirable.

## AI Classification

**Two axes**. The classification is done along two axes, area and type. The reason for that decision is that accuracy would be higher on two smaller choices rather than one choice from a flat list of 18 different categories. This decision also means that depending on the type, we can easily infer who is going to act on the ticket (Broken goes to engineer, How-To means the interface or docs failed and Feature Request goes to product).

**Every label records its source.** Model-authored and human-corrected labels are stored distinctly, along with the prompt version and model that produced them. Without this, a trend chart can't report how much of itself to trust, and accuracy figures recorded before a prompt change become uninterpretable. The decision was made because a chart the client acts on incorrectly is worse than no chart.

**Random spot-checks rather than universal confirmation.** Confirming every ticket produces better data, but would be resented on every ticket. Every-Nth sampling risks correlating with queue regularity. Fixed random sampling is unbiased and imposes a tenth of the friction.

**Two separate accuracy measurements**. By using two different accuracy readings, one static and accurate (controlled accuracy measurement via a holdout set) and one dynamic (spot-checks) but biased upward, we can detect drift by comparing the live number against the static one.

**Asynchronous Classification**. Background work runs on a hosted service polling a database work table rather than an external queue or job framework. At this scale the relevant design property is that classification is asynchronous and retryable. A broker/message queue would be incidental complexity.

## Infrastructure

11th of August 2026 - **Single deployable rather than separate frontend and API.** ASP.NET Core serves the built React bundle from `wwwroot` and the API from the same origin. The alternative, frontend on a CDN, API deployed separately, is the more common option, but it requires CORS configuration, environment-specific API base URLs, and two deploy targets. At six users the former costs nothing and the simplicity is worth more. In development, Vite proxies `/api` to the API so that local behaviour matches production rather than requiring a CORS exception that only exists locally. The accepted cost is that a frontend-only change rebuilds and redeploys the whole application.

11th of August 2026 - **PostgreSQL rather than SQL Server.** While MSSQL is still common in enterprise .NET, the fact that Postgres is readily available on the chosen host (Render), steered the decision towards it. I would have considered Azure with MSSQL but for a project of this size, an easy to deploy solution was preferable. Postgres runs locally in Docker via `docker-compose.yml` while it's a managed instance on Render.

11th of August 2026 - **Migrations run automatically at startup.** `MigrateAsync` is called during application startup, so a deploy applies any pending migrations without manual intervention. This is not what a production system should do, a failed migration takes the application down on boot, and there is no review step between writing a migration and it running against live data. Running `dotnet ef database update` by hand against the deployed database is safer but easy to forget on a project where deploys are frequent. Automatic application was chosen because forgetting is the more likely failure.

12th of August 2026 - **Cookie auth rather than JWT**. Since the frontend and API are served from the same origin, we can forego the JWT option. Plus it gives us a real logout without having to code server-side revocation of a JWT. The only cost on this project is CSRF, which is handled by `SameSite=Lax`. Antiforgery tokens are the fuller answer and are deferred at the moment.

12th of August 2026 - **Users and roles seeded from configuration**. Accounts created by hand disappear when the database is recreated (which matters on Render's free tier since the Postgres database expires after 30 days). Config-driven seeding makes the environment easy to reproduce, and adding a user is a config edit rather than code change. The same password is used for every user (weak locally, strong variant in production). If we had used random passwords for each user, there would have been no way to easily log into the demo.

TODO: Add decision for `AuthController` rather than `MapIdentityApi`
