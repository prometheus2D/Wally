# Memory Summarization Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `memory-summarization`
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Defines reusable guidance for consolidating investigation progress into durable memory summaries that survive one-shot loop execution.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `Findings` | No | `Docs/Findings.md` | Evidence to condense |
| `Ideas` | No | `Docs/Ideas.md` | Alternatives worth carrying forward |
| `InvestigationLog` | No | `Docs/InvestigationLog.md` | Timeline of prior work |
| Existing memory files | No | `Memory/` | Prior summaries or notes |

---

## Prompt Guidance

- summarize only the information worth preserving across iterations
- prefer compact, high-signal notes over transcript-like restatements
- preserve unresolved blockers, important findings, and draft direction
- write summaries that help later steps reconstruct context quickly

---

## Output Contract

- Produces or updates memory summary files under `Memory/`.
- Distills high-value context without replacing canonical docs.
- Makes later prompt assembly lighter and more focused.

---

## Constraints

- Must not replace canonical docs with memory summaries.
- Must not copy raw outputs wholesale into memory without condensation.
- Must not hide important unresolved questions in private memory only.

---

## Integration Notes

- Primary consumer: `updateMemory` or similar summarization step.
- Best used after meaningful investigation progress, not after every trivial action.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Investigation workflow treats memory as private durable context |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Open Questions

- None.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Validate what minimum summary format should live in `Memory/` for v1 | Medium | ?? Not Started | @developer | 2026-04-04 | Keep summaries concise and durable |

---

## Acceptance Criteria

- The ability explains what belongs in durable memory summaries and what stays in canonical docs.
- A loop author can reference it without creating transcript-like memory files.
- The guidance supports one-shot prompt reconstruction rather than hidden state.