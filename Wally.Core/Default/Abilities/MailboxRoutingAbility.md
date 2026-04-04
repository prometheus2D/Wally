# Mailbox Routing Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `mailbox-routing`
**Created**: 2026-03-29
**Last Updated**: 2026-04-02

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Defines reusable workflow guidance for routing mailbox messages as part of a loop-owned step.

This ability is not part of the streamlined default InvestigationLoop, but it remains available for mailbox-enabled workflows that need explicit delivery behavior.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `sourceFolder` | Yes | Step arguments | Usually an `Outbox/` path |
| `logPath` | No | Step arguments | Optional persisted routing summary |
| Message front matter | Yes | Mailbox files | Used to resolve targets and routing metadata |

---

## Prompt Guidance

Treat mailbox routing as a deterministic workflow operation.

Rules:

- read message metadata before moving anything
- resolve the `to` recipient from message front matter
- move the file from the sender's `Outbox/` to the recipient's `Inbox/`
- report what routed, skipped, or failed
- keep mailbox behavior narrow; richer workflow decisions belong in loop steps and docs

Preferred result keywords:

- `ROUTED` when one or more messages moved successfully
- `NO_MESSAGES` when there is nothing to do
- `ROUTING_FAILED` when a fatal routing problem blocks progress

---

## Output Contract

- Produce a routing summary that can be written to a loop-owned log file when requested.
- Identify each moved, skipped, or failed mailbox item.
- Make it clear whether the workflow should continue, reassess, or stop.
- Align with the `route_messages` handler behavior documented in the proposals.

---

## Constraints

- Must not assume an always-on router service exists.
- Must not grow into a general mailbox orchestration subsystem.
- Must not hide routing failures; they must be visible in handler output.
- Must not treat mailbox files as the long-term source of truth.

---

## Integration Notes

- Primary consumer: a `routeMessages` code step or equivalent loop-owned routing step.
- Steps and loops reference this guidance via `abilityRefs`, for example `"abilityRefs": ["mailbox-routing"]`.
- Best paired with explicit step arguments for `sourceFolder` and optional `logPath`.
- The runtime implementation should remain a thin recipient-resolution and file-move helper.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Mailbox routing is available for richer workflows even though the default loop no longer depends on it |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | `route_messages` is the primary code-handler example |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Acceptance Criteria

- A reviewer can explain how routing works without assuming a separate router service.
- A loop author can use `mailbox-routing` to document what a routing step is expected to do.
- The ability remains consistent with the mailbox semantics in the current proposals.
