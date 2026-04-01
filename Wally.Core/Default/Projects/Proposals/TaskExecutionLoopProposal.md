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
- each run ends with a named stop outcome derived from the persisted tracker state
- operational failures should be recorded in the tracker as recoverable workflow state when possible
- loops and steps may use `abilityRefs`, but they must remain fully customizable without abilities
- the user may start this loop manually after `ProposalToTasks` writes the tracker

---

## Canonical Workflow Placement

`ExecuteTasksLoop` is the third loop in the current default three-loop workflow:

1. `InvestigationLoop`: input = user request; output = approved proposal document
2. `ProposalToTasks`: input = approved proposal path; output = one dependency-aware `*Tasks.md` tracker in the same directory as the proposal
3. `ExecuteTasksLoop`: input = task tracker path; output = the same tracker updated until all eligible work is complete or recoverably blocked

Current workflow decisions:

- implementation plans are not part of the current default workflow
- execution plans are not part of the current default workflow
- the task tracker is the authoritative handoff artifact between proposal approval and task execution
- the user may start each loop manually; automatic loop-to-loop transition is deferred

---

## Task Tracker Handoff Contract

`ExecuteTasksLoop` expects `ProposalToTasks` to hand off exactly one canonical `*Tasks.md` tracker.

Required tracker assumptions:

- every task row includes `Status`, `Owner`, `Dependencies`, and `Done-Condition`
- task statuses use only `Not Started`, `In Progress`, `Blocked`, and `Complete`
- dependency gating is defined in the tracker itself rather than hidden runtime state
- blocker context is recorded in the tracker when execution cannot continue responsibly
- the progress summary includes phase rows when phases exist and always includes a `Total` row

This keeps the loop contract artifact-based and inspectable: execution can resume from the tracker alone without an implementation plan, execution plan, or hidden memory.

Detailed runtime behavior is defined in [../../Docs/TaskExecutionWorkflowContract.md](../../Docs/TaskExecutionWorkflowContract.md). This proposal states the design intent; the contract document defines the normative tracker interpretation, stop outcomes, and failure boundary.

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
- a task already marked `In Progress` remains eligible for continuation until it becomes `Complete` or `Blocked`
- a task enters `Blocked` when it cannot continue responsibly
- when a task is `Blocked`, dependency completion should be reviewed before introducing or preserving a blocker state
- a blocked task may become eligible again only after its dependencies are complete and its recorded blocker has been cleared
- a task becomes `Complete` only when its done-condition is verified

`Blocked` is recoverable workflow state. It should not be treated as proof that the whole tracker has failed permanently.

---

## One Task At A Time

The loop should execute one eligible task at a time.

Contract behavior:

1. load the task tracker
2. recompute dependency and blocker state from the tracker only
3. continue the earliest task already marked `In Progress`, if one exists
4. otherwise choose the first `Not Started` or recoverably `Blocked` task that is now eligible in tracker order
5. mark the task `In Progress` before meaningful work starts when it was previously `Not Started`
6. perform the task work
7. update status, notes, blockers, and progress summary in the tracker
8. stop after that task reaches `Complete` or `Blocked`, using a named stop outcome that matches the persisted tracker state
9. on the next run, re-read the tracker and choose the next eligible task again

This keeps the loop simple, inspectable, and easy for a user to supervise.

---

## Eligibility Rules

A task is eligible when:

- it is already `In Progress`
- it is `Not Started` and all declared dependencies are `Complete`
- it is `Blocked`, all declared dependencies are `Complete`, and its recorded blocker has been cleared

A task is not eligible when it is `Complete`, when dependencies remain incomplete, or when a blocker is still active.

If no task is eligible:

- and all tasks are `Complete`, the loop stops successfully with `ALL_TASKS_COMPLETE`
- and at least one task remains `Blocked` or has incomplete dependencies, the loop stops in recoverable blocked state with `TASKS_BLOCKED`

---

## Failure And Recovery Model

The execution loop should be self-recoverable where practical.

Rules:

- ordinary task-level failures should be written into the tracker as blocker or execution notes
- task-level failure should prefer `Blocked` over losing context
- re-running the loop should resume from the current tracker state rather than from hidden memory
- only true unrecoverable runtime failure should stop the loop as failed with `EXECUTION_FAILED`

Named stop outcomes:

- `TASK_COMPLETED`
- `TASK_BLOCKED`
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

The default execution shape for each run is:

1. `readTracker`
2. `selectNextTask`
3. `reviewSelectedTask`
4. `executeSelectedTask`
5. `verifySelectedTask`
6. `persistTracker`
7. `stopWithOutcome`

Step responsibilities:

| Step | Responsibility |
|------|----------------|
| `readTracker` | Read and validate the tracker before any state transition |
| `selectNextTask` | Choose the one eligible task for this run, or stop early with `ALL_TASKS_COMPLETE` or `TASKS_BLOCKED` |
| `reviewSelectedTask` | Re-check dependencies and blocker state before meaningful work begins |
| `executeSelectedTask` | Perform the selected task's work against the repository and required documents |
| `verifySelectedTask` | Compare results to the task's done-condition and decide whether the task is `Complete` or `Blocked` |
| `persistTracker` | Write task-state changes, notes, and progress counts back to the tracker |
| `stopWithOutcome` | End the run with `TASK_COMPLETED`, `TASK_BLOCKED`, `ALL_TASKS_COMPLETE`, `TASKS_BLOCKED`, or `EXECUTION_FAILED` |

These steps may be implemented as custom steps, ability-backed steps, or mixed steps. The companion runtime contract document is the normative source for the detailed execution shape, task selection order, blocked-state recovery, tracker update expectations, and stop-outcome boundaries.

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
| [InvestigationLoopProposal](./InvestigationLoopProposal.md) | Upstream sibling | Produces the approved proposal that `ProposalToTasks` later converts into a task tracker |
| [ExecutableLoopStepsProposal](./ExecutableLoopStepsProposal.md) | Depends on | Provides the shared customizable step model |

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [../../Docs/TaskExecutionWorkflowContract.md](../../Docs/TaskExecutionWorkflowContract.md) | Companion contract | Defines the normative runtime behavior for eligibility, blocker recovery, and stop outcomes |
| [../../Templates/TaskTrackerTemplate.md](../../Templates/TaskTrackerTemplate.md) | Companion template | Defines the canonical tracker structure required by execution |

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
| `Wally.Core/Default/Docs/TaskExecutionWorkflowContract.md` | Define the runtime contract for tracker interpretation and stop outcomes | Low |
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