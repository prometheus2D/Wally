# Next Steps — Prioritised Work Queue

**Last Updated**: 2025-07-15  
**Context**: Documentation refactor complete. AsyncExecution + TextEditorIntegration + EnhancedTextEditor proposals archived as complete. RunbookScriptingLanguage cancelled (handled elsewhere). All remaining documentation updated.

---

## Dependency Graph

```
AutonomyLoopProposal (UNBLOCKED — highest priority)
  ??? DocumentationWorkflowProposal (blocked by autonomy loop + mailbox)

MailboxProtocolProposal (UNBLOCKED — highest priority, parallel with autonomy loop)
  ??? DocumentationWorkflowProposal (blocked by autonomy loop + mailbox)

AutonomousBotGapsProposal (parent — complete when all children complete)
```

---

## Recommended Next Prompt

### Priority 1: Implement AutonomyLoopProposal

**Why**: Now unblocked by async completion. Enables self-driving actor iteration — the core "autonomous bot" capability. Required before DocumentationWorkflowProposal can be implemented.

**Scope**: Implement `WallyAgentLoop` class with stop conditions, feedback modes, and `MaxIterations` support.

**Files to modify/create**:
1. `Wally.Core/WallyAgentLoop.cs` (new) — Core loop class wrapping actor + wrapper; iterates until stop condition or max iterations
2. `Wally.Core/WallyLoopDefinition.cs` — Add `MaxIterations`, `StopKeyword`, `FeedbackMode` fields to existing JSON schema
3. `Wally.Core/WallyRunResult.cs` — Add `Iteration` field
4. `Wally.Core/WallyEnvironment.cs` — Add `RunAgentLoopAsync` entry point
5. `Wally.Core/WallyCommands.cs` — Route to agent loop when loop params include `MaxIterations` or `FeedbackMode`

**Key design constraints** (from proposal):
- Stop condition priority: StopKeyword ? no-actions ? MaxIterations
- `FeedbackMode`: `AppendResponse`, `ReplacePrompt`, or custom
- `CancellationToken` honoured at every iteration boundary
- History bloat prevention: `Iteration > 0` turns handled appropriately
- Existing single-shot and pipeline paths must remain unchanged

**Suggested prompt**:
```
Implement the AutonomyLoopProposal. Read the proposal at 
Wally.Core/Default/Projects/Proposals/AutonomyLoopProposal.md for full 
specification. Create WallyAgentLoop class. Add MaxIterations, StopKeyword, 
FeedbackMode to WallyLoopDefinition. Add RunAgentLoopAsync to 
WallyEnvironment. Route HandleRunTypedAsync to agent loop when loop params 
are configured. Honour CancellationToken at every iteration boundary. 
Existing single-shot and pipeline paths must remain unchanged.
```

---

### Priority 1 (parallel): Implement MailboxProtocolProposal (remaining work)

**Why**: Also unblocked by async completion. `send_message` is already implemented; remaining work is `process_all_mailboxes`, MailboxRouter for Inbox?Active?Outbox lifecycle, and optional MailboxWatcher.

**Scope**: Implement message routing, lifecycle management, and `process_all_mailboxes` action.

**Suggested prompt**:
```
Implement the remaining MailboxProtocolProposal work. Read the proposal at 
Wally.Core/Default/Projects/Proposals/MailboxProtocolProposal.md. 
send_message is already implemented in ActionDispatcher.ExecuteSendMessage 
using YAML front-matter format. Implement: (1) process_all_mailboxes action 
in ActionDispatcher, (2) WallyMessage envelope model for YAML parsing, 
(3) MailboxRouter for Inbox?Active?Outbox lifecycle with failure?Pending, 
(4) optional MailboxWatcher with FileSystemWatcher + polling fallback.
```

---

## Priority 2: DocumentationWorkflowProposal

**After autonomy loop + mailbox routing are done**. Create DocumentationReflection loop definition and update actor prompts for reflection patterns.

---

## Completed Work (for reference)

| Proposal | Archive Location |
|----------|-----------------|
| ScrollbarAndCommandArgParsing | `Archive/CompletedProposals/ScrollbarAndCommandArgParsingProposal.md` |
| WorkspaceMemory | `Archive/CompletedProposals/WorkspaceMemoryProposal.md` |
| TextEditorIntegration | `Archive/CompletedProposals/TextEditorIntegrationProposal.md` |
| ChatDefaultsManager | `Archive/CompletedProposals/ChatDefaultsManagerProposal.md` |
| AsyncExecution | `Archive/CompletedProposals/AsyncExecutionProposal.md` |
| EnhancedTextEditor | `Archive/CompletedProposals/EnhancedTextEditorAndRunbookLanguageProposal.md` |

## Cancelled Work (for reference)

| Proposal | Archive Location | Reason |
|----------|-----------------|--------|
| RunbookScriptingLanguage | `Archive/CancelledProposals/RunbookScriptingLanguageProposal.md` | Handled elsewhere; `.wrb` files remain simple command lists |
