# Autonomous Bot Gaps — Proposal

**Status**: In Progress (Phases 1-3 Complete)
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2025-07-17

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

The current Wally architecture executes single-shot and pre-scripted work well but has three structural gaps that prevent it from operating as a truly autonomous AI bot:

1. **No async execution path** — ~~`LLMWrapper.Execute` blocks the calling thread~~ **Status: ? COMPLETE**
2. **No autonomy loop** — ~~there is no plan?act?observe?re-plan cycle~~ **Status: ? COMPLETE — `WallyAgentLoop` with StopKeyword/NoActions/MaxIterations stop conditions, AppendResponse/ReplacePrompt feedback modes, CancellationToken at every iteration**
3. **Mailbox system is inert** — ~~no code processes or routes messages~~ **Status: ? COMPLETE — `send_message` writes to sender's Outbox; `process-mailboxes` reads Inbox ? prompts actor ? deletes; `route-outbox` delivers Outbox ? target Inbox**

---

## Resolution

Three independent workstreams delivered in priority order. Each is detailed in its own proposal.

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Async execution path | 2 days | None |
| 2 | Autonomy loop | 5-7 days | Phase 1 complete |
| 3 | Mailbox protocol | 2-3 days | Phase 1 complete |

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| ~~[AsyncExecutionProposal](../Archive/CompletedProposals/AsyncExecutionProposal.md)~~ | Child — Phase 1 | ? **COMPLETE** — archived to `../Archive/CompletedProposals/` |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Child — Phase 2 | Now unblocked — depends on Phase 1 async path |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Child — Phase 3 | Now unblocked — two commands: `process-mailboxes` + `route-outbox` |

---

## Impact

| System | Change | Risk Level |
|--------|--------|------------|
| `LLMWrapper` | Add async methods | Low |
| `WallyEnvironment` | Add async execution + agent loop | Low |
| `WallyCommands` | Add async variants + mailbox commands | Low |
| `ChatPanel` | Replace `Task.Run` with `await` | Low |
| Console commands | No change (sync wrappers) | None |
| Mailbox system | Two new commands using existing infrastructure | Low |

---

## Benefits

- **Phase 1**: UI never hangs; cancellation propagates end-to-end; console behavior identical
- **Phase 2**: Bot pursues goals across iterations without human re-prompting
- **Phase 3**: Actors process inbox messages and route responses via simple commands

---

## Risks

- **Async deadlock potential**: Mitigated by `.ConfigureAwait(false)` throughout `Wally.Core`
- **Runaway agent loops**: Mitigated by `MaxIterations` hard cap with low defaults
- **Circular messaging**: Mitigated by human-triggered processing (no daemon/auto-run)
- **Large inboxes**: Mitigated by warning when >10 messages; batching can be added later

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Phase 1 async execution | High | ? Complete | @architect | 2025-07-15 | ? All 4 layers async, sync wrappers, end-to-end cancellation |
| Phase 2 autonomy loop | Medium | ? Complete | @developer | 2025-07-17 | ? `WallyAgentLoop` with stop conditions + feedback modes |
| Phase 3 mailbox commands | Medium | ? Complete | @developer | 2025-07-17 | ? `send_message` ? Outbox; `process-mailboxes` + `route-outbox` implemented |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [x] All child proposals approved
- [x] Phase dependencies validated
- [x] Effort estimates confirmed by engineering team
- [x] Risk mitigation strategies approved

#### Should Have (Preferred for Quality)
- [ ] Performance impact analysis across all phases
- [ ] Integration testing strategy defined
- [ ] Rollback procedures for each phase

#### Completion Checklist
- [ ] All child proposals implemented
- [ ] End-to-end testing completed
- [ ] Documentation updated
- [ ] Status updated to "Implemented"
