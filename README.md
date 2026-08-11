# Bitewing - support ticketing

A lightweight support ticketing system, built solo as a portfolio project.

The client AND data are fictional. The requirements are not invented in one sitting, they were developed through a simulated requirements-gathering conversation, then written up as a specification before any code was written. Both the spec and the reasoning behind each decision are in this repository.

## Stack

- **Backend:** ASP.NET Core
- **Frontend:** React, Shadcn/ui
- **Database:** Postgres
- **Hosting:** Render, deployed continuously from `main`
- **LLM**: Claude (Anthropic API)

## Links

**[Live demo](https://bitewing-support-ticketing.onrender.com)**

**[Specification](docs/spec.md)** 

**[Decision log](docs/decision-log.md)**

---

## The problem

Chairside sells cloud scheduling and practice management software to small dental clinics (1–4 dentists). Its product handles booking, automated patient reminders, insurance claim submission, patient records, recall lists, and a small patient-facing portal.

Chairside's own support team, six agents and a team lead, runs out of a shared Gmail inbox and a Google Sheet. Three problems follow from that:

- **No ownership.** Two agents reply to the same customer; other tickets sit unclaimed for days.
- **No status.** A ticket waiting on a customer reply looks identical to one being ignored.
- **No aggregate view.** The team lead can't answer "what are customers actually complaining about" without reading the inbox by hand.

Commercial helpdesk products were rejected as too expensive and far larger than the need. The goal is a small tool that solves those three problems and ships quickly, with a second phase planned.

---

## What v1 does

**Primary feature: Ticket lifecycle.** 

Five states. Open, In Progress, Blocked, Resolved, Cancelled, with an explicit set of legal transitions. Blocked distinguishes "waiting on the customer" from "being ignored." Cancelled is separate from Resolved so that withdrawn, duplicate, and expired tickets don't inflate resolution numbers.

**Other features**

- Single Ownership: Tickets are assigned to one agent only.
- Priority: Set by agents against strict criteria, never by customers.
- Bulk Unassign: Team lead can return an absent agent's tickets to the queue in one action.
- Auto-Cancel: Tickets that are left without a customer reply for a long time eventually expire.
- Notes: Internal notes on a ticket, not visible to the customer.
- Event History: Every classification, priority or assignment change is recorded in the ticket history.
- Two Roles: Agents and Team Lead have different permissions.

---

## AI classification

Incoming tickets are classified by an LLM along two independent axes:

- **Area**: where in the product (Claims, Booking & Calendar, Reminders, Access & Accounts, Subscription & Invoicing, Other)
- **Type**: what kind of request (Broken, How-to, Feature Request)

Two axes rather than one flat list of eighteen. The model makes two small choices instead of one large one, and the result maps onto who acts: Broken goes to engineering, How-to means the interface or documentation failed, Feature Request goes to product.

The call runs asynchronously via a work table and a background worker.

Model-authored and human-corrected labels are tracked separately.

### How accuracy is measured

Prompts were developed against one set of tickets and accuracy measured against a second set that was never looked at during development. The reason for that is that using that set to calibrate the prompt would be grading yourself on the answers you saw. 

There are no real tickets, so the corpus is synthetic: LLM-generated tickets split into a working set and a sealed holdout, plus a smaller set of tickets hand-written to reflect realistic mess such as one-liners, buried problems, multiple issues in one message, low signal-to-noise. The hand-written set exists because generated tickets are systematically cleaner than real ones and will flatter the model. Accuracy is reported on both, and **the gap between them is the more informative number.**

Agents are occasionally asked to confirm a label, at random, which gives a running signal that the model hasn't drifted, though agents catch obvious errors and miss subtle ones, so the signal reads higher than real accuracy.

Both figures appear on the dashboard, labelled, alongside the count of unclassified tickets.

### Results

|                      | Area          | Type          |
| -------------------- | ------------- | ------------- |
| Generated holdout    | *placeholder* | *placeholder* |
| Hand-written holdout | *placeholder* | *placeholder* |

*Prompt version: placeholder · Model: placeholder · Measured: placeholder*

Notes on failure modes: *placeholder expected to be worst on Broken vs. How-to, since a customer reports the same symptom whether the product is broken or they can't find the button.*
