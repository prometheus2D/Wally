# Autonomy Loop — Proposal

**Status**: Draft  
**Author**: System Architecture Team  
**Created**: 2024-01-10  
**Last Updated**: 2024-01-10  

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
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 2 |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depends on | `ExecuteActorAsync` must exist before the loop can call it |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Sibling | Independent; agents can emit `send_message` actions from within a loop |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Implement `WallyAgentLoop` core | 5-7 days | Async execution complete |

---

## Concepts

- `WallyAgentLoop`: Wraps a single actor + wrapper; runs iteratively until stop condition or max iterations
- `StopCondition`: `Func<string, bool>` — called with latest response; `true` halts loop
- `FeedbackMode`: How previous response feeds into next prompt (append, replace, or custom)
- `AgentLoopResult`: Contains all iteration results, stop reason, and iteration count

---

## Execution Model

```
userPrompt ? Actor.ProcessPrompt ? LLMWrapper.ExecuteAsync ? response
ActionDispatcher.Dispatch(response) ? actionSummary
StopCondition(response) ? true ? done
                      ? false, iteration < MaxIterations
FeedbackMode.Combine(userPrompt, response) ? newPrompt ? back to top
```

### Stop Condition Priority
1. Response contains `StopKeyword` (case-insensitive, when configured)
2. `ActionDispatcher.DispatchedCount == 0` AND `ActionDispatcher.SkippedCount == 0` (no actions — actor considers itself done)
3. `MaxIterations` reached — loop exits with `StoppedByCondition = false`

### Loop Definition Schema
```json
{
  "name": "AutoRequirements",
  "actorName": "RequirementsExtractor",
  "maxIterations": 5,
  "stopKeyword": "REQUIREMENTS_COMPLETE",
  "feedbackMode": "AppendResponse",
  "startPrompt": "..."
}
```

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/WallyLoop.cs` | Add `WallyAgentLoop` class | Low |
| `Wally.Core/WallyLoopDefinition.cs` | Add `MaxIterations`, `StopKeyword`, `FeedbackMode` fields | Low |
| `Wally.Core/WallyRunResult.cs` | Add `Iteration` field | Low |
| `Wally.Core/WallyEnvironment.cs` | Add `RunAgentLoopAsync` entry point | Low |
| `Wally.Core/WallyCommands.cs` | Route to agent loop when loop params are set | Low |

---

## Benefits

- Bot pursues goals across multiple iterations without human re-prompting
- `MaxIterations` cap and `StopKeyword` prevent runaway loops
- Existing one-shot and pipeline paths unchanged
- `CancellationToken` honored at every iteration boundary

---

## Risks

- **Runaway loops** — mitigated by `MaxIterations` hard stop with low defaults (e.g., 5)
- **History bloat** — `Iteration > 0` turns suppressed by `ConversationLogger.GetRecentTurns`
- **`FeedbackMode.ReplacePrompt`** — discards original prompt after iteration 1; document usage clearly

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Implement `WallyAgentLoop` core class | High | ?? Not Started | @developer | 2024-01-18 | Core loop logic |
| Add loop configuration to `WallyLoopDefinition` | High | ?? Not Started | @developer | 2024-01-19 | JSON schema updates |
| Implement stop condition handlers | Medium | ?? Not Started | @developer | 2024-01-20 | Keyword and action-based stopping |
| Add feedback mode implementations | Medium | ?? Not Started | @developer | 2024-01-21 | Append/replace/custom combiners |
| Integration testing with sample loops | Medium | ?? Not Started | @qa | 2024-01-22 | Verify termination conditions |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] `WallyAgentLoop` implements all feedback modes correctly
- [ ] Stop conditions work reliably (keyword, no-actions, max-iterations)
- [ ] Loop configuration integrates with existing JSON schema
- [ ] `CancellationToken` support at iteration boundaries
- [ ] History bloat prevention verified

#### Should Have (Preferred for Quality)
- [ ] Comprehensive testing of edge cases (empty responses, malformed actions)
- [ ] Performance testing with longer iteration sequences
- [ ] Documentation for different feedback modes and use cases

#### Completion Checklist
- [ ] All loop functionality implemented and tested
- [ ] Integration with `HandleRunTypedAsync` complete
- [ ] Default `MaxIterations` values set appropriately
- [ ] Ready for Phase 3 (mailbox protocol) if needed
