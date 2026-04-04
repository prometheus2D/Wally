# Investigator Workspace

Runtime state folder for `InvestigationLoop`. The streamlined loop now keeps its per-run investigation state under a temporary active-investigation folder so one-shot iterations can rebuild from loop-specific docs instead of mailbox state.

## Folders

| Folder | Created By | Contents |
|--------|-----------|----------|
| `Active/CurrentInvestigation/` | Loop steps and runtime | Temporary per-loop docs such as `LoopState.md`, `InteractionState.md`, `LatestUserResponse.md`, `UserResponses.md`, `Findings.md`, `Ideas.md`, `ProposalDraft.md`, and `ExecutionState.md` |
| `Docs/` | General investigator docs | Shared or legacy investigator docs not tied to the current active loop instance |
| `Memory/` | Optional future loop state | Durable summaries or private notes if the workflow later needs them |
| `Inbox/` | Reserved | Not part of the streamlined default investigation loop |
| `Outbox/` | Reserved | Not part of the streamlined default investigation loop |

The loop rebuilds context from the files in `Active/CurrentInvestigation/` each iteration. Documentation is the source of truth, not prompt history.

## Active Investigation Files

- `LoopState.md` records the current request summary, open questions, acceptance criteria, completion assessment, next ability, and routing decision.
- `ExecutionState.md` records the generic loop runtime checkpoint so InvestigationLoop can resume from the next step without hidden prompt carry-over. It is the source of truth for run id, lifecycle status, and resume mechanics.
- `InteractionState.md` records only the current waiting question batch when the loop pauses for user input. It exists only while the loop is waiting for answers.
- `LatestUserResponse.md` isolates the newest recorded answer batch so the next planning turn can reason from the freshest user clarification directly.
- `UserResponses.md` stores the user's recorded answer batches for the active investigation.
- `Findings.md`, `Ideas.md`, and `ProposalDraft.md` are written by the selected ability steps as the loop progresses.

See `Loops/InvestigationLoop.json` for the `documentInputs` that declare every path.