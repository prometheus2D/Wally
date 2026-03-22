# Mailbox System Architecture

> *"An actor that can only talk to itself is not an agent — it's a script."*

*Template: [../Templates/ArchitectureTemplate.md](../Templates/ArchitectureTemplate.md)*

---

> **?? Implementation Status**: This document describes the **target architecture**. As of 2025-07-16:
> - ? `send_message` action exists in `ActionDispatcher` — but currently writes to the **target's Inbox** (needs change to write to sender's Outbox)
> - ?? `process-mailboxes` command — not yet implemented
> - ?? `route-outbox` command — not yet implemented
> - ?? `MailboxHelper` YAML parser — not yet implemented
> - See [MailboxProtocolProposal](Projects/Proposals/MailboxProtocolProposal.md) for implementation tracking.

---

## Core Principle

Every actor owns a private, file-based mailbox — folders on disk that define the lifecycle of a message. No actor may directly invoke another. All cross-actor communication flows through the mailbox with **one delivery path**: messages are written to the sender's Outbox, then `route-outbox` delivers them to the recipient's Inbox.

| Benefit | Detail |
|---------|--------|
| Decoupling | Actor A has no compile-time or runtime reference to Actor B |
| Auditability | Every message is a file on disk; the full chain is inspectable at any time |
| Simplicity | One delivery path: Outbox ? `route-outbox` ? Inbox |
| Human control | Processing only happens when explicitly triggered — no background automation |

---

## Folder Structure

Each actor's directory contains four mailbox folders:

```
.wally/Actors/<ActorName>/
    Inbox/      — messages waiting for this actor to read
    Outbox/     — messages this actor wants to send
    Active/     — reserved for future use
    Pending/    — reserved for future use
```

All four folders are created by `WallyHelper.CreateMailboxFolders` and are guaranteed to exist after `wally setup` or `wally load`.

### Folder Lifecycle

| Folder | Written by | Deleted by | Purpose |
|--------|-----------|------------|---------|
| **Inbox/** | `route-outbox` command only | `process-mailboxes` command | Messages waiting to be read by the actor |
| **Outbox/** | `send_message` action | `route-outbox` command | Messages the actor wants to send to others |
| **Active/** | — | — | Reserved for future mid-processing state |
| **Pending/** | — | — | Reserved for future error recovery |

---

## One Delivery Path

All messages follow the same path. No exceptions.

```
Actor emits send_message action
  ? Written to sender's Outbox/

wally route-outbox
  ? Parses to: field from YAML front-matter
  ? Copies to target actor's Inbox/
  ? Deletes Outbox/ original

wally process-mailboxes
  ? Reads actor's Inbox/ messages
  ? Feeds to actor as prompt
  ? Actor responds (may emit send_message ? goes to their Outbox/)
  ? Deletes processed Inbox/ files
```

There is no direct-to-inbox delivery. `route-outbox` is the **only** mechanism that writes to any actor's Inbox.

---

## Message Format

Every message is a Markdown file with YAML front-matter.

**File naming**: `{timestamp}_{correlationId}_{subject}.md`

**File content**:

```markdown
---
from: Engineer
to: BusinessAnalyst
subject: FeasibilityCheck
replyTo: Engineer
correlationId: a1b2c3d4
timestamp: 2024-01-15T14:30:00.000Z
status: new
---

Please review the following proposal for business alignment...
```

**Front-matter fields**:

| Field | Required | Description |
|-------|----------|-------------|
| `from` | Yes | Sending actor name |
| `to` | Yes | Target actor name; comma-separated for multiple recipients |
| `subject` | Yes | Short description; used in file name and logs |
| `replyTo` | No | Actor to receive response; defaults to `from` |
| `correlationId` | No | Links related messages; auto-generated |
| `timestamp` | No | ISO-8601 UTC; auto-generated |
| `status` | No | Message status; defaults to `new` |

---

## `send_message` Action

Emitted by an actor in their LLM response. Writes to the **sending actor's own Outbox**.

**Action block format**:

```action
name: send_message
to: BusinessAnalyst
subject: FeasibilityCheck
replyTo: Engineer
body: |
  Please review the attached proposal for business alignment.
```

**Dispatcher behaviour**:
1. Validates `to` actor exists ? error if not
2. Generates `correlationId` and `timestamp`
3. Writes message file to **sender's Outbox/** (not target's Inbox)
4. Returns confirmation

---

## Commands

### `process-mailboxes`

```bash
wally process-mailboxes
```

For each actor with Inbox messages:
1. Read all `.md` files from Inbox
2. Concatenate as prompt with header
3. Call `ExecuteActorAsync` — actor responds naturally
4. Response dispatched through `ActionDispatcher` (any `send_message` actions ? actor's Outbox)
5. Delete processed Inbox files

### `route-outbox`

```bash
wally route-outbox
```

For each actor's Outbox folder:
1. Read each `.md` file
2. Parse `to:` from YAML front-matter
3. Copy to target actor's `Inbox/` (each recipient if comma-separated)
4. Delete Outbox original

### Runbook Usage

```wrb
# Full mailbox cycle
process-mailboxes
route-outbox
```

---

## Authority Model

| Component | Responsibility |
|-----------|----------------|
| `ActionDispatcher.ExecuteSendMessage` | Writes `send_message` to **sender's Outbox** |
| `WallyCommands.HandleProcessMailboxes` | Reads Inbox ? prompts actor ? deletes Inbox |
| `WallyCommands.HandleRouteOutbox` | Reads Outbox ? copies to target Inbox ? deletes Outbox |
| `Mailbox/MailboxHelper` | Parses YAML front-matter fields from message files |
| `WallyHelper` | Creates and validates mailbox folder structure |

---

## Patterns

**? Pattern**: One delivery path — Outbox ? `route-outbox` ? Inbox.
**? Anti-pattern**: Writing directly to another actor's Inbox — bypasses routing.

**? Pattern**: All inbox messages concatenated into one prompt — actor sees everything at once.
**? Anti-pattern**: Per-message LLM calls — slow, expensive, loses cross-message context.

**? Pattern**: Message body is free-text Markdown; routing data in YAML front-matter only.
**? Anti-pattern**: Structured parameters in message body.

**? Pattern**: Inbox files only deleted after successful processing.
**? Anti-pattern**: Delete-then-process — data loss on failure.

---

## Diagnostics

| Observable | Location | Meaning |
|------------|----------|---------|
| Files in `Inbox/` | `Actors/<Name>/Inbox/` | Unprocessed messages — run `process-mailboxes` |
| Files in `Outbox/` | `Actors/<Name>/Outbox/` | Undelivered messages — run `route-outbox` |
| Files in `Active/` | `Actors/<Name>/Active/` | Should be empty in v1 |
| Files in `Pending/` | `Actors/<Name>/Pending/` | Should be empty in v1 |

---

## Design Principles

1. **One delivery path** — Outbox ? `route-outbox` ? Inbox. Always.
2. **File is the unit of communication** — no in-memory queues.
3. **`send_message` writes to sender's Outbox** — never directly to another actor's Inbox.
4. **Two commands complete the lifecycle** — `process-mailboxes` + `route-outbox`.
5. **Human-triggered** — no daemon mode, no automatic processing.
6. **Error-safe** — files only deleted after success.
7. **`correlationId` provides traceability** — across message chains.
