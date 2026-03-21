# Async Execution Path — Proposal

**Status**: Draft
**Author**: System Architecture Team  
**Created**: 2024-01-10
**Last Updated**: 2024-01-10

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`LLMWrapper.Execute` synchronously blocks the calling thread for the entire LLM call duration. Every layer above inherits this block. `ChatPanel` works around it with `Task.Run`, which offloads the block to a thread-pool thread but does not make the call genuinely async, and splits cancellation handling across two layers.

---

## Resolution

Add `ExecuteAsync` methods at each layer. Sync methods become one-line `GetAwaiter().GetResult()` wrappers — no logic is duplicated and no existing call site changes. `ChatPanel` drops its `Task.Run` wrapper and `await`s directly.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 1 |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Depended on by | Agent loop builds on the async path |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Depended on by | Mailbox dispatch uses async execution |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Add async methods to all layers | 3-5 days | None |

---

## Concepts

- `ExecuteAsync`: Async version of every `Execute` method in the call chain
- `ConfigureAwait(false)`: Used throughout `Wally.Core` to prevent deadlock
- Sync wrapper: `=> ExecuteAsync(...).GetAwaiter().GetResult()`

---

## Layer-by-layer Changes

#### `LLMWrapper` (`Wally.Core/LLMWrappers/LLMWrapper.cs`)
- Add `ExecuteAsync(processedPrompt, sourcePath, model, logger, CancellationToken)` — contains all logic
- Replace `RunProcess` with `RunProcessAsync`: `await process.WaitForExitAsync(cancellationToken)`
- `Execute(...)` becomes: `=> ExecuteAsync(...).GetAwaiter().GetResult()`

#### `WallyEnvironment` (`Wally.Core/WallyEnvironment.cs`)
- Add `ExecutePromptAsync(prompt, modelOverride, wrapperOverride, loopName, iteration, skipHistory, CancellationToken)`
- Add `ExecuteActorAsync(actor, prompt, modelOverride, wrapperOverride, loopName, iteration, skipHistory, CancellationToken)`
- Sync methods become one-line wrappers

#### `WallyCommands` (`Wally.Core/WallyCommands.cs`)
- Add `HandleRunTypedAsync(env, prompt, actorName, model, loopName, wrapper, noHistory, CancellationToken)`
- Add `RunPipelineAsync(env, prompt, loopDef, loopLabel, model, wrapper, noHistory, CancellationToken)`
- Sync methods become one-line wrappers

#### `ChatPanel` (`Wally.Forms/Controls/ChatPanel.cs`)
- **Before**: `await Task.Run(() => WallyCommands.HandleRunTyped(..., token), token)`
- **After**: `await WallyCommands.HandleRunTypedAsync(..., cancellationToken: token)`

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/LLMWrappers/LLMWrapper.cs` | Add `ExecuteAsync`; `Execute` becomes wrapper | Low |
| `Wally.Core/WallyEnvironment.cs` | Add async execution methods | Low |
| `Wally.Core/WallyCommands.cs` | Add async variants; sync become wrappers | Low |
| `Wally.Forms/Controls/ChatPanel.cs` | Replace `Task.Run` with direct `await` | Low |
| `Wally.Console/Program.cs` | **Unchanged** | None |

---

## Benefits

- UI thread never blocks; `ChatPanel` cancellation propagates end-to-end to `process.Kill`
- `async`/`await` composable throughout — foundation for agent loops and mailbox protocol
- Console and runbook behavior identical to current
- No performance impact on existing sync call paths

---

## Risks

- **Sync-over-async deadlock** — mitigated by `.ConfigureAwait(false)` throughout `Wally.Core`
- **`WaitForExitAsync` cancellation** — explicit `process.Kill(entireProcessTree: true)` required in catch block

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Implement `LLMWrapper.ExecuteAsync` | High | ?? Not Started | @developer | 2024-01-12 | Core async foundation |
| Add async methods to `WallyEnvironment` | High | ?? Not Started | @developer | 2024-01-13 | Depends on LLMWrapper |
| Update `WallyCommands` with async variants | High | ?? Not Started | @developer | 2024-01-14 | Depends on environment |
| Refactor `ChatPanel` to use direct await | Medium | ?? Not Started | @frontend | 2024-01-15 | Remove Task.Run wrapper |
| Add comprehensive cancellation tests | Medium | ?? Not Started | @qa | 2024-01-16 | Validate token propagation |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] All layers have async methods with proper cancellation support
- [ ] Sync methods are simple one-line wrappers (no duplicated logic)
- [ ] `ChatPanel` uses direct await instead of `Task.Run`
- [ ] Console behavior unchanged
- [ ] Cancellation propagates end-to-end to process termination

#### Should Have (Preferred for Quality)
- [ ] All async methods use `.ConfigureAwait(false)` in `Wally.Core`
- [ ] Comprehensive tests for cancellation scenarios
- [ ] Performance comparison showing no regression for sync paths

#### Completion Checklist
- [ ] All async methods implemented and tested
- [ ] No sync-over-async deadlocks in testing
- [ ] UI responsiveness verified
- [ ] Ready for Phase 2 (autonomy loop) implementation
