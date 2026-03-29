# Mailbox Routing Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `mailbox-routing`
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Defines the reusable workflow guidance for routing mailbox messages as part of a loop-owned step.

This ability is primarily documentation for the expected routing behavior and is intended to align with the `route_messages` code handler and related loop steps.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `sourceFolder` | Yes | Step arguments | Usually an `Outbox/` path |
| `logPath` | Yes | Step arguments | Usually `Docs/InvestigationLog.md` |
| Message front matter | Yes | Mailbox files | Used to resolve targets and routing metadata |
| Current docs | No | `Docs/` | Useful when routing results affect canonical records |

---

## Prompt Guidance

Treat mailbox routing as a deterministic workflow operation.

Rules:

- read message metadata before moving anything
- validate that the destination exists or can be resolved
- record what was routed, skipped, or failed
- when a consumed inbox message has been fully reflected in canonical docs, it should be deleted
- documentation remains the durable record; mailbox files are delivery surfaces

Preferred result keywords:

- `ROUTED` when one or more messages moved successfully
- `NO_MESSAGES` when there is nothing to do
- `ROUTING_FAILED` when a fatal routing problem blocks progress

---

## Output Contract

- Produces a routing summary suitable for `InvestigationLog.md`.
- Identifies each moved, skipped, or failed mailbox item.
- Makes it clear whether the workflow should continue, reassess, or stop.
- Aligns with the `route_messages` handler behavior documented in the proposals.

---

## Constraints

- Must not assume an always-on router service exists.
- Must not delete a message until its content is reflected in canonical docs when consumption is complete.
- Must not hide routing failures; they must be logged explicitly.
- Must not treat mailbox files as the long-term source of truth.

---

## Integration Notes

- Primary consumer: `routeMessages` code step or equivalent loop-owned routing step.
- May also inform future runbook routing behavior.
- Best paired with explicit step arguments for `sourceFolder` and `logPath`.
- This ability documents expected behavior even when the runtime implementation is still incomplete.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Mailbox routing is part of investigation workflow progression |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | `route_messages` is the primary code-handler example |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Open Questions

- None.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Align the future `route_messages` handler output with this ability contract | High | ?? Not Started | @developer | 2026-04-03 | Keep runtime and docs in sync |
| Validate consume-then-delete behavior against inbox processing rules | Medium | ?? Not Started | @developer | 2026-04-04 | Ensure canonical docs stay authoritative |

---

## Acceptance Criteria

- A reviewer can explain how routing works without assuming a separate router service.
- A loop author can use `mailbox-routing` to document what a routing step is expected to do.
- The ability remains consistent with the mailbox semantics in the current proposals.
