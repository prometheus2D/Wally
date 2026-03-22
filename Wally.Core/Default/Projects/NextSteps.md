# Next Steps — Prioritised Work Queue

**Last Updated**: 2025-07-15  
**Context**: Documentation refactor complete. Mailbox proposal simplified to two commands (`process-mailboxes` + `route-outbox`). No MailboxRouter/Watcher/daemon — just simple Wally commands.

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

**Why**: Also unblocked by async completion. `send_message` is already implemented; remaining work is two new Wally commands.

**Scope**: Two commands that complete the mailbox lifecycle:
1. `process-mailboxes` — Read each actor's Inbox ? feed to actor as prompt ? save response to Outbox ? delete Inbox originals
2. `route-outbox` — Read each actor's Outbox ? parse `to:` YAML field ? copy to target Inbox ? delete Outbox originals

**Files to modify/create**:
1. `Wally.Core/Mailbox/MailboxHelper.cs` (new) — YAML front-matter parser for `to:`, `from:`, `replyTo:`, `subject:` fields
2. `Wally.Core/WallyCommands.cs` — Add `process-mailboxes` and `route-outbox` verbs + handler methods

**Key design constraints** (from proposal):
- Parse existing YAML front-matter format from `send_message` — do not change the format
- Inbox files only deleted after successful processing (error = files stay in place)
- Outbox files only deleted after successful delivery to all recipients
- Multiple recipients via comma-separated `to:` field
- Actor responses dispatched through `ActionDispatcher` (handles `send_message` actions in response)
- One outbox response per unique `replyTo`/`from` sender when actor has multiple inbox messages

**Suggested prompt**:
```
Implement the remaining MailboxProtocolProposal work. Read the proposal at 
Wally.Core/Default/Projects/Proposals/MailboxProtocolProposal.md. 
send_message is already implemented in ActionDispatcher.ExecuteSendMessage 
using YAML front-matter format. Implement: (1) MailboxHelper.cs to parse 
YAML front-matter fields, (2) process-mailboxes command in WallyCommands 
that reads Inbox ? prompts actor ? saves response to Outbox ? deletes 
Inbox, (3) route-outbox command that reads Outbox ? parses to: field ? 
copies to target Inbox ? deletes Outbox. No MailboxRouter, no 
MailboxWatcher, no daemon mode — just two simple commands.
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
