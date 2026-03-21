# Autonomous Bot Gaps — Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2024-01-10

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

The current Wally architecture executes single-shot and pre-scripted work well but has three structural gaps that prevent it from operating as a truly autonomous AI bot:

1. **No async execution path** — `LLMWrapper.Execute` blocks the calling thread for the full LLM call duration. `ChatPanel` works around it with `Task.Run`, which is not genuinely async and splits cancellation handling.
2. **No autonomy loop** — there is no plan?act?observe?re-plan cycle. `WallyPipeline` runs N steps and stops. `MaxIterations` is declared in `WallyConfig` but never consumed.
3. **Mailbox system is inert** — `Inbox/Outbox/Pending/Active` folders exist for every actor but no code reads, writes, or routes them.

---

## Resolution

Three independent workstreams delivered in priority order. Each is detailed in its own proposal.

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Async execution path | 3-5 days | None |
| 2 | Autonomy loop | 5-7 days | Phase 1 complete |
| 3 | Mailbox protocol | 5-7 days | Phase 1 complete |

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Child — Phase 1 | Async path; no existing call sites change |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Child — Phase 2 | Depends on Phase 1 async path |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Child — Phase 3 | Depends on Phase 1; independent of Phase 2 |

---

## Impact

| System | Change | Risk Level |
|--------|--------|------------|
| `LLMWrapper` | Add async methods | Low |
| `WallyEnvironment` | Add async execution + agent loop | Low |
| `WallyCommands` | Add async variants + routing | Low |
| `ChatPanel` | Replace `Task.Run` with `await` | Low |
| Console commands | No change (sync wrappers) | None |
| Mailbox system | New protocol implementation | Medium |

---

## Benefits

- **Phase 1**: UI never hangs; cancellation propagates end-to-end; console behavior identical
- **Phase 2**: Bot pursues goals across iterations without human re-prompting
- **Phase 3**: Actors become independent agents triggered by file drops

---

## Risks

- **Async deadlock potential**: Mitigated by `.ConfigureAwait(false)` throughout `Wally.Core`
- **Runaway agent loops**: Mitigated by `MaxIterations` hard cap with low defaults
- **File system reliability**: Mitigated by polling fallback for mailbox watching

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Phase 1 async execution design review | High | ?? Not Started | @architect | 2024-01-15 | Review async patterns |
| Phase 2 autonomy loop validation | Medium | ?? Not Started | @product | 2024-01-20 | Validate loop termination |
| Phase 3 mailbox protocol security review | Medium | ?? Not Started | @security | 2024-01-25 | File-based messaging risks |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] All child proposals approved
- [ ] Phase dependencies validated
- [ ] Effort estimates confirmed by engineering team
- [ ] Risk mitigation strategies approved

#### Should Have (Preferred for Quality)
- [ ] Performance impact analysis across all phases
- [ ] Integration testing strategy defined
- [ ] Rollback procedures for each phase

#### Completion Checklist
- [ ] All child proposals implemented
- [ ] End-to-end testing completed
- [ ] Documentation updated
- [ ] Status updated to "Implemented"
