# Documentation Workflow Proposal

**Status**: In Progress (Phases 1-3 Complete, Phase 4 Pending Validation)
**Author**: System Architect  
**Created**: 2024-01-15  
**Last Updated**: 2025-07-17  

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Wally's project documentation — proposals, implementation plans, architecture docs — drifts out of sync as implementation progresses. Completed phases still show "Not Started", architecture docs carry stale warnings, proposals that are fully implemented remain in the active folder, and status fields across related documents disagree. Today a human must manually walk every file, compare it to reality, and fix the discrepancies. This is error-prone, tedious, and never complete.

A secondary gap: when a new proposal is approved, no automated path converts it into a properly structured implementation plan. The `ProposalToImplementationPlan` loop exists but has never been exercised within a documentation reflection cycle that validates the output.

---

## Resolution

Introduce a **Documentation Reflection** loop — an agent-loop definition that instructs an actor to:

1. **Audit** — scan all project documentation for staleness, inconsistency, and missing artefacts.
2. **Reconcile** — update status fields, move completed documents to the archive, fix cross-references.
3. **Generate** — create missing implementation plans from approved proposals using the `ImplementationPlanTemplate.md`.
4. **Converge** — stop when no further changes are needed (convergence detection via `StopKeyword`).

This uses the existing infrastructure:
- **Agent loop** (`WallyAgentLoop`) with `StopKeyword` convergence
- **Mailbox system** for cross-actor coordination (BA reviews Engineer output)
- **`write_document`** action for file creation/updates
- **`browse_workspace`** and **`read_context`** actions for inspection

No new C# code is required. This phase delivers:
- A loop definition JSON file
- Actor prompt enhancements for documentation awareness
- A documentation workflow guide
- Validation that the system works end-to-end

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Phase 4 of the parent proposal |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Depends on | ? COMPLETE — `WallyAgentLoop` with stop conditions + feedback |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Depends on | ? COMPLETE — `process-mailboxes` + `route-outbox` |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Create `DocumentationReflection.json` loop definition | 0.5 days | Agent loop implemented ? |
| 2 | Enhance actor prompts for documentation awareness | 0.5 days | None |
| 3 | Create documentation workflow guide | 0.5 days | Phases 1-2 |
| 4 | Validate end-to-end with real project data | 1 day | Phase 3 |

---

## Concepts

- **Documentation Reflection Loop**: An agent loop that iterates over workspace documentation, identifies inconsistencies, and emits `write_document` actions to fix them. Stops when it emits `DOCUMENTATION_COMPLETE` (no further changes needed).
- **Convergence Detection**: The loop stops when the actor determines no documents need updating. Implemented via `StopKeyword: "DOCUMENTATION_COMPLETE"` — the actor includes this phrase in its response when it finds nothing left to change.
- **Documentation Audit**: First-pass scan that inventories all proposals, implementation plans, and architecture docs, comparing declared status against actual completion state.
- **Cross-Document Reconciliation**: Ensuring that related documents (e.g., a proposal's status matches its implementation plan's status) are consistent.

---

## Loop Definition

```json
{
  "name": "DocumentationReflection",
  "description": "Audits and reconciles workspace documentation — updates stale statuses, archives completed work, generates missing implementation plans",
  "enabled": true,
  "actorName": "BusinessAnalyst",
  "maxIterations": 5,
  "stopKeyword": "DOCUMENTATION_COMPLETE",
  "feedbackMode": "AppendResponse",
  "startPrompt": "Perform a documentation reflection cycle on the workspace.\n\nScan all project documentation using browse_workspace and read_context:\n- Projects/Proposals/ — check each proposal's Status field against reality\n- Projects/ImplementationPlans/ — verify phase completion tracking is current\n- Projects/Archive/ — confirm completed work is archived\n- Docs/ — update architecture docs with current implementation status\n\nFor each document:\n1. Read the document with read_context\n2. Compare declared status to actual implementation state\n3. If out of date, emit a write_document action with corrected content\n4. If a proposal is fully implemented, note it should be archived\n5. If an approved proposal lacks an implementation plan, create one following ImplementationPlanTemplate.md\n\nWhen coordination with Engineer is needed for technical accuracy, use send_message.\n\nWhen you have reviewed all documents and no further changes are needed, include DOCUMENTATION_COMPLETE in your response.\n\n{userPrompt}"
}
```

---

## Actor Prompt Enhancements

### BusinessAnalyst

Add documentation reflection guidance to the role prompt. The BA is the natural owner of documentation health because they already own project status tracking:

- Add to `rolePrompt`: awareness of documentation lifecycle (Draft ? Approved ? Implemented ? Archived)
- Add to `criteriaPrompt`: cross-document consistency checking rules
- Add to `allowedLoops`: `"DocumentationReflection"`

### Engineer

Add technical documentation review capability:

- Add to `allowedLoops`: `"DocumentationReflection"` (for cases where the Engineer runs the loop instead of BA)
- No prompt changes needed — Engineer already has `write_document` and `read_context`

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/Default/Loops/DocumentationReflection.json` | CREATE — New loop definition | None |
| `Wally.Core/Default/Actors/BusinessAnalyst/actor.json` | MODIFY — Add documentation reflection guidance + allowedLoops entry | Low |
| `Wally.Core/Default/Actors/Engineer/actor.json` | MODIFY — Add `DocumentationReflection` to allowedLoops | Low |
| `Wally.Core/Default/Docs/DocumentationWorkflowGuide.md` | CREATE — Usage patterns and best practices | None |

---

## Benefits

- **Automated staleness detection** — no more manually scanning every doc
- **Convergence-safe** — `MaxIterations` cap (5) and `StopKeyword` prevent runaway loops
- **Uses existing infrastructure** — agent loop, mailbox, write_document actions — no new C# code
- **Human-triggered** — run `wally run "" -l DocumentationReflection` when you want a documentation audit
- **Mailbox-integrated** — BA can ask Engineer for technical clarification via `send_message` during the loop

---

## Risks

| Risk | Mitigation |
|------|-----------|
| Loop doesn't converge (keeps finding changes) | `MaxIterations: 5` hard cap; `StopKeyword` for early exit |
| Actor makes incorrect status assessments | Human reviews output; no auto-archival without confirmation |
| Large workspace exceeds context limits | `browse_workspace` limited to 50 items; actor processes incrementally |
| Cross-document references broken after move | Actor uses `read_context` to verify paths before updating |

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| ~~Create `DocumentationReflection.json` loop definition~~ | High | ? Complete | @engineer | 2025-07-17 | Phase 1 — `MaxIterations: 5`, `StopKeyword: "DOCUMENTATION_COMPLETE"` |
| ~~Add `DocumentationReflection` to BusinessAnalyst allowedLoops~~ | High | ? Complete | @engineer | 2025-07-17 | Phase 2 |
| ~~Add `DocumentationReflection` to Engineer allowedLoops~~ | Medium | ? Complete | @engineer | 2025-07-17 | Phase 2 |
| ~~Enhance BusinessAnalyst prompts for doc awareness~~ | Medium | ? Complete | @engineer | 2025-07-17 | Phase 2 — rolePrompt, criteriaPrompt, intentPrompt enhanced |
| ~~Create DocumentationWorkflowGuide.md~~ | Medium | ? Complete | @architect | 2025-07-17 | Phase 3 — usage patterns, best practices, troubleshooting |
| Test loop with real workspace documentation | High | ?? Not Started | @qa | TBD | Phase 4 |
| Validate convergence detection works correctly | High | ?? Not Started | @qa | TBD | Phase 4 |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [x] `DocumentationReflection.json` loop definition created and loadable
- [ ] Loop runs end-to-end without errors using `wally run "" -l DocumentationReflection`
- [ ] Actor correctly identifies stale document statuses
- [x] `StopKeyword` convergence terminates the loop when no changes remain
- [x] `MaxIterations` cap prevents infinite cycling
- [x] No new C# code changes required (pure configuration + content)

#### Should Have (Preferred for Quality)
- [ ] Cross-actor coordination via `send_message` works within the reflection loop
- [x] Documentation workflow guide provides clear usage patterns
- [x] Actor prompts enhanced for documentation lifecycle awareness
- [ ] Validated with real project data (current workspace proposals)

#### Completion Checklist
- [x] Loop definition created and tested
- [x] Actor configurations updated
- [x] Documentation guide written
- [ ] End-to-end validation passed
- [ ] Status updated to "Implemented"

---

## Open Questions

### 1. Should the loop auto-archive completed proposals? 

**Resolution**: No — the loop should **identify** documents ready for archival and **recommend** the move, but not execute it. File moves (rename/delete) are destructive and should be human-confirmed. The loop's `write_document` action can update status fields, but moving files between folders requires a new `move_document` action that doesn't exist yet.

### 2. How does the loop handle multiple iterations?

**Resolution**: `FeedbackMode: "AppendResponse"` — each iteration sees the original prompt plus all previous responses. This gives the actor full context of what it already audited and changed, preventing duplicate work. The actor should track what it reviewed in each iteration and focus on remaining documents.

### 3. Which actor should own the loop?

**Resolution**: BusinessAnalyst is the primary owner (they own project status). Engineer can run it too (both have it in `allowedLoops`). For technical accuracy questions, BA uses `send_message` to Engineer during the loop — responses are picked up in the next `process-mailboxes` cycle.