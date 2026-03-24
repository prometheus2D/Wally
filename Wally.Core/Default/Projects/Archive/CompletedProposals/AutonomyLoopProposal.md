# Autonomy Loop — Proposal

**Status**: ? Implemented
**Author**: System Architecture Team  
**Created**: 2024-01-10  
**Last Updated**: 2025-07-17  
**Archived**: 2025-07-17

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

No feedback loop exists. An actor runs once and stops regardless of whether the goal was achieved. `MaxIterations` is declared in `WallyConfig` but never consumed. Actors cannot decide what to do next or self-correct based on their own output.

---

## Resolution

Introduce `WallyAgentLoop` — a self-driving iteration class that feeds each response back into the actor until a `StopCondition` is satisfied or `MaxIterations` is reached. Route `HandleRunTypedAsync` to the loop when loop parameters are configured. Existing single-shot and pipeline paths are unchanged.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](../../Proposals/AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 2 |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depends on | ? COMPLETE |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Sibling | ? COMPLETE |

---

## Implementation Summary

All deliverables complete:

| File | Change |
|------|--------|
| `Wally.Core/WallyAgentLoop.cs` | ? Created — core loop with StopKeyword / NoActions / MaxIterations stop conditions, AppendResponse / ReplacePrompt feedback modes, CancellationToken at every boundary |
| `Wally.Core/WallyLoopDefinition.cs` | ? Added `MaxIterations`, `StopKeyword`, `FeedbackMode`, `IsAgentLoop` |
| `Wally.Core/WallyRunResult.cs` | ? Added `Iteration` and `StopReason` fields |
| `Wally.Core/WallyEnvironment.cs` | ? Added `RunAgentLoopAsync` entry point |
| `Wally.Core/WallyCommands.cs` | ? Routes to agent loop when `loopDef.IsAgentLoop == true` |
