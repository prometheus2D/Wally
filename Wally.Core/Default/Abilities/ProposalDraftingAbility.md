# Proposal Drafting Ability

**Status**: Draft
**Owner**: @developer
**Ability Id**: `proposal-drafting`
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../Templates/AbilityTemplate.md](../Templates/AbilityTemplate.md)*

---

## Purpose

Provides reusable guidance for turning investigation findings, open questions, and ideas into proposal-quality draft content.

---

## Inputs

| Input | Required | Source | Notes |
|------|----------|--------|-------|
| `Findings` | No | `Docs/Findings.md` | Evidence base |
| `OpenQuestions` | No | `Docs/OpenQuestions.md` | Remaining blockers and resolved decisions |
| `Ideas` | No | `Docs/Ideas.md` | Candidate approaches |
| Existing draft | No | `Memory/ProposalDraft.md` | Used when revising rather than starting fresh |
| Proposal template rules | Yes | Proposal template | Keeps outputs aligned with repository conventions |

---

## Prompt Guidance

- draft toward repository proposal standards, not freeform notes
- surface unresolved questions explicitly instead of hiding them
- convert findings and ideas into clear problem, resolution, impact, and risk sections
- keep the proposal reviewable by humans and AI agents
- prefer incremental revision of an existing draft over full rewrites when practical

---

## Output Contract

- Produces or updates `Memory/ProposalDraft.md`.
- Preserves explicit open questions when they still matter.
- Moves the workflow toward proposal-quality output rather than brainstorming output.

---

## Constraints

- Must not mark a proposal ready when blocking questions remain hidden or undocumented.
- Must not discard relevant findings or ideas without traceability.
- Must not replace the repository proposal template with an ad hoc structure.

---

## Integration Notes

- Primary consumer: `draftProposal` prompt step.
- Best paired with local step instructions defining whether the run is creating, revising, or promoting a draft.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal.md](../Projects/Proposals/InvestigationLoopProposal.md) | Informs | Proposal drafting is a primary investigation outcome |
| [ProposalTemplate.md](../Templates/ProposalTemplate.md) | Informs | Drafting must stay aligned with canonical proposal structure |
| [AbilityTemplate.md](../Templates/AbilityTemplate.md) | Follows | Structural template for this ability document |

---

## Open Questions

- None.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Validate proposal drafting guidance against the tightened proposal template | High | ?? Not Started | @developer | 2026-04-02 | Ensure open questions remain explicit |

---

## Acceptance Criteria

- The ability clearly bridges investigation artifacts into proposal draft content.
- A loop author can reuse the guidance across proposal-creation and proposal-revision steps.
- The resulting drafts remain aligned with the canonical proposal template.
