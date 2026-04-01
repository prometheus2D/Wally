# Mailbox Message Template

> Reference: File-based inter-actor communication format for the Wally mailbox system.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Actors communicating through the mailbox system |
| **Scope** | Message structure, routing metadata, free-text content |
| **Out of Scope** | Complex workflows; structured parameters |

---

## Mailbox Folders

| Folder | Purpose | Written by | Deleted by |
|--------|---------|-----------|------------|
| **Inbox/** | Messages waiting for actor to read | `route-outbox` | `process-mailboxes` |
| **Outbox/** | Messages actor wants to send | `send_message` action | `route-outbox` |

Lifecycle: `send_message ? Outbox/ ? route-outbox ? Inbox/ ? process-mailboxes ? deleted`

---

## Message Format

### YAML Front Matter
```yaml
---
from: [ActorName]
to: [ActorName]
subject: [BriefSubject]
replyTo: [ActorName]        # optional, defaults to from
correlationId: [8-char hex]  # auto-generated
timestamp: [ISO-8601 UTC]    # auto-generated
status: new
---
```

### Body
Free-text Markdown after the closing `---`.

---

## send_message Action

```
name: send_message
to: BusinessAnalyst
subject: FeasibilityCheck
body: |
  Please review the proposal for business alignment.
```

---

## File Naming

`{timestamp}_{correlationId}_{subject}.md`

Example: `2024-01-15T14:30:00.000Z_a1b2c3d4_RequirementsReview.md`

Replies preserve the original `correlationId` with a new timestamp.