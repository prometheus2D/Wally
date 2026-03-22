# Autonomous Bot Implementation Plan

**Status**: In Progress (Phase 1 Complete)
**Owner**: Lead Engineer
**Started**: 2025-07-15
**Target Completion**: TBD

*Template: [../../Templates/ImplementationPlanTemplate.md](../../Templates/ImplementationPlanTemplate.md)*

---

## Summary

Autonomous Bot system implementation: 3 phases enabling async execution, mailbox processing, and documentation workflow automation.

---

## Proposal Breakdown

| Phase | Days | Deps | Deliverable | Status |
|-------|------|------|-------------|--------|
| Phase 1 | 2 | None | Async execution path | ? **COMPLETE** |
| Phase 2 | 2-3 | Phase 1 | Mailbox protocol: `process-mailboxes` + `route-outbox` commands | ? Partial (`send_message` implemented; now UNBLOCKED) |
| Phase 3 | 3 | Phase 2 | Documentation workflow automation | ?? Not Started |

---

## Phase Dependencies

```mermaid
flowchart LR
    P1[Phase 1: Async Execution ?] --> P2[Phase 2: Mailbox Protocol]
    P2 --> P3[Phase 3: Documentation Workflow]
```

---

## Detailed Steps

### Phase 1: Async Execution Path ? COMPLETE
1. ~~MODIFY `Wally.Core/LLMWrappers/LLMWrapper.cs`~~ — ? `ExecuteAsync` with `WaitForExitAsync` + `process.Kill(entireProcessTree: true)` on cancellation; `Execute` is sync wrapper
2. ~~MODIFY `Wally.Core/WallyEnvironment.cs`~~ — ? `ExecutePromptAsync` + `ExecuteActorAsync` with `.ConfigureAwait(false)`; sync wrappers
3. ~~MODIFY `Wally.Core/WallyCommands.cs`~~ — ? `HandleRunTypedAsync` with `TextWriter? output` + `RunPipelineAsync`; sync wrappers
4. ~~MODIFY `Wally.Forms/Controls/ChatPanel.cs`~~ — ? Direct `await HandleRunTypedAsync` — no `Task.Run`
5. ~~MODIFY `Wally.Core/ActionDispatcher.cs`~~ — ? `send_message` action already implemented with YAML front-matter
6. ~~TEST~~ — ? Async execution works in ChatPanel; console behavior unchanged

### Phase 2: Mailbox Protocol — `process-mailboxes` + `route-outbox`
1. CREATE `Wally.Core/Mailbox/MailboxHelper.cs` — Simple YAML front-matter parser: extract `to:`, `from:`, `replyTo:`, `subject:`, `correlationId:` fields from message files
2. MODIFY `Wally.Core/WallyCommands.cs` — Add `process-mailboxes` and `route-outbox` verbs to `DispatchCommand` switch + `_knownVerbs` array
3. MODIFY `Wally.Core/WallyCommands.cs` — Add `HandleProcessMailboxes`: iterate actors ? read Inbox ? build prompt ? `ExecuteActorAsync` ? dispatch actions ? save response to Outbox ? delete Inbox originals
4. MODIFY `Wally.Core/WallyCommands.cs` — Add `HandleRouteOutbox`: iterate actors ? read Outbox ? parse `to:` ? copy to target Inbox ? delete Outbox originals
5. ~~MODIFY `Wally.Core/ActionDispatcher.cs`~~ — ? Already implemented — `ExecuteSendMessage` writes YAML front-matter messages to target actor's Inbox
6. CREATE example runbook: `process-mailboxes` then `route-outbox`
7. TEST: Verify full cycle — send_message ? process-mailboxes ? route-outbox ? message arrives in target Inbox

### Phase 3: Documentation Workflow Automation
1. CREATE `Wally.Core/Default/Loops/DocumentationReflection.json` — Loop definition with convergence detection
2. MODIFY `Wally.Core/Actors/BusinessAnalyst/actor.json` — Add documentation reflection guidance to prompts  
3. MODIFY `Wally.Core/Actors/Engineer/actor.json` — Add technical documentation review patterns
4. MODIFY `Wally.Core/Actors/RequirementsExtractor/actor.json` — Add requirements completeness review
5. CREATE `Wally.Core/Default/Docs/DocumentationWorkflowGuide.md` — Usage patterns and best practices
6. TEST: Verify loop convergence detection prevents infinite cycles
7. TEST: Verify todo task persistence and cross-batch coordination
8. TEST: Validate complete documentation reflection workflow with real project data

---

## Timeline

| Days | Phase | Parallel? | Status |
|------|-------|-----------|--------|
| 1-2 | Phase 1: Async Execution | No | ? **COMPLETE** |
| 3-4 | Phase 2: Mailbox Protocol | No | ? Partial (now UNBLOCKED) |
| 5-7 | Phase 3: Documentation Workflow | No | ?? Not Started |

---

## Resources

| Devs | Scope | Tools |
|------|-------|-------|
| 1 Senior Engineer | Full-stack .NET 8, async patterns, file I/O | Visual Studio, Git, Mermaid |

---

## Risks

| Risk | Mitigation | Status |
|------|-----------|--------|
| Sync-over-async deadlock in batch processing | Use `.ConfigureAwait(false)` throughout Wally.Core | ? Implemented |
| Large inboxes exceeding LLM context limits | Log warning when >10 messages; future batching if needed | ?? Planned |
| Circular messaging (A?B?A?B…) | Controlled by human deciding when to run `process-mailboxes` | ? By design |
| Documentation loop infinite cycles | Implement convergence detection with max iteration limits | ?? Prevention |

---

## Todo Tracker

| Task | Phase | Priority | Status | Owner | Due Date | Notes |
|------|--------|----------|--------|-------|----------|-------|
| ~~Implement async LLMWrapper.ExecuteAsync method~~ | Phase 1 | High | ? Complete | @lead-engineer | 2025-07-15 | `ExecuteAsync` + `RunProcessAsync` + kill on cancel |
| ~~Add async methods to WallyEnvironment~~ | Phase 1 | High | ? Complete | @lead-engineer | 2025-07-15 | `ExecutePromptAsync` + `ExecuteActorAsync` |
| ~~Add HandleRunTypedAsync to WallyCommands~~ | Phase 1 | High | ? Complete | @lead-engineer | 2025-07-15 | With `TextWriter? output` parameter |
| ~~Update ChatPanel to use direct await~~ | Phase 1 | Medium | ? Complete | @lead-engineer | 2025-07-15 | No `Task.Run` — direct `await` |
| Create MailboxHelper YAML parser | Phase 2 | High | ?? Not Started | @lead-engineer | TBD | Extract `to:`, `from:`, `replyTo:` fields |
| Implement `process-mailboxes` command | Phase 2 | High | ?? Not Started | @lead-engineer | TBD | Read Inbox ? prompt ? Outbox ? delete |
| Implement `route-outbox` command | Phase 2 | High | ?? Not Started | @lead-engineer | TBD | Parse `to:` ? copy to Inbox ? delete |
| Create example mailbox runbook | Phase 2 | Low | ?? Not Started | @lead-engineer | TBD | `process-mailboxes` + `route-outbox` |
| Design DocumentationReflection loop definition | Phase 3 | High | ?? Not Started | @lead-engineer | TBD | Workflow automation |
| Update actor prompts for documentation awareness | Phase 3 | Medium | ?? Not Started | @lead-engineer | TBD | Actor capability enhancement |
| Test complete workflow with real project data | Phase 3 | High | ?? Not Started | @lead-engineer | TBD | End-to-end validation |
| Write usage documentation and best practices | Phase 3 | Low | ?? Not Started | @lead-engineer | TBD | User guidance |

---

## Acceptance Criteria

### Must Have (Required for Completion)
- [x] Async execution works without blocking UI thread
- [ ] `process-mailboxes` reads Inbox ? prompts actor ? saves Outbox ? deletes Inbox
- [ ] `route-outbox` reads Outbox ? parses `to:` ? copies to target Inbox ? deletes Outbox
- [ ] Mailbox protocol enables file-based actor communication using existing YAML format
- [ ] Documentation workflow automates reflection and task creation
- [ ] All integration tests pass for new functionality
- [x] Console behavior remains unchanged for single-user operations
- [x] Cancellation propagates end-to-end to LLM processes

### Should Have (Preferred for Quality)
- [ ] Multiple recipients supported via comma-separated `to:` field
- [ ] Warning logged when inbox has >10 messages
- [ ] Documentation loop has convergence detection preventing infinite cycles
- [ ] Performance benchmarks show no regression in single-user scenarios

### Completion Checklist
- [ ] All three phases completed with deliverables validated
- [ ] Code review completed and approved for all changes
- [ ] Integration testing completed in staging environment
- [x] Documentation updated to reflect new async capabilities
- [ ] Status updated to "Complete"

---

## Related Plans

| Plan | Relationship | Notes |
|------|--------------|-------|
| ~~[AsyncExecutionProposal](../Proposals/AsyncExecutionProposal.md)~~ | Implements | ? **COMPLETE** — archived to `../Archive/CompletedProposals/` |
| [MailboxProtocolProposal](../Proposals/MailboxProtocolProposal.md) | Implements | Phase 2 — two commands: `process-mailboxes` + `route-outbox` |
| [DocumentationWorkflowProposal](../Proposals/DocumentationWorkflowProposal.md) | Implements | Phase 3 implementation source |
| [AutonomousBotGapsProposal](../Proposals/AutonomousBotGapsProposal.md) | Implements | Parent proposal coordinating all phases |

---

## References

| Document | Relationship |
|----------|-------------|
| [ImplementationPlanTemplate](../../Templates/ImplementationPlanTemplate.md) | Follows | Document structure and formatting |
| [ProposalTemplate](../../Templates/ProposalTemplate.md) | Follows | Source proposal specifications |