# Loop Resume Contract

**Status**: Draft
**Owner**: @developer
**Created**: 2026-04-04
**Last Updated**: 2026-04-04

*Template: [../Templates/ArchitectureTemplate.md](../Templates/ArchitectureTemplate.md)*

> Reference: This document defines the shared runtime contract for persisted loop execution state, pause/resume behavior, and surface-agnostic loop continuation.

---

## Purpose

This contract makes loop continuation reconstructable from persisted state alone.

`InvestigationLoop` and other resumable workflows must not depend on hidden UI memory, wrapper transcript history, or prompt carry-over to continue work. A reviewer should be able to inspect `ExecutionState.md` plus the canonical workflow docs and determine whether the next safe action is:

- start a fresh run
- resume pending work
- preview the next step without mutation

---

## Scope

This contract applies to:

- `Wally.Core/WallyLoopExecutionStateStore.cs`
- `Wally.Core/commands/WallyCommands.Run.cs`
- `Wally.Core/WallyLoopStepExecutionOutcome.cs`
- `Wally.Forms/ChatPanelSupport/ChatPanelExecutionSession.cs`
- future Console resume entry points

This contract does not change the ownership boundary already established for investigation interaction state:

- `ExecutionState.md` owns loop lifecycle, checkpointing, and resumability
- `InteractionState.md` owns only the current pending user-question batch
- `UserResponses.md` and `LatestUserResponse.md` own recorded answers
- chat transcripts under workspace history remain operational logs, not canonical workflow state

---

## Non-Goals

This contract does not promise transactional rollback of external side effects or advanced operator control features.

Non-goals:

- automatic rollback of shell commands, Wally commands, or code handlers
- replay, retry, or run-from-step controls in v1
- surface-specific execution rules for WinForms or Console
- using transcript logs as substitute workflow state
- attempt-history ledgers or event-sourced loop state

"Atomic" here means the saved state is good enough to resume responsibly, not that the runtime can undo every side effect.

---

## Canonical Execution-State Requirements

The execution-state document remains a Markdown file with one metadata section and the existing content sections. The runtime should extend the metadata without changing the basic file shape.

### Required Metadata Fields

| Field | Requirement | Purpose |
|-------|-------------|---------|
| `LoopName` | Required | Names the owning loop definition |
| `RunId` | Required | Stable identifier for the current run |
| `Mode` | Required | Distinguishes routed-loop, pipeline, agent-loop, or single-shot execution |
| `Status` | Required | Current lifecycle state such as `Running`, `WaitingForUser`, `Stopped`, `Completed`, or `Failed` |
| `CurrentStepName` | Required | The step most recently touched by the runtime |
| `NextStepName` | Required when resumable | The next boundary the runtime should execute on resume |
| `IterationCount` | Required | Count of committed step boundaries |
| `StopReason` | Optional | Names the reason the last boundary stopped or paused |
| `StartedAtUtc` | Required | Run start time |
| `LastUpdatedUtc` | Required | Last write time for the state document |

### Existing Content Sections

The runtime should preserve these sections:

- `## Original Request`
- `## Current Prompt`
- `## Previous Step Result`

Compatibility rule:

- existing execution-state files must remain loadable
- missing optional metadata should default cleanly
- richer operator controls, if they are ever needed, should be documented separately instead of being implied here

### Required Metadata Order

For readability and stable parsing, resumable loop state files should write metadata bullets in this order:

1. `LoopName`
2. `RunId`
3. `Mode`
4. `Status`
5. `CurrentStepName`
6. `NextStepName`
7. `IterationCount`
8. `StopReason`
9. `StartedAtUtc`
10. `LastUpdatedUtc`

---

## Resume Model

The runtime only needs enough persisted state to restart or continue the loop from a known point.

Rules:

- `NextStepName` is the authoritative resume target
- `Status` tells the surface whether the loop is running, waiting, stopped, completed, or failed
- `PreviousStepResult` carries the last step output when the next step needs it
- the runtime must never infer resumability from UI state alone

Important distinction:

- `CurrentStepName` is the step most recently touched by the runtime
- `NextStepName` is the step the runtime should execute next when resuming
- `Status=WaitingForUser` means the loop is paused on purpose, not broken

A reviewer should be able to resume the loop from `NextStepName` plus the canonical docs without additional hidden state.

---

## Runtime Rules

1. Starting a fresh run writes a new execution-state document, sets `Status=Running`, sets the initial `NextStepName`, and clears loop-specific transient artifacts declared by the loop.
2. Before each step executes, the runtime updates `CurrentStepName`, preserves the original request, and keeps `NextStepName` aligned with the current continuation target.
3. After a normal step completes, the runtime writes the step result into `PreviousStepResult`, updates `IterationCount`, and sets `NextStepName` to the resolved next step or clears it if the loop is complete.
4. A `user_input` step persists `InteractionState.md`, sets `Status=WaitingForUser`, and writes the resume target into `NextStepName`.
5. Recording a user answer updates `UserResponses.md`, refreshes `LatestUserResponse.md`, deletes `InteractionState.md`, and then resumes from shared execution state.
6. Preview flows may inspect the current step and render prompts or equivalent commands, but they must not mutate execution state.

---

## Control Semantics

### Fresh Run

Use when a new prompt is supplied.

Rules:

- clear only the transient paths declared by the loop definition
- write a new `RunId`
- write the initial execution state before any step execution begins
- do not reuse stale state from a previous run

### Resume Pending Work

Resume means continue from the persisted `NextStepName` and current state file.

Rules:

- resume uses the existing `RunId`, original request, and current docs
- resume is allowed whenever `NextStepName` exists and the loop is not terminal
- resume after `WaitingForUser` is allowed only after the answer batch has been recorded and `InteractionState.md` has been removed

### Preview Step

Preview is a read-only inspection flow.

Rules:

- preview must not write `ExecutionState.md`
- preview may render the current prompt, resolved inputs, route hints, or an equivalent CLI command
- equivalent CLI commands are informational until explicitly executed

---

## Surface Rules

Console and Forms must consume the same persisted state model.

Rules:

- waiting state, run labels, and resume guidance come from `ExecutionState.md` plus `InteractionState.md`
- manual stepping must reuse the shared Core step executor instead of inventing a second runtime path
- transcript logs are operational history only; they are never required input for loop continuation

---

## Core API Plan

The next implementation should keep the helper surface small and centralize state mutation in the store.

## Recommended Minimal Delivery Sequence

The best-engineered implementation path is to change the runtime in the smallest number of places that can become authoritative for every surface.

Delivery sequence:

1. Keep `WallyLoopExecutionStateStore` as the single authority for reading and writing resumable loop state.
2. Move `WallyCommands.Run.cs` and `ChatPanelExecutionSession.cs` onto the same small store helper surface instead of open-coding lifecycle mutations in multiple places.
3. Keep the state shape close to the current runtime model unless a real resume bug proves more metadata is necessary.
4. Validate fresh run, waiting-for-user, answer recording, preview, and resume across Console and WinForms.

Why this order is minimal:

- the state store becomes the single mutation authority instead of spreading lifecycle logic across Core and Forms
- the runtime stays aligned with the existing persisted state shape instead of inventing a second layer of metadata
- Console and WinForms share one resume path

## Engineering Guardrails

The implementation should stay intentionally narrow.

Required guardrails:

- do not introduce a second execution-state model for WinForms or Console
- do not add a general event store, attempt-history ledger, or transcript-driven resume mechanism in v1
- do not add replay policy or side-effect metadata unless a proven runtime need appears
- do not refactor unrelated loop infrastructure while improving resume behavior
- do not let preview flows call state-mutating helpers or write execution-state files

Preferred defaults:

- centralize all execution-state writes in the store even if older helper names remain as thin wrappers temporarily
- preserve backward compatibility for existing state files and loop definitions

### `WallyLoopExecutionStateStore.cs`

Add or refactor helpers with responsibilities like:

- `PrepareForRun(...)`
- `TryLoadCurrent(...)`
- `Save(...)`
- `UpdateAndSave(...)`

Design rules:

- keep markdown parsing backward compatible
- centralize metadata writes in the store instead of open-coding state transitions in UI or loop executors
- keep `InvestigationInteractionStore` out of lifecycle ownership

### `WallyLoopStepExecutionOutcome.cs`

Keep the shared outcome type small and focused on what the runtime actually needs to continue execution.

Recommended additions:

- resolved route keyword when one matched
- resolved next step name when known
- a concise outcome classification such as `Completed`, `WaitingForUser`, `Failed`, or `Stopped`

### `WallyCommands.Run.cs`

Move named-step lifecycle updates onto the shared store helpers.

Required work:

- use the shared state helpers consistently before and after each step
- keep resume behavior based on `NextStepName`, `Status`, and persisted docs
- do not add new operator-control APIs until a concrete need exists beyond resume

### `ChatPanelExecutionSession.cs`

WinForms manual stepping should call the same helpers as Core loop execution.

Rules:

- manual stepping must not write bespoke checkpoint transitions
- prompt preview remains read-only
- the session layer may decide which button to show, but Core owns what resume means

### `Program.cs`

Console does not need special step-by-step semantics. It only needs to load the same state and resume the same way.

---

## Intentional Deferrals

These items should stay deferred until the minimal shared runtime path is complete:

- first-class Console replay UI or multi-command step-by-step UX
- retry, replay, or run-from-step control features
- durable attempt-history logs beyond the current state document
- rollback semantics for external side effects
- automatic continuation suggestions driven by transcript analysis
- broad loop-authoring changes outside the fields already needed for resume

---

## Resume Eligibility Rules

| Action | Minimum Required State | Additional Rule |
|--------|------------------------|-----------------|
| `resume` | `NextStepName` exists and `Status` is not terminal | Use the persisted original request plus current docs |
| `resume-after-answer` | `Status=WaitingForUser` and the answer batch has been recorded | `InteractionState.md` must be removed before continuing |
| `preview` | Any resolvable pending step | Must not mutate execution state |

---

## Validation Scenarios

The resume implementation is not done until these scenarios are reconstructable from persisted state:

1. Fresh run writes a new execution-state document before the first step executes.
2. A prompt step completes and updates `NextStepName` correctly for the next resume point.
3. A `user_input` step persists `InteractionState.md`, sets `Status=WaitingForUser`, and resumes correctly after answers are saved.
4. Prompt preview renders the next step without changing any execution-state field.
5. WinForms manual stepping and the shared routed-loop runtime produce the same saved state for the same step sequence.
6. Console can resume the same paused loop that WinForms created, and WinForms can resume the same paused loop that Console created.

---

## Acceptance Criteria

### Must Have

- [ ] The execution-state contract is small enough to implement directly against the current runtime model without inventing speculative metadata.
- [ ] The contract defines fresh run, pause, resume, and preview semantics clearly enough that Console and WinForms can share the same persisted state.
- [ ] The ownership boundary between `ExecutionState.md`, `InteractionState.md`, user-response docs, and transcript logs remains explicit.
- [ ] The Core API responsibilities are assigned clearly enough that WinForms and future Console controls can share the same runtime path.

### Should Have

- [ ] Validation scenarios cover fresh run, waiting-for-user, preview, and cross-surface resume behavior.

---

## Document Lifecycle

This document should exist only while it adds clarity that the code, tests, and minimal durable docs do not yet provide.

Expected end state after implementation is stable:

- shared resume behavior is enforced by the runtime and verified by tests
- Console and WinForms follow the same Core path without needing a separate planning document to stay aligned
- any essential operator or maintainer guidance has been folded into a durable runtime doc or inline code documentation
- this document can be merged into a smaller durable note or removed entirely if it becomes duplicate explanation

The goal is not to keep a permanent contract catalog. The goal is to make shared resume behavior obvious enough in the real system that temporary planning material can disappear.

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| `../Projects/Proposals/InvestigationLoopProposal.md` | Implements | Defines investigation-loop behavior and paused interaction ownership that this contract makes runtime-explicit |
| `../Projects/Proposals/ExecutableLoopStepsProposal.md` | Implements | Defines typed-step behavior that this contract constrains to a simple shared resume model |
| `../Projects/Proposals/InvestigationLoopProposalTasks.md` | Informs | Tracks checkpoint and operator-control implementation work that follows from this contract |
| `../Projects/Proposals/ExecutableLoopStepsProposalTasks.md` | Informs | Tracks typed-step and Core API work that follows from this contract |
| `TaskExecutionWorkflowContract.md` | Informs | Example of a proposal-backed runtime contract document |
