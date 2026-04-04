# Current Investigation State

Temporary, loop-specific documentation for the active `InvestigationLoop` instance.

## Expected Files

| File | Purpose |
|------|---------|
| `ExecutionState.md` | Generic loop runtime checkpoint for the active loop instance, including the run id, next step, status, and resumable prompt context |
| `LoopState.md` | Current request summary, open questions, acceptance criteria, completion assessment, and next-step decision |
| `InteractionState.md` | Pending user question batch when the loop is waiting for input; deleted after the answer batch is recorded |
| `LatestUserResponse.md` | The newest recorded answer batch in a planner-friendly format |
| `UserResponses.md` | Recorded user answer batches for the active investigation |
| `Findings.md` | Evidence gathered to close current open questions |
| `Ideas.md` | Candidate approaches and decision tradeoffs |
| `ProposalDraft.md` | Working proposal draft produced by the drafting step |

These files are rewritten by loop steps as the investigation progresses. They are specific to the current active loop instance and should be treated as temporary working state. `ExecutionState.md` owns loop lifecycle and resumability; `InteractionState.md` only carries the current pending questions.