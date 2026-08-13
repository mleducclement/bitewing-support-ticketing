**Initial entries came from the requirements phase on the 10th of August 2026. Any newer entry will have a date in front of the item**

## Workflow

**A customer replying after expiry or resolution opens a new ticket.** Some line must be drawn, and this one is cheap to implement and easy to explain. The accepted cost, losing the thread between the old issue and the new one, is bought back by `related_ticket_id`.

**Expiry window is configurable, not hardcoded.** The client stated she would want to change it without calling. Correct window is domain-dependent; 14 days is a starting default.

**Single ownership.** Collision on shared tickets is the client's primary complaint; multiple owners would reintroduce it. Engineer collaboration was raised and the client confirmed it happens internally, outside the tool.

**Priority is agent-set, never customer-set.** Every customer experiences their own issue as urgent. A form field would arrive uniformly Urgent and the signal would be worthless.

**Priority is editable by any agent on any ticket, including unowned ones.** Restricting it to the assignee would mean priority isn't set until a ticket leaves the queue, which defeats its purpose, since the queue is exactly where ordering matters. An outage would sit indistinguishable from an invoice question until someone happened to open it.

**Downgrades are permitted rather than blocked.** Considered restricting downgrades to the team lead (unmanageable, requires reading every ticket) and making priority a one-way ratchet (rejected: the Urgent bucket would grow until it distinguished nothing, and mistaken escalations would be permanent). Free editing plus a logged reason makes misuse visible, which the client preferred to preventing it. The event log already answers the objection that a new owner wouldn't know a ticket had been downgraded before handoff.

**Handoff as a flag, not a status.** A released ticket is functionally identical to a new one, unowned and in the queue, so a separate status would duplicate `Open` while adding a transition. What the flag preserves is that the ticket is not untouched, so the next agent doesn't make the customer re-explain.

**Handoff independent of priority.** Considered giving handed-off tickets queue precedence. Rejected: bulk-unassigning an absent agent's thirty tickets would place all of them ahead of a clinic outage. Handoff means "not new," not "urgent."

**No automated reassignment on absence.** Considered load-balancing by agent workload. Rejected on two grounds: the team lead has strictly better information about who is available and who handles what, and agents receiving five silently-assigned tickets would resent it. The actual problem was tedium, not decision-making, bulk unassign removes the tedium and leaves the judgment with a human.

**Auto-cancel rather than auto-resolve.** Stale blocked tickets did not get solved. Counting them as resolved would corrupt the metric the client wants.

## Scope

**Web form intake rather than email ingestion.** Email ingestion requires mailbox polling or webhooks, reply threading, bounce and auto-responder handling, quoted-text stripping, and attachments, plausibly the entire build budget. The client accepted this with the explicit condition that email remain addable later, which the pluggable-intake design supports. Cost is that her customers have emailed the same address for years and she will run both channels during transition.

**Two roles.** Agents altering the expiry window or bulk-unassigning their own tickets is a permissions failure the client would immediately flag.

## AI Classification

**Two axes**. The classification is done along two axes, area and types. The reason for that decision is that accuracy would be higher on two smaller choices rather than one choice from a set of 18 different categories. This decision also mean that depending on the type, we can easily infer who is going to act on the ticket (Broken goes to engineer, How-To means the interface or docs failed and Feature Request goes to product).

**Every label records its source.** Model-authored and human-corrected labels are stored distinctly, along with the prompt version and model that produced them. Without this, a trend chart can't report how much of itself to trust, and accuracy figures recorded before a prompt change become uninterpretable. The decision was made because a chart the client acts on incorrectly is worse than no chart.

**Random spot-checks rather than universal confirmation.** Confirming every ticket produces better data and would be resented on every ticket. Every-Nth sampling risks correlating with queue regularity. Fixed random sampling is unbiased and imposes a tenth of the friction.

**Two separate accuracy measurements**. By using two different accuracy reading, one static and accurate and one dynamic but biased upward, we can monitor the drift signal by comparing the live number with the static one.

**Asynchronous Classification**. Background work runs on a hosted service polling a database work table rather than an external queue or job framework. At this scale the relevant design property is that classification is asynchronous and retryable. A broker/message queue would be incidental complexity.

## Infrastructure

**Single deployable rather than separate frontend and API.** ASP.NET Core serves the built React bundle from `wwwroot` and the API from the same origin. The alternative, frontend on a CDN, API deployed separately, is the more common option, but it requires CORS configuration, environment-specific API base URLs, and two deploy targets. At six users the former costs nothing and the simplicity is worth more. In development, Vite proxies `/api` to the API so that local behaviour matches production rather than requiring a CORS exception that only exists locally. The accepted cost is that a frontend-only change rebuilds and redeploys the whole application.

11th of August 2026 - **PostgreSQL rather than SQL Server.** While MSSQL is still common in enterprise .NET, the fact that Postgres is readily available on the chosen host (Render), steered the decision towards it. I would have considered Azure with MSSQL but for a project of this size, an easy to deploy solution was preferable. Postgres runs locally in Docker via `docker-compose.yml` while it's a managed instance on Render.

11th of August 2026 - **Migrations run automatically at startup.** `MigrateAsync` is called during application startup, so a deploy applies any pending migrations without manual intervention. This is not what a production system should do, a failed migration takes the application down on boot, and there is no review step between writing a migration and it running against live data. Running `dotnet ef database update` by hand against the deployed database is safer but easy to forget on a project where deploys are frequent. Automatic application was chosen because forgetting is the more likely failure.
