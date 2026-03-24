# Mailbox Protocol — Proposal

**Status**: ? Implemented
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2025-07-17
**Archived**: 2025-07-17

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`Inbox/Outbox/Pending/Active` folders were scaffolded for every actor but no code read, wrote, or routed them. `send_message` delivered directly to the target's Inbox, bypassing the Outbox entirely. Nothing processed Inbox messages or routed responses.

---

## Resolution

Two new commands and one change to `send_message`:

1. **`send_message`** — writes to the **sending actor's own Outbox** (staging area)
2. **`process-mailboxes`** — reads each actor's Inbox, feeds messages as a prompt, deletes on success
3. **`route-outbox`** — reads each actor's Outbox, parses `to:` field, copies to target Inbox, deletes original

One delivery path: **Outbox ? `route-outbox` ? Inbox**.

---

## Implementation Summary

All deliverables complete:

| File | Change |
|------|--------|
| `Wally.Core/ActionDispatcher.cs` | ? `send_message` writes to sender's Outbox |
| `Wally.Core/Mailbox/MailboxHelper.cs` | ? Created — YAML front-matter parser (`ParseFrontMatter` + `ExtractBody`) |
| `Wally.Core/WallyCommands.cs` | ? `process-mailboxes` + `route-outbox` commands added |
| `Wally.Core/Default/Runbooks/MailboxCycle.wrb` | ? Example runbook created |

Features delivered:
- Comma-separated `to:` for multiple recipients
- Warning when inbox has >10 messages
- Inbox files left in place on processing failure (safe retry)
- Outbox files deleted only after all recipients delivered
