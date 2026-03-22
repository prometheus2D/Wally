# Mailbox Protocol — Proposal

**Status**: Complete
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2025-07-15

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`Inbox/Outbox/Pending/Active` folders are scaffolded for every actor but no code reads, writes, or routes them. `send_message` currently delivers directly to the target's Inbox, bypassing the Outbox entirely. Nothing processes Inbox messages or routes responses. The mailbox system is structurally complete but behaviourally inert.

---

## Resolution

Two new Wally commands and one change to `send_message`:

1. **Change `send_message`** — Write to the **sending actor's own Outbox** instead of the target's Inbox. The Outbox is the staging area for all outgoing messages.
2. **`process-mailboxes`** — For each actor with Inbox messages: read all messages, feed them to the actor as a prompt. The actor responds naturally and may emit `send_message` actions (which write to their Outbox). Delete the Inbox originals after successful processing.
3. **`route-outbox`** — Scan all actors' Outbox folders, read the `to:` field from YAML front-matter, copy each message to the target actor's Inbox, delete the Outbox original.

One delivery path: **Outbox ? `route-outbox` ? Inbox**. That's it.

A runbook chains these: `process-mailboxes` then `route-outbox`.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 3 |
| ~~[AsyncExecutionProposal](./AsyncExecutionProposal.md)~~ | Depends on | ? **COMPLETE** — `ExecuteActorAsync` available |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Independent; agents can emit `send_message` from within a loop |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Change `send_message` + implement `process-mailboxes` + `route-outbox` | 2-3 days | Async execution complete |

---

## Concepts

- **One delivery path**: All messages flow through Outbox ? `route-outbox` ? Inbox. There is no direct-to-inbox delivery.
- **`send_message`**: Writes to the **sending actor's own Outbox**. This is a change from the current behaviour (which writes to the target's Inbox). The message sits in the sender's Outbox until `route-outbox` delivers it.
- **`process-mailboxes`**: Reads each actor's Inbox, feeds messages as a prompt, actor thinks and responds (may emit `send_message` actions which go to their Outbox), deletes processed Inbox files.
- **`route-outbox`**: Reads each actor's Outbox, parses `to:` field, copies to target's Inbox, deletes Outbox original.
- **Mailbox cycle**: `process-mailboxes` ? `route-outbox`. Run manually or via runbook.

---

## Implementation Status

> `send_message` is currently implemented in `ActionDispatcher.ExecuteSendMessage` writing to the **target's Inbox**. This needs to change to write to the **sending actor's Outbox** instead. The YAML front-matter format (`from`, `to`, `replyTo`, `subject`, `correlationId`, `timestamp`, `status`) stays the same.

---

## Message Format

All messages use YAML front-matter:

```markdown
---
from: Engineer
to: BusinessAnalyst
replyTo: Engineer
subject: RequirementsReview
correlationId: a1b2c3d4
timestamp: 2024-01-15T14:30:00.000Z
status: new
---

Please review the attached requirements document and provide feedback on business alignment...
```

### File Naming

```
{timestamp}_{correlationId}_{subject}.md
```

---

## Mailbox Lifecycle

### One Path

```
Actor emits send_message action
  ? Message written to sender's Outbox/

wally route-outbox
  ? Reads each Outbox/ message
  ? Parses to: field
  ? Copies to target actor's Inbox/
  ? Deletes Outbox/ original

wally process-mailboxes
  ? Reads each actor's Inbox/ messages
  ? Feeds to actor as prompt
  ? Actor responds (may emit send_message ? goes to their Outbox/)
  ? Deletes processed Inbox/ files

wally route-outbox
  ? Delivers new Outbox/ messages to recipients' Inboxes
```

### Folder Usage

| Folder | Purpose | Written by | Deleted by |
|--------|---------|-----------|------------|
| **Inbox/** | Messages waiting for this actor to read | `route-outbox` only | `process-mailboxes` |
| **Outbox/** | Messages this actor wants to send | `send_message` action | `route-outbox` |
| **Active/** | Reserved for future use | — | — |
| **Pending/** | Reserved for future use | — | — |

### Multiple outbox messages from one input

An actor processing inbox messages may create multiple outbox messages — they simply emit multiple `send_message` action blocks in their response. Each creates a separate file in the actor's Outbox. `route-outbox` delivers each one to its respective recipient.

---

## Commands

### `process-mailboxes`

```bash
wally process-mailboxes
```

**Behaviour**:
1. Iterate all actors in workspace
2. For each actor, check `Inbox/` for `.md` files
3. If no messages, skip actor
4. Read all Inbox message files, concatenate their content
5. Build prompt: `"You have {n} new message(s) in your inbox. Review and respond to each.\n\n{message contents}"`
6. Call `ExecuteActorAsync` with the prompt
7. Process response through `ActionDispatcher` (any `send_message` actions write to this actor's Outbox)
8. Delete all processed Inbox files
9. Print summary

**Edge cases**:
- No inbox messages ? skip silently
- Multiple inbox messages ? concatenate into one prompt
- `ExecuteActorAsync` fails ? leave Inbox files in place, log error, continue to next actor

### `route-outbox`

```bash
wally route-outbox
```

**Behaviour**:
1. Iterate all actors in workspace
2. For each actor, check `Outbox/` for `.md` files
3. For each file, parse YAML front-matter to extract `to:` field
4. If `to:` contains comma-separated names, deliver to each target
5. Copy file to each target actor's `Inbox/`
6. Delete Outbox original
7. Print summary

**Edge cases**:
- `to:` field missing/empty ? log warning, leave in Outbox
- Target actor doesn't exist ? log warning, leave in Outbox
- All recipients invalid ? file stays in Outbox

### Runbook

```wrb
# Full mailbox cycle
process-mailboxes
route-outbox
```

```wrb
# Multiple rounds
process-mailboxes
route-outbox
process-mailboxes
route-outbox
```

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/ActionDispatcher.cs` | Change `send_message` to write to sender's Outbox instead of target's Inbox | Low |
| `Wally.Core/WallyCommands.cs` | Add `process-mailboxes` and `route-outbox` verbs + handler methods | Medium |
| `Wally.Core/Mailbox/MailboxHelper.cs` | New — YAML front-matter parser for `to:`, `from:`, `replyTo:`, `subject:` | Low |

---

## Benefits

- **One delivery path** — Outbox ? `route-outbox` ? Inbox. No confusion about direct vs routed.
- **Uses existing infrastructure** — `ExecuteActorAsync`, `ActionDispatcher`, YAML front-matter
- **No new abstractions** — no MailboxRouter, no MailboxWatcher, no BatchContext, no daemon mode
- **Chainable** — runbooks sequence processing rounds
- **Error-safe** — Inbox files only deleted after success; Outbox files only deleted after delivery

---

## Risks

- **Message ordering**: Multiple inbox messages concatenated chronologically (timestamp-prefixed filenames).
- **Large inboxes**: Many messages in one prompt could exceed LLM context. Mitigation: warn when >10 messages.
- **Circular messaging**: A?B?A?B. Mitigation: human controls when to run commands.

---

## Todo Tracker

| Task | Priority | Status | Owner | Notes |
|------|----------|--------|-------|-------|
| ~~Implement `send_message` action~~ | High | ? Complete | @developer | Needs change: write to sender's Outbox |
| Change `send_message` to write to sender's Outbox | High | ?? Not Started | @developer | Currently writes to target's Inbox |
| Create YAML front-matter parser (`MailboxHelper`) | High | ?? Not Started | @developer | Extract `to:`, `from:`, etc. |
| Implement `process-mailboxes` command | High | ?? Not Started | @developer | Read Inbox ? prompt actor ? delete Inbox |
| Implement `route-outbox` command | High | ?? Not Started | @developer | Read Outbox ? copy to target Inbox ? delete Outbox |
| Add verbs to `DispatchCommand` + `_knownVerbs` | Medium | ?? Not Started | @developer | |
| Create example runbook | Low | ?? Not Started | @developer | `process-mailboxes` then `route-outbox` |
| Test end-to-end cycle | Low | ?? Not Started | @qa | |

---

## Acceptance Criteria

#### Must Have
- [ ] `send_message` writes to the sending actor's Outbox (not target's Inbox)
- [ ] `process-mailboxes` reads Inbox, prompts actor, deletes Inbox files on success
- [ ] `route-outbox` reads Outbox, parses `to:`, copies to target Inbox, deletes Outbox
- [ ] Inbox files only deleted after successful processing
- [ ] Outbox files only deleted after successful delivery
- [ ] YAML front-matter format unchanged

#### Should Have
- [ ] Multiple recipients via comma-separated `to:`
- [ ] Warning when inbox has >10 messages
- [ ] Per-actor summary output
- [ ] Example runbook

#### Completion Checklist
- [ ] Commands in `DispatchCommand` and `_knownVerbs`
- [ ] YAML parser handles missing/malformed fields
- [ ] No new external dependencies

---

## Open Questions — All Resolved

### 1. How does `process-mailboxes` build the prompt? ?

Concatenate all inbox messages into one prompt:
```
You have {n} new message(s). Review and respond to each.

--- Message 1: {subject} (from {from}) ---
{body}

--- Message 2: {subject} (from {from}) ---
{body}
```

### 2. Where does `send_message` write? ?

To the **sending actor's own Outbox**. `route-outbox` delivers it. One path.

### 3. Should `route-outbox` delete or keep outbox files? ?

Delete. Outbox is staging, not archive. If audit trail is needed later, add a `Sent/` folder.

### 4. Active/ and Pending/ folders? ?

Not used in v1. Reserved for future use.

### 5. Daemon / watch mode? ?

No. Manual trigger only.

### 6. Message format? ?

YAML front-matter. Already implemented by `send_message`. Canonical format.
