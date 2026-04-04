# Investigation Assessment Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `investigation-assessment`
**Created**: 2026-03-29
**Last Updated**: 2026-04-02

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides reusable assessment guidance for investigation loops: read the current investigation state, identify what is known and unknown, decide what should happen next, and emit one routing keyword plus the required documentation updates.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `userPrompt` | Yes | Loop input | Original user request |
| `LoopState` | Yes | Current loop docs | Canonical state for the current turn |
| `UserResponses` | No | `UserResponses.md` | Prior user answers |
| `LatestUserResponse` | No | `LatestUserResponse.md` | Most recent answer batch |
| `Findings` | No | `Findings.md` | Evidence gathered so far |
| `Ideas` | No | `Ideas.md` | Alternatives and candidate approaches |
| `ProposalDraft` | No | `ProposalDraft.md` | Current draft state |

---

## Prompt Guidance

Assess the current investigation state from persisted documentation only.

Rules:

- restate the current goal before choosing the next step
- separate confirmed findings from assumptions and unanswered questions
- prefer the smallest next step that reduces uncertainty or advances the proposal draft
- emit exactly one routing keyword
- identify which documents must be updated before the step completes
- do not rely on hidden chat history or transient memory

Preferred routing outcomes:

- `REQUEST_USER_INPUT` when the loop is blocked on user clarification
- `GATHER_CONTEXT` when evidence is insufficient
- `GENERATE_IDEAS` when the problem is understood well enough to explore options
- `DRAFT_PROPOSAL` when the current state supports drafting
- `COMPLETE` when proposal-quality output is ready

---

## Output Contract

- Emit exactly one routing keyword.
- Produce a concise assessment summary suitable for `LoopState.md` or a completion-check step.
- List the required documentation updates for findings, ideas, proposal draft, or user input state.
- Do not mutate files directly; the consuming step remains responsible for documented writes.

---

## Constraints

- Must not invent facts not supported by current docs or newly gathered evidence.
- Must not skip directly to drafting when unresolved blockers remain.
- Must not treat old chat turns as authoritative state.
- Must not emit multiple competing routing keywords.

---

## Integration Notes

- Primary consumers: `planInvestigation` and `complete` prompt steps.
- Intended to be referenced through `abilityRefs`.
- Best used with loop-state documents that make the next routing decision explicit.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Core investigation workflow uses this ability for branch selection |
| [ExecutableLoopStepsProposal.md](../Projects/Proposals/ExecutableLoopStepsProposal.md) | Informs | Step definitions may reference this ability through `abilityRefs` |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Acceptance Criteria

- A loop author can reference `investigation-assessment` and know what decision behavior it injects.
- A reviewer can explain which routing keywords the ability is allowed to produce.
- The ability guidance is reusable across more than one investigation assessment step.
