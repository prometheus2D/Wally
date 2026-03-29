# Task Execution Loop Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2026-03-29
**Last Updated**: 2026-03-29

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Wally has a way to investigate requests and a way to decompose proposals into task trackers, but it does not yet have a documented default loop for executing those task trackers.

The desired loop is simple:

- read one task tracker
- choose one eligible task
- execute that task
- update the tracker
- stop when all tasks are complete or a true unrecoverable failure prevents continuation

This loop must not depend on implementation plans or execution plans. The task tracker is the execution artifact.

---

## Resolution

Introduce `ExecuteTasksLoop` as the third loop in the default workflow.

Core decisions:

- `ExecuteTasksLoop` reads a single task tracker as its canonical source of execution state
- the loop focuses on one eligible task at a time
- task eligibility is determined by task status plus dependency completion
- the loop updates the tracker after each meaningful execution step
- operational failures should be recorded in the tracker as recoverable workflow state when possible
- loops and steps may use `abilityRefs`, but they must remain fully customizable without abilities
- the user may start this loop manually after `ProposalToTasks` completes

---

## Task State Model

The task tracker defines the execution state machine:

- `Not Started`
- `In Progress`
- `Blocked`
- `Complete`

Rules:

- every task begins as `Not Started`
- a task may start only when all of its dependencies are `Complete`
- a task enters `Blocked` when it cannot continue responsibly
- when a task is `Blocked`, dependency completion should be reviewed before introducing or preserving a blocker state
- a task becomes `Complete` only when its done-condition is verified

`Blocked` is recoverable workflow state. It should not be treated as proof that the whole tracker has failed permanently.

---

## One Task At A Time

The loop should execute one eligible task at a time.

Recommended behavior:

1. load the task tracker
2. find the next eligible task
3. mark it `In Progress` if it was `Not Started`
4. perform the task work
5. update status, notes, and blockers in the tracker
6. stop after that task reaches `Complete` or `Blocked`
7. on the next run, re-read the tracker and choose the next eligible task

This keeps the loop simple, inspectable, and easy for a user to supervise.

---

## Eligibility Rules

A task is eligible when:

- its status is `Not Started`
- all declared dependencies are `Complete`

A task already marked `In Progress` remains eligible for continuation.

A task marked `Blocked` may become eligible again when its dependencies and blocker conditions have been cleared.

If no task is eligible:

- and all tasks are `Complete`, the loop stops successfully
- and at least one task remains `Blocked` or has incomplete dependencies, the loop stops in recoverable blocked state

---

## Failure And Recovery Model

The execution loop should be self-recoverable where practical.

Rules:

- ordinary task-level failures should be written into the tracker as blocker or execution notes
- task-level failure should prefer `Blocked` over losing context
- re-running the loop should resume from the current tracker state rather than from hidden memory
- only true unrecoverable runtime failure should stop the loop as failed

Recommended stop outcomes:

- `ALL_TASKS_COMPLETE`
- `TASKS_BLOCKED`
- `EXECUTION_FAILED`

---

## Customization And Abilities

`ExecuteTasksLoop` must preserve the same flexibility as the other loops.

Rules:

- a step may be fully custom
- a step may reference one or more abilities through `abilityRefs`
- a step may combine local prompt text with reusable abilities
- loops must not require abilities when direct step-local configuration is clearer

The loop should use abilities only where they provide real reuse, such as task assessment, blocker review, validation, or tracker summarization.

---

## Proposed Execution Shape

Recommended conceptual step flow:

1. `readTracker`
2. `selectNextTask`
3. `reviewDependencies`
4. `executeTask`
5. `verifyTask`
6. `updateTracker`
7. `completeExecution`

These steps may be implemented as custom steps, ability-backed steps, or mixed steps.

---

## Open Questions

None. The current workflow decisions are explicit:

1. The task tracker is the execution artifact.
2. The execution loop works one task at a time.
3. Dependencies determine whether a task may start.
4. Blocked work is recoverable workflow state.
5. Manual user transition into the execution loop is acceptable in v1.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [ThreeLoopWorkflowProposal](./ThreeLoopWorkflowProposal.md) | Parent | Defines the canonical three-loop workflow |
| [InvestigationLoopProposal](./InvestigationLoopProposal.md) | Sibling | Produces the proposal that later becomes a task tracker |
| [ExecutableLoopStepsProposal](./ExecutableLoopStepsProposal.md) | Depends on | Provides the shared customizable step model |

---

## Phases

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|
| 1 | Finalize task state transitions and dependency rules | 1 | None |
| 2 | Define loop-3 step flow and stop outcomes | 1 | Phase 1 |
| 3 | Align task tracker generation with execution requirements | 1 | Phase 2 |
| 4 | Validate loop behavior against a real task tracker | 1-2 | Phase 3 |

---

## Concepts

- `ExecuteTasksLoop`: The loop that executes one eligible task at a time from a task tracker.
- `Eligible task`: A task whose dependencies are complete and whose current status allows execution.
- `Recoverable blocked state`: A tracker state where work cannot continue immediately but can resume later from the same tracker.
- `Ability-optional execution`: A step model where abilities are available but never mandatory.

---

## Impact

| System/File | Change | Risk Level |
|-------------|--------|------------|
| `Wally.Core/Default/Templates/TaskTrackerTemplate.md` | Define execution-ready task states and dependencies | Medium |
| `Wally.Core/Default/Loops/ProposalToTasks.json` | Produce trackers suitable for loop-3 execution | Low |
| `Wally.Core/Default/Projects/Proposals/TaskExecutionLoopProposal.md` | Canonical design reference for loop 3 | Low |

---

## Benefits

- Keeps execution simple and inspectable.
- Uses the task tracker as the single source of truth for work status.
- Supports manual supervision without hidden orchestration logic.
- Allows self-recovery through tracker updates instead of restarting from scratch.

---

## Risks

- Poor dependency definitions can stall execution.
- Oversized tasks can make one-task-at-a-time execution clumsy.
- Weak blocker notes can make recovery noisy.

Mitigations:

- require dependencies for every task
- require specific done-conditions
- require blockers to be recorded in the tracker
- keep decomposition granular in `ProposalToTasks`

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Define the canonical stop outcomes for loop 3 | High | ?? Not Started | @developer | 2026-03-30 | Successful completion, blocked state, true failure |
| Align task tracker generation with execution eligibility rules | High | ?? In Progress | @developer | 2026-03-29 | Dependencies and state transitions are required |
| Validate one-task-at-a-time execution against a realistic tracker | Medium | ?? Not Started | @developer | 2026-03-30 | Prefer simple, inspectable flow |

---

## Acceptance Criteria

### Must Have (Required for Approval)

- [ ] The execution loop is defined as the third loop in the default workflow.
- [ ] The loop operates on one eligible task at a time.
- [ ] The task tracker is the authoritative execution artifact.
- [ ] Dependency completion rules are explicit.
- [ ] Recoverable blocked state is explicit.
- [ ] Abilities are optional, not required.

### Should Have

- [ ] A reviewer can explain how the loop recovers from blocked work quickly.
- [ ] The loop contract is simple enough to supervise manually.

### Success Metrics

- [ ] A user can re-run execution from the tracker without reconstructing hidden state.
- [ ] A completed tracker unambiguously means the proposal work is done.
- [ ] A blocked tracker preserves enough information for the next run to continue responsibly.