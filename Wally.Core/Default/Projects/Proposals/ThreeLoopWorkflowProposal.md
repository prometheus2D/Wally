# Three Loop Workflow Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Wally currently contains pieces of a workflow, but the intended end-to-end operating model has not been stated simply enough.

The repo has investigation concepts, proposal decomposition, and older implementation-plan and execution-plan artifacts. That creates ambiguity about the real default path a user should follow.

The desired workflow is simpler:

1. gather information and produce a proposal
2. decompose the approved proposal into a task tracker
3. execute the task tracker one task at a time

Implementation plans and execution plans are redundant for this current workflow. They add an extra layer between proposal approval and task execution without adding enough value for v1.

---

## Resolution

Adopt one canonical three-loop workflow for the current product:

1. `InvestigationLoop`
2. `ProposalToTasks`
3. `ExecuteTasksLoop`

Core decisions:

- `InvestigationLoop` gathers context, asks follow-up questions, and produces proposal-quality output
- `ProposalToTasks` reads an approved proposal and writes one authoritative task tracker
- `ExecuteTasksLoop` reads that task tracker and executes one eligible task at a time
- the task tracker is the authoritative execution artifact after proposal approval
- implementation plans are not part of the current default workflow
- execution plans are not part of the current default workflow
- the user may move between loops manually in v1; automatic transition is deferred
- loops and steps may use `abilityRefs`, but loops and steps must remain fully customizable without abilities when that is the better fit
- the workflow should remain simple, inspectable, and self-recoverable

---

## Canonical Loop Sequence

### Loop 1: InvestigationLoop

- input: user request
- output: approved proposal document
- purpose: understand the problem, gather evidence, ask questions, and converge on a proposal

### Loop 2: ProposalToTasks

- input: approved proposal path
- output: one `*Tasks.md` tracker in the same directory as the proposal
- purpose: convert proposal scope into dependency-aware executable tasks

### Loop 3: ExecuteTasksLoop

- input: task tracker path
- output: the same task tracker updated until all eligible work is complete or recoverably blocked
- purpose: execute one eligible task at a time, update task state, and recover from blockers without losing workflow state

---

## Task Tracker As Execution Artifact

The task tracker replaces implementation plans and execution plans in the current workflow.

Rules:

- every approved proposal must be decomposed into exactly one task tracker before task execution begins
- task execution reads and writes the task tracker as the canonical source of task state
- task dependencies are declared in the tracker itself
- blocked work is recorded in the tracker itself
- execution does not require a separate implementation-plan or execution-plan document

---

## Manual Workflow Transition

The user may start each loop manually in v1.

Examples:

1. run `InvestigationLoop` to create or refine a proposal
2. run `ProposalToTasks` against the approved proposal
3. run `ExecuteTasksLoop` against the resulting task tracker path

This keeps the workflow understandable while the loop contracts are still being refined. Automatic loop chaining can be added later without changing the core artifacts.

---

## Open Questions

None. The current workflow decisions are now explicit:

1. `InvestigationLoop -> ProposalToTasks -> ExecuteTasksLoop` is the default three-loop workflow.
2. Implementation plans are removed from the current default workflow.
3. Execution plans are removed from the current default workflow.
4. Task trackers are the authoritative execution artifact.
5. Users may transition between loops manually in v1.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal](./InvestigationLoopProposal.md) | Child - Loop 1 | Defines the proposal-authoring investigation workflow |
| [ExecutableLoopStepsProposal](./ExecutableLoopStepsProposal.md) | Depends on | Shared step and routing model used across the loops |
| [TaskExecutionLoopProposal](./TaskExecutionLoopProposal.md) | Child - Loop 3 | Defines task execution from the task tracker |

---

## Phases

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|
| 1 | Finalize the canonical three-loop workflow and artifact handoffs | 1 | None |
| 2 | Align task tracker structure with execution needs | 1 | Phase 1 |
| 3 | Align loop proposals and shipped loop definitions with the canonical workflow | 1-2 | Phases 1-2 |
| 4 | Validate the workflow manually end to end | 1-2 | Phase 3 |

---

## Concepts

- `Three-loop workflow`: The default user-facing sequence of `InvestigationLoop`, `ProposalToTasks`, and `ExecuteTasksLoop`.
- `Task tracker`: The canonical execution artifact created from an approved proposal.
- `Manual transition`: The user starts the next loop explicitly rather than relying on automatic chaining.
- `Ability-optional loop`: A loop or step that may use `abilityRefs` but is not required to do so.

---

## Impact

| System/File | Change | Risk Level |
|-------------|--------|------------|
| `Wally.Core/Default/Projects/Proposals/InvestigationLoopProposal.md` | Clarify loop 1 in the three-loop chain | Low |
| `Wally.Core/Default/Loops/ProposalToTasks.json` | Clarify loop 2 handoff into task execution | Low |
| `Wally.Core/Default/Templates/TaskTrackerTemplate.md` | Make the task tracker execution-ready | Medium |
| `Wally.Core/Default/Projects/Proposals/TaskExecutionLoopProposal.md` | Add canonical loop-3 proposal | Low |
| `Wally.Core/Default/Templates/ProposalTemplate.md` | Replace plan-based next-step language with task-tracker language | Low |

---

## Benefits

- Removes redundant planning layers from the core workflow.
- Gives users a simple, inspectable path from question to proposal to execution.
- Makes the task tracker the single source of truth for execution state.
- Preserves flexibility: loops and steps can stay custom or use abilities where helpful.

---

## Risks

- Some complex workstreams may later need a richer orchestration artifact.
- If the task tracker is underspecified, execution can become noisy or inconsistent.

Mitigations:

- keep the current workflow minimal and explicit
- define task dependencies and state transitions in the tracker template
- add automation only after the three-loop contracts are stable

---

## Acceptance Criteria

### Must Have (Required for Approval)

- [ ] The default workflow is documented as exactly three loops.
- [ ] The task tracker is defined as the canonical execution artifact.
- [ ] Implementation plans are not required anywhere in the default workflow.
- [ ] Execution plans are not required anywhere in the default workflow.
- [ ] Manual user transition between loops is explicitly documented.

### Should Have

- [ ] A reviewer can explain the workflow in under a minute.
- [ ] The loop handoffs are artifact-based and unambiguous.

### Success Metrics

- [ ] A user can describe which file is produced after each loop without guessing.
- [ ] A proposal can move into decomposition and then execution without any plan-based intermediate artifact.