# Bitewing — v1 Specification

**Status:** Draft for review
**Scope:** v1 build. Deferred items listed explicitly in §8.

---

## 1. Context

Chairside sells cloud scheduling and practice management software to small dental clinics (1–4 dentists). Its support team is six agents plus a team lead, currently operating out of a shared Gmail inbox and a Google Sheet.

Three problems drive this build:

1. **No ownership.** Two agents answer the same customer; other tickets sit unclaimed for days.
2. **No visibility into status.** A ticket waiting on a customer reply is indistinguishable from a ticket being ignored.
3. **No aggregate view.** The team lead cannot answer "what are customers actually complaining about" without reading the inbox by hand.

v1 targets these three. It is not a Zendesk replacement and is explicitly scoped below the feature set of commercial helpdesk products.

---

## 2. Users and roles

Two roles in v1.

**Agent**

- Create tickets, view all tickets, claim unowned tickets
- Change status of tickets they own
- Add notes, change priority, correct classification
- Respond to spot-check confirmation prompts

**Team Lead**

- Everything an Agent can do
- Bulk unassign tickets from a departing/absent agent
- Change the stale-blocked expiry window
- View the trend dashboard

Customers are **not** users. They have no accounts and no login. They interact only through the public submission form.

---

## 3. Ticket lifecycle

### States

| State        | Meaning                                                                    |
| ------------ | -------------------------------------------------------------------------- |
| `Open`       | Unowned, in the queue, awaiting an agent                                   |
| `InProgress` | Owned by exactly one agent, actively being worked                          |
| `Blocked`    | Agent has responded; waiting on the customer                               |
| `Resolved`   | Issue addressed                                                            |
| `Cancelled`  | Closed without resolution (duplicate, spam, customer withdrew, or expired) |

`Resolved` and `Cancelled` are terminal. The split exists so that withdrawn and junk tickets do not inflate resolution metrics.

### Legal transitions

| From         | To           | Trigger                            | Side effects                                                    |
| ------------ | ------------ | ---------------------------------- | --------------------------------------------------------------- |
| `Open`       | `InProgress` | Agent claims ticket                | `assignee` set                                                  |
| `Open`       | `Cancelled`  | Agent/lead marks spam or duplicate | —                                                               |
| `InProgress` | `Blocked`    | Agent awaits customer reply        | `blocked_since` set                                             |
| `InProgress` | `Open`       | Agent releases ticket              | `assignee` cleared, `handoff_flag` set                          |
| `InProgress` | `Resolved`   | Agent resolves                     | —                                                               |
| `InProgress` | `Cancelled`  | Customer withdraws / duplicate     | —                                                               |
| `Blocked`    | `InProgress` | Customer replies                   | `blocked_since` cleared                                         |
| `Blocked`    | `Open`       | Agent releases ticket              | `assignee` cleared, `handoff_flag` set, `blocked_since` cleared |
| `Blocked`    | `Cancelled`  | Expiry window elapsed (automatic)  | `cancellation_reason = Expired`                                 |
| `Blocked`    | `Cancelled`  | Manual close                       | —                                                               |

Invariants:

- `InProgress` and `Blocked` require a non-null `assignee`.
- `Open` requires a null `assignee`.
- `handoff_flag` clears when a ticket transitions out of `Open` (i.e. when a new agent claims it). The reassignment itself is preserved in ticket history.
- Priority is independent of `handoff_flag`. A handed-off low-priority ticket does not jump the queue ahead of an urgent one.
- Any transition from `Blocked` is done manually by an agent when the customer replies to their emails (communication between customer and agent is still done outside the application for v1).

**No reopening.** `Resolved` and `Cancelled` are final. A customer returning on a closed issue results in a new ticket, which carries a nullable `related_ticket_id` pointing at the original so agents can read the prior history rather than asking the customer to re-explain.

---

## 4. Entities

### Ticket

| Field                                            | Notes                                                                       |
| ------------------------------------------------ | --------------------------------------------------------------------------- |
| `id`                                             |                                                                             |
| `subject`, `body`                                | From the submission form                                                    |
| `customer_name`, `customer_email`, `clinic_name` | From the submission form                                                    |
| `status`                                         | See §3                                                                      |
| `priority`                                       | `Low` / `Normal` / `Urgent`                                                 |
| `assignee_id`                                    | Nullable. Null if status is `Open` (FK to AspNetUsers.Id which is a string) |
| `handoff_flag`                                   | Boolean. Set on release, cleared on claim                                   |
| `blocked_since`                                  | Nullable timestamp. Drives auto-cancel                                      |
| `cancellation_reason`                            | Nullable: `Duplicate`, `Spam`, `Withdrawn`, `Expired`                       |
| `related_ticket_id`                              | Nullable. Set when a ticket succeeds a closed one                           |
| `created_at`, `updated_at`                       |                                                                             |

### Classification

Stored per ticket. Two independent axes (§6).

| Field            | Notes                                    |
| ---------------- | ---------------------------------------- |
| `area`           | Nullable — see failure handling          |
| `area_source`    | `Model` / `Human`                        |
| `type`           | Nullable                                 |
| `type_source`    | `Model` / `Human`                        |
| `prompt_version` | Which prompt produced the model's answer |
| `model_name`     |                                          |
| `classified_at`  | Nullable                                 |

`prompt_version` is non-negotiable: without it, accuracy figures recorded before a prompt change become uninterpretable, and this cannot be reconstructed retroactively.

### Note

Free-text, authored by an agent, attached to a ticket, timestamped. Internal only, never visible to customers. Used for handoff context and for an agent's own record of steps taken.

### TicketEvent

Append-only history: status changes, assignment changes, priority changes, classification corrections. Records actor and timestamp. This is what preserves "this ticket was handed off twice" after `handoff_flag` clears.

### SpotCheck

| Field                              | Notes                                                  |
| ---------------------------------- | ------------------------------------------------------ |
| `ticket_id`                        |                                                        |
| `agent_id`                         | Who was asked (FK to AspNetUsers.Id which is a string) |
| `area_confirmed`, `type_confirmed` | Boolean                                                |
| `responded_at`                     |                                                        |

### User

Provided by ASP.NET Core Identity. `ApplicationUser` extends `IdentityUser`, which
supplies `Id`, `Email`, `UserName`, `PasswordHash` and related fields.

| Field        | Notes                                  |
| ------------ | -------------------------------------- |
| `Id`         | From `IdentityUser`. String. FK target |
| `Email`      | From `IdentityUser`. Used for login    |
| `first_name` | Added on `ApplicationUser`             |
| `last_name`  | Added on `ApplicationUser`             |

Roles are handled by Identity's role system rather than a column on the user.
Two roles are seeded at startup: `Agent` and `TeamLead`. Endpoints are protected
with role-based authorisation attributes.

No self-registration. Accounts are created by seeding or by a Team Lead.

### Settings

Single row. Holds `blocked_expiry_days` (default 14). Editable by Team Lead, the client stated she would want to change this without developer involvement. While only the team lead can edit this value, it will still be visible to the agents

---

## 5. v1 features

**Intake.** Public web form: clinic name, contact name, email, subject, body. No authentication. Creates a ticket in `Open`.

**Queue.** List of tickets, filterable by status, priority, assignee, area. Sorted by priority then age. Handoff flag displayed as a visual marker, not as a sort key.

**Ticket detail.** Full text, status controls (legal transitions only), priority, assignee, notes, event history, classification with correction control.

**Claim / release.** Agent claims an `Open` ticket or releases one they own.

**Priority.** Set by agents only, never by the submission form. Any agent may change priority on any ticket, including unowned ones, so that queue triage happens before a ticket is claimed. Every change is recorded in the event log with actor and reason. Downgrades require a reason.

**Priority criteria.** `Urgent` is restricted to tickets where the clinic cannot see patients today, schedule inaccessible, bookings failing outright, or a full outage during office hours. Everything is `Normal` unless it meets that bar. `Low` is rare and explicit: feature requests, and how-to questions the customer has stated aren't time-sensitive. Time of day is deliberately not encoded; whether a Friday-evening report is urgent stays an agent judgment.

**Bulk unassign.** Team Lead filters by assignee, selects many, returns them to `Open` with `handoff_flag` set. Priority is preserved unchanged.

**Auto-cancel.** Background job cancels `Blocked` tickets whose `blocked_since` exceeds `blocked_expiry_days`, with `cancellation_reason = Expired`.

**Classification.** Asynchronous. On creation, a row is written to a work table; a background worker picks it up, calls the model, writes `area` and `type`. Ticket creation never blocks on the model and never fails because of it.

**Spot-check confirmation.** Each ticket has a fixed random probability (10% in v1) of prompting its agent to confirm area and type on resolution. Randomised rather than every-Nth to avoid systematic bias in which tickets get sampled.

**Trend dashboard** (Team Lead). Ticket counts by area and by type, shown as separate breakdowns rather than a 7×3 cross-tab. Displays alongside: count of unclassified tickets, live spot-check agreement rate, and the date and figure of the last controlled accuracy measurement.

---

## 6. Classification design

### Two axes, not one

**Area**: where in the product (`Claims`, `Booking & Calendar`, `Reminders`, `Access & Accounts`, `Subscription & Invoicing`, `Other`).

**Type**: what kind of request (`Broken`, `HowTo`, `FeatureRequest`).

The model makes two independent choices (one of six, one of three), not one choice among eighteen. Combining them into a single flat list would force the model to arbitrarily pick an axis per ticket and would degrade accuracy.

Notes on specific categories:

- `Subscription & Invoicing` is named explicitly rather than "Billing," because in a dental context "billing" ambiguously refers to Chairside's invoices, the clinic's patient billing, and insurance claims.
- `Access & Accounts` rather than "Login," because the client's described volume driver is user management during staff turnover, not password resets.
- The patient portal is folded into `Booking & Calendar` for v1. Its primary function is appointment request/confirmation, so a separate category would produce a boundary the model flip-flops on. Split it out later if the data justifies it.
- `Broken` vs `HowTo` is the hardest boundary and the most decision-relevant one, a customer reports the same symptom whether the product is broken or they can't find the button. Expect this to score worst. It is worth measuring honestly rather than hiding.
- `Other` is monitored. A rising `Other` rate signals the list is incomplete; it does not reveal what is missing. Identifying that requires reading tickets by hand in v1.

### Failure handling

`area` and `type` are nullable and an unclassified ticket is a normal, valid state, not an error. If the model call times out, is rate-limited, or returns malformed output, the ticket remains unclassified and is retried a bounded number of times (3 times at most, no backoffs at the moment). Agents can classify manually at any point.

### Measurement

Two distinct figures, reported separately and never averaged:

**Controlled accuracy (point-in-time).** Measured against a sealed holdout set, once per prompt revision. Trustworthy. Attached to a specific `prompt_version`.

**Spot-check agreement rate (live).** Fraction of sampled tickets where the agent confirmed the model's label. Updates continuously. Biased upward, agents catch obvious errors and overlook subtle ones, so it is a drift signal, not a true accuracy figure. A sustained drop triggers a re-calibration cycle.

Area and Type are scored separately.

### Development corpus (bootstrap only)

No real ticket data exists at build time.

- ~300 LLM-generated tickets, labelled at generation. Split: ~200 **working set** (used for prompt iteration), ~100 **holdout** (sealed until iteration is complete).
- ~50 **hand-written tickets**, authored to reflect realistic mess, one-liners, buried problems, multiple issues per ticket, venting. Second sealed holdout.

Both holdouts are single-use. If a holdout is run, its failures inspected, and the prompt adjusted, it has become part of the working set and a fresh one must be generated.

Two figures are reported: accuracy on the generated holdout, and on the hand-written holdout. The gap between them is the meaningful result, generated tickets are systematically cleaner than real ones, and a small gap would itself be suspicious.

The generation script, its parameters, and its output are version-controlled. This corpus is a bootstrap and is retired once real tickets accumulate.

---

## 7. Non-functional

- Deployed to a public URL from week one, before feature work begins.
- Stack: ASP.NET Core API + React frontend.
- UI is deliberately utilitarian. This is an internal tool for six agents.
- Identity is used to handle authentication and authorization.

---

## 8. Explicitly out of scope for v1

Listed so that each can be declined by pointing at this section.

**Deferred to v2:**

- Email intake. Architecturally anticipated, intake is pluggable and the form is one source, but not built.
- In-app messaging between agents
- Bulk reassign to a named agent (bulk *unassign* is in v1 and solves the stated tedium)
- AI-suggested replies
- Emergent-topic clustering for category discovery
- Per-category adaptive sampling rates and automated drift monitoring
- File attachments on the submission form

**Not planned:**

- SLA tracking and escalation chains
- Multi-tenancy
- Canned responses
- Free-text tagging
- Customer-facing ticket portal or status page
- Multi-agent ticket ownership, the client confirmed engineer collaboration stays outside the app
- Email or push notifications
- Load-balancing assignment algorithms

---

## 9. Open items

- Whether spot-check prompts should be capped per agent per day, the client raised uneven random distribution and it was not resolved
