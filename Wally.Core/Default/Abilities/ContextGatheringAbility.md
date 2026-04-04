# Context Gathering Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `context-gathering`
**Created**: 2026-03-29
**Last Updated**: 2026-04-02

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Defines reusable guidance for gathering repository, document, command, and workspace context needed to advance an investigation.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `userPrompt` | Yes | Loop input | Original request |
| `LoopState` | No | Current loop docs | Current goals, open questions, and acceptance criteria |
| Existing findings | No | `Findings.md` | Helps avoid repeated collection |
| Workspace structure | No | WorkSource | Used to locate relevant files and systems |

---

## Prompt Guidance

- gather only the context needed to reduce a current uncertainty
- prefer authoritative files and directly relevant code over broad scans
- summarize gathered evidence into canonical docs
- distinguish facts from guesses
- stop gathering once the next decision can be made responsibly

---

## Output Contract

- Produce evidence summaries suitable for `Findings.md`.
- Identify which open questions were answered by the gathered context.
- Support routing toward reassessment, user questions, or drafting.

---

## Constraints

- Must not gather context aimlessly with no decision target.
- Must not leave important command or file findings only in transient output.
- Must not overfit to one file when the question spans multiple systems.

---

## Integration Notes

- Useful for prompt, shell, command, and code steps.
- Often precedes `investigation-assessment` or `alternative-generation`.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Investigation workflow relies on evidence gathering |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Acceptance Criteria

- The ability gives loop authors a reusable way to frame evidence gathering.
- The guidance prevents context collection from drifting into undocumented exploration.
- Outputs clearly map into canonical findings documentation.
