# Alternative Generation Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `alternative-generation`
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides reusable guidance for generating multiple credible approaches before the loop converges on one recommendation.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `Findings` | No | `Docs/Findings.md` | Evidence gathered so far |
| `OpenQuestions` | No | `Docs/OpenQuestions.md` | Helps separate blocked options from viable ones |
| `Ideas` | No | `Docs/Ideas.md` | Existing alternatives to refine or prune |
| `userPrompt` | Yes | Loop input | Original request |

---

## Prompt Guidance

- generate at least two materially different approaches when the state allows it
- explain tradeoffs, not just option names
- avoid fake variety where options collapse to the same implementation
- call out options that should be deferred or rejected
- prefer evidence-backed differences over speculative differences

---

## Output Contract

- Produces candidate approaches suitable for `Ideas.md`.
- Distinguishes recommended, deferred, and rejected options.
- May emit `ASK_USER`, `GATHER_CONTEXT`, or `DRAFT_PROPOSAL` style routing depending on the step.

---

## Constraints

- Must not pretend unresolved blockers are already settled.
- Must not generate alternatives unrelated to the documented request.
- Must not collapse recommendation and exploration into a single unsupported answer.

---

## Integration Notes

- Primary consumer: `generateIdeas` prompt step example in the executable-step proposal.
- Best combined with local step text that defines the output format expected by `Ideas.md`.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Investigation workflow expects idea generation before proposal convergence |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | Example prompt step already references `alternative-generation` |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Open Questions

- None.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Validate that `generateIdeas` step output maps cleanly into `Ideas.md` | Medium | ?? Not Started | @developer | 2026-04-03 | Keep loop example and ability guidance aligned |

---

## Acceptance Criteria

- The ability clearly separates alternative exploration from final recommendation.
- A loop author can reuse the guidance across more than one idea-generation step.
- The ability stays grounded in findings and open questions rather than freeform brainstorming.
