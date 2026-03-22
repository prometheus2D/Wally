# Mailbox Protocol — Proposal

**Status**: Complete
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2025-07-15

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`Inbox/Outbox/Pending/Active` folders are scaffolded for every actor but no code reads, writes, or routes them. `send_message` can deliver a message to an actor's Inbox, but nothing processes those messages or routes responses back. The mailbox system is structurally complete but behaviourally inert.

---

## Resolution

Two new Wally commands that complete the mailbox lifecycle:

1. **`process-mailboxes`** — For each actor with Inbox messages: read all messages, feed them to the actor as a prompt, save the actor's response as Outbox message(s), delete the Inbox originals.
2. **`route-outbox`** — Scan all actors' Outbox folders, read the `to:` field from YAML front-matter, move each message to the target actor's Inbox, delete the Outbox original.

A runbook or manual sequence chains these: `process-mailboxes` then `route-outbox`. That's it.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 3 |
| ~~[AsyncExecutionProposal](./AsyncExecutionProposal.md)~~ | Depends on | ? **COMPLETE** — `ExecuteActorAsync` available for message processing |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Independent; agents can emit `send_message` actions from within a loop |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Implement `process-mailboxes` and `route-outbox` commands | 2-3 days | Async execution complete |

---

## Concepts

- **`send_message`**: Already implemented in `ActionDispatcher.ExecuteSendMessage`. Writes a YAML front-matter message file directly to a target actor's `Inbox/`. This is the "direct delivery" path — an actor's LLM response emits a `send_message` action block, and it arrives in the target's Inbox immediately.
- **`process-mailboxes`**: New WallyCommands verb. Iterates all actors, reads Inbox messages, calls `ExecuteActorAsync` with message content as prompt, saves the LLM response to the actor's `Outbox/` as a new message file, deletes the Inbox originals.
- **`route-outbox`**: New WallyCommands verb. Iterates all actors' Outbox folders, reads the `to:` YAML field, moves each file to the target actor's `Inbox/`, deletes the Outbox original. Messages with multiple recipients (comma-separated `to:` field) are copied to each target's Inbox.
- **Mailbox cycle**: A complete round is `process-mailboxes` ? `route-outbox`. Can be chained in a runbook or run manually.

---

## Implementation Status

> **Note**: `send_message` is already implemented in `ActionDispatcher.ExecuteSendMessage` using **YAML front-matter** format (`---` fenced metadata) with fields: `from`, `to`, `replyTo`, `subject`, `correlationId`, `timestamp`, `status`. Both new commands parse this same YAML format.

---

## Message Format

All messages use the existing YAML front-matter format established by `send_message`:

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

Example: 2024-01-15T14:30:00.000Z_a1b2c3d4_RequirementsReview.md
```

This matches the existing naming convention used by `ExecuteSendMessage`.

---

## Mailbox Lifecycle

### Complete Cycle

```
1. Message arrives in Actor's Inbox/
   (via send_message action OR via route-outbox delivery)

2. process-mailboxes runs:
   For each actor with Inbox/ messages:
     - Read all .md files from Inbox/
     - Concatenate as prompt context
     - Call ExecuteActorAsync with message content
     - Actor responds (may emit send_message actions ? direct delivery to other Inboxes)
     - Save actor's full response as Outbox/ message file (to: original sender via replyTo)
     - Delete processed Inbox/ files

3. route-outbox runs:
   For each actor's Outbox/ folder:
     - Read each .md file
     - Parse to: field from YAML front-matter
     - Copy file to target actor's Inbox/ (for each recipient if comma-separated)
     - Delete Outbox/ original
```

### Two delivery paths

| Path | Mechanism | When |
|------|-----------|------|
| **Direct delivery** | `send_message` action in LLM response | Actor explicitly sends a message during processing |
| **Response routing** | `route-outbox` command | System moves actor's response back to the original sender |

Both paths write to the target's `Inbox/`. Both use the same YAML front-matter format.

### Folder Usage

| Folder | Purpose | Lifecycle |
|--------|---------|-----------|
| **Inbox/** | Unprocessed messages waiting for the actor | Written by `send_message` or `route-outbox`; deleted by `process-mailboxes` |
| **Outbox/** | Actor's responses waiting to be routed | Written by `process-mailboxes`; deleted by `route-outbox` |
| **Active/** | Reserved for future use | Not used in v1 — kept for potential mid-processing state |
| **Pending/** | Reserved for future use | Not used in v1 — kept for potential error recovery |

### Multiple outbox messages from one input

An actor processing one inbox message may produce multiple outbox messages. This happens naturally: the actor's LLM response may contain multiple `send_message` action blocks (each creating a direct-delivery message to a different actor's Inbox). The `process_mailboxes` command also saves the actor's full response as a single Outbox message addressed back to the `replyTo` sender. So one inbox message can result in:
- 1 outbox message (the response back to the sender)
- 0+ direct-delivery messages (via `send_message` actions in the response)

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
5. Build a prompt: `"You have {n} new message(s) in your inbox. Review and respond to each.\n\n{message contents}"`
6. Call `ExecuteActorAsync` with the prompt
7. Process the response through `ActionDispatcher` (handles any `send_message` actions in the response)
8. Save the full response as an Outbox message file with `to:` set to the `replyTo`/`from` of the inbox message(s)
9. Delete all processed Inbox files
10. Print summary: `"Processed {n} messages for {actor}"`

**Edge cases**:
- Actor has no inbox messages ? skip silently
- Multiple inbox messages ? concatenate all into one prompt (actor sees all messages at once)
- `ExecuteActorAsync` fails ? leave Inbox files in place (no deletion on error), log error, continue to next actor

### `route-outbox`

```bash
wally route-outbox
```

**Behaviour**:
1. Iterate all actors in workspace
2. For each actor, check `Outbox/` for `.md` files
3. For each file, parse YAML front-matter to extract `to:` field
4. If `to:` contains comma-separated names, deliver to each target
5. Copy the file to each target actor's `Inbox/`
6. Delete the Outbox original
7. Print summary: `"Routed {n} messages from {actor}'s outbox"`

**Edge cases**:
- `to:` field missing or empty ? log warning, skip file (leave in Outbox)
- Target actor doesn't exist ? log warning, skip that recipient (leave in Outbox for that target)
- All recipients invalid ? file stays in Outbox

### Runbook Example

```wrb
# Full mailbox processing cycle
process-mailboxes
route-outbox
```

```wrb
# Multiple rounds of mailbox processing
process-mailboxes
route-outbox
process-mailboxes
route-outbox
```

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/WallyCommands.cs` | Add `process-mailboxes` and `route-outbox` verbs to `DispatchCommand` | Low |
| `Wally.Core/WallyCommands.cs` | Add `HandleProcessMailboxes` and `HandleRouteOutbox` methods | Medium |
| `Wally.Core/Mailbox/MailboxHelper.cs` | New file — YAML front-matter parsing utility for reading `to:`, `from:`, `replyTo:` fields | Low |

---

## Benefits

- **Two commands, complete lifecycle** — `process-mailboxes` + `route-outbox` is the entire system
- **Uses existing infrastructure** — `ExecuteActorAsync`, `ActionDispatcher`, YAML front-matter format
- **No new abstractions** — no MailboxRouter, no MailboxWatcher, no BatchContext, no daemon mode
- **Chainable** — runbooks can sequence multiple rounds of processing
- **Error-safe** — inbox files only deleted after successful processing; outbox files only deleted after successful delivery

---

## Risks

- **Message ordering**: Multiple inbox messages are concatenated into one prompt — actor sees them all at once. Order within the concatenation is filesystem order (alphabetical by filename, which is timestamp-prefixed, so chronological).
- **Large inboxes**: Many messages concatenated into one prompt could exceed LLM context limits. Mitigation: log a warning when inbox has >10 messages; future enhancement could batch.
- **Circular messaging**: Actor A sends to B, B responds to A, A responds to B, etc. Mitigation: not addressed in v1 — controlled by the human deciding when to run `process-mailboxes`.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| ~~Implement `send_message` action~~ | High | ? Complete | @developer | — | YAML front-matter format in `ActionDispatcher.ExecuteSendMessage` |
| Create YAML front-matter parser (`MailboxHelper`) | High | ?? Not Started | @developer | TBD | Extract `to:`, `from:`, `replyTo:`, `subject:` fields |
| Implement `process-mailboxes` command | High | ?? Not Started | @developer | TBD | Read Inbox ? prompt actor ? save Outbox ? delete Inbox |
| Implement `route-outbox` command | High | ?? Not Started | @developer | TBD | Read Outbox ? parse `to:` ? copy to target Inbox ? delete Outbox |
| Add verbs to `DispatchCommand` switch | Medium | ?? Not Started | @developer | TBD | `process-mailboxes`, `route-outbox` |
| Create example runbook for mailbox cycle | Low | ?? Not Started | @developer | TBD | `process-mailboxes` then `route-outbox` |
| Test end-to-end mailbox cycle | Low | ?? Not Started | @qa | TBD | Send ? Process ? Route ? Process |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] `process-mailboxes` reads all actors' Inbox messages, feeds to actor, saves response to Outbox, deletes Inbox originals
- [ ] `route-outbox` reads all actors' Outbox messages, parses `to:` field, copies to target Inbox, deletes Outbox originals
- [ ] Inbox files are only deleted after successful actor processing (error = files stay)
- [ ] Outbox files are only deleted after successful delivery to all recipients
- [ ] Commands work with existing `send_message` YAML front-matter format
- [ ] `send_message` actions in actor responses are dispatched normally (direct delivery)

#### Should Have (Preferred for Quality)
- [ ] Multiple recipients supported via comma-separated `to:` field
- [ ] Warning logged when inbox has >10 messages
- [ ] Summary output shows per-actor processing results
- [ ] Example runbook demonstrating full cycle

#### Completion Checklist
- [ ] Both commands registered in `DispatchCommand` and `_knownVerbs`
- [ ] YAML front-matter parser handles missing/malformed fields gracefully
- [ ] Existing `send_message` format unchanged
- [ ] No new external dependencies

---

## Open Questions & Recommendations

### 1. **How does `process-mailboxes` build the actor prompt from inbox messages?** ? Resolved

**Decision**: Concatenate all inbox messages into a single prompt with a header:

```
You have {n} new message(s) in your inbox. Review and respond to each.

--- Message 1: {subject} (from {from}) ---
{message body}

--- Message 2: {subject} (from {from}) ---
{message body}
```

**Rationale**: Simple. Actor sees all messages at once and can prioritise. Avoids per-message LLM calls which would be slow and expensive. If context limits become an issue, batching can be added later.

### 2. **What does the outbox response message look like?** ? Resolved

**Decision**: The `process-mailboxes` command saves the actor's full LLM response as an Outbox message with:
- `from:` = the processing actor
- `to:` = the `replyTo:` (or `from:`) field of the original inbox message
- `subject:` = `"Re: {original subject}"`
- `correlationId:` = same as original message's correlationId
- `status:` = `new`

If the actor had multiple inbox messages from different senders, one outbox message is created per unique `replyTo`/`from` sender. The response content for each is the full LLM response (since the actor saw all messages in one prompt, the response addresses all of them).

### 3. **Should `route-outbox` delete outbox files or keep them as audit trail?** ? Resolved

**Decision**: Delete. The outbox is a staging area, not an archive. The message is now in the target's Inbox — that's the source of truth. If audit trail is needed later, a `Sent/` folder can be added.

**Rationale**: Keeping outbox files leads to accumulation. The lifecycle should be clean: Inbox ? processed ? Outbox ? routed ? target Inbox. No lingering files.

### 4. **What about the Active/ and Pending/ folders?** ? Resolved

**Decision**: Not used in v1. Reserved for future use.

- **Active/**: Could be used for mid-processing state (move from Inbox ? Active before processing, then to Outbox on success). Not needed now because `process-mailboxes` is synchronous — if it fails, files stay in Inbox.
- **Pending/**: Could be used for error recovery (failed messages moved here for retry). Not needed now because failure = files stay in Inbox.

### 5. **Should there be a daemon / watch mode?** ? Resolved

**Decision**: No. Manual trigger only. Run `process-mailboxes` and `route-outbox` when you want to. Chain them in a runbook for convenience. Daemon mode is a separate concern if ever needed.

### 6. **What about message format — YAML front-matter vs RFC email headers?** ? Resolved

**Decision**: YAML front-matter. This is already implemented by `send_message` and is the canonical format. The proposal's original RFC-email-style headers were aspirational but the implementation went with YAML, which is simpler to parse and consistent with Wally's document conventions. All documentation should reference the YAML format.
