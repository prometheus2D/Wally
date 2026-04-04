# Alternative Generation Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `alternative-generation`
**Created**: 2026-03-29
**Last Updated**: 2026-04-02

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides reusable guidance for generating materially different candidate approaches before a loop converges on one recommendation.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `userPrompt` | Yes | Loop input | Original request |
| `LoopState` | No | Current loop docs | Current goals, open questions, and acceptance criteria |
| `Findings` | No | `Findings.md` | Evidence gathered so far |
| `Ideas` | No | `Ideas.md` | Existing alternatives to refine or prune |

---

## Prompt Guidance

- generate options that are meaningfully different, not cosmetic variants
- distinguish recommended, deferred, and rejected alternatives explicitly
- tie each option back to current findings, constraints, and open questions
- surface tradeoffs and unknowns instead of hiding them
- prefer evidence-backed differences over speculative differences
- emit exactly one routing keyword for the next investigation step

Preferred routing outcomes:

- `REQUEST_USER_INPUT` when option selection depends on missing user constraints or preferences
- `GATHER_CONTEXT` when a candidate depends on evidence not yet collected
- `DRAFT_PROPOSAL` when one option is ready to carry into a proposal draft

---

## Output Contract

- Produce option summaries suitable for `Ideas.md`.
- Preserve traceability between findings and proposed alternatives.
- Make it clear which option is recommended, deferred, or rejected.

---

## Constraints

- Must not pretend unresolved blockers are already settled.
- Must not generate alternatives unrelated to the documented request.
- Must not collapse recommendation and exploration into a single unsupported answer.

---

## Integration Notes

- Primary consumer: the `generateIdeas` prompt step in executable-step loops.
- Best combined with local step text that defines the exact markdown structure expected by `Ideas.md`.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Investigation workflow expects idea generation before proposal convergence |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | Step definitions may reference this ability through `abilityRefs` |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Acceptance Criteria

- The ability clearly separates alternative exploration from final recommendation.
- A loop author can reuse the guidance across more than one idea-generation step.
- The ability stays grounded in findings and open questions rather than freeform brainstorming.
