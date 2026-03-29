# Investigation Assessment Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `investigation-assessment`
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides the reusable assessment behavior for investigation loops: read the current investigation state, identify what is known and unknown, decide what kind of work should happen next, and emit a routing keyword plus required documentation updates.

This ability should be used by prompt steps that need to decide the next branch of an investigation workflow.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `userPrompt` | Yes | Loop input | Original user request |
| `InvestigationBrief` | Yes | `Docs/InvestigationBrief.md` | Canonical statement of request and success conditions |
| `OpenQuestions` | No | `Docs/OpenQuestions.md` | Current unresolved questions |
| `Findings` | No | `Docs/Findings.md` | Evidence gathered so far |
| `Ideas` | No | `Docs/Ideas.md` | Alternatives and candidate approaches |
| `InteractionState` | No | `Docs/InteractionState.md` | Needed when resuming from waiting state |
| `MemorySummary` | No | `Memory/` digest | Compact summary of prior work |

---

## Prompt Guidance

Assess the current investigation state from persisted documentation only.

Rules:

- restate the current goal in precise terms before deciding the next step
- separate confirmed findings from assumptions and unanswered questions
- prefer the smallest next step that reduces uncertainty or advances the proposal draft
- produce one routing keyword only
- identify which documents must be updated before the step completes
- do not rely on hidden chat history or transient memory

Preferred routing outcomes:

- `GATHER_CONTEXT` when evidence is insufficient
- `NEED_USER_INPUT` when the loop is blocked on user clarification
- `GENERATE_IDEAS` when the problem is understood well enough to explore options
- `DRAFT_PROPOSAL` when the current state supports drafting
- `COMPLETE` when proposal-quality output is ready

---

## Output Contract

- Emits exactly one routing keyword.
- Produces a concise assessment summary suitable for `InvestigationLog.md`.
- Lists the required documentation updates for `OpenQuestions.md`, `Findings.md`, `Ideas.md`, or the proposal draft.
- Does not mutate files directly; the consuming step remains responsible for documented writes.

---

## Constraints

- Must not invent facts not supported by current docs or newly gathered evidence.
- Must not skip directly to drafting when unresolved blockers remain.
- Must not treat old chat turns as authoritative state.
- Must not emit multiple competing routing keywords.

---

## Integration Notes

- Primary consumer: `assessState` prompt step in `InvestigationLoop`.
- Intended to be referenced through `abilityRefs`.
- Best used with document inputs for `InvestigationBrief`, `OpenQuestions`, `Findings`, and `Ideas`.
- May be combined with local step-specific prompt text that narrows the assessment scope.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Core investigation workflow uses this ability for branch selection |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | Step definitions may reference this ability through `abilityRefs` |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Open Questions

- None.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Validate the first `assessState` step prompt against this ability | High | ?? Not Started | @developer | 2026-04-02 | Ensure the injected guidance is sufficient but not bloated |
| Confirm routing keyword vocabulary remains stable across loops | Medium | ?? Not Started | @developer | 2026-04-03 | Keep keywords aligned with loop JSON |

---

## Acceptance Criteria

- A loop author can reference `investigation-assessment` and know what decision behavior it injects.
- A reviewer can explain which routing keywords the ability is allowed to produce.
- The ability guidance is reusable across more than one investigation assessment step.
