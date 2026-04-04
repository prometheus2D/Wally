# Memory Summarization Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `memory-summarization`
**Created**: 2026-03-29
**Last Updated**: 2026-04-02

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides reusable guidance for condensing durable investigation context into private memory artifacts without displacing canonical docs as the source of truth.

This ability is optional and not part of the streamlined default InvestigationLoop, but it remains useful for workflows that want a compact durable summary between runs.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `Findings` | No | `Findings.md` | Evidence to condense |
| `Ideas` | No | `Ideas.md` | Alternatives worth carrying forward |
| `ProposalDraft` | No | `ProposalDraft.md` | Current draft direction |
| Existing memory files | No | Durable memory docs | Prior summaries or notes |

---

## Prompt Guidance

- summarize only the context that should survive future one-shot iterations
- keep canonical docs authoritative; memory is a compact aid, not a replacement
- preserve decisions, open threads, and proposal direction that will matter on the next run
- remove transient details that can be reconstructed from the docs
- prefer compact, high-signal notes over transcript-like restatements

Preferred routing outcomes:

- `MEMORY_UPDATED` when the summary is refreshed and the workflow should reassess
- `COMPLETE` when the proposal is already ready to finalize

---

## Output Contract

- Produce durable summary content suitable for a loop-owned memory file.
- Highlight active decisions, unresolved blockers, and proposal direction.
- Keep the summary compact enough for future one-shot reuse.

---

## Constraints

- Must not replace canonical docs with memory summaries.
- Must not copy raw outputs wholesale into memory without condensation.
- Must not hide important unresolved questions in private memory only.

---

## Integration Notes

- Primary consumer: an `updateMemory` or similar summarization step.
- Best used after meaningful investigation progress, not after every trivial action.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Investigation workflow may keep private durable context, but canonical docs remain authoritative |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Acceptance Criteria

- The ability explains what belongs in durable memory summaries and what stays in canonical docs.
- A loop author can reference it without creating transcript-like memory files.
- The guidance supports one-shot prompt reconstruction rather than hidden state.