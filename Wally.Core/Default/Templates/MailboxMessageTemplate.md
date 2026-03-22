# Mailbox Message Template

> Reference: Mailbox Messages define simple inter-actor communication within the Wally file-based mailbox system.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Actors communicating through the mailbox system |
| **Scope** | Message structure, basic routing metadata, free-text content |
| **Out of Scope** | Complex workflows; structured parameters |
| **Maintenance** | Update when basic message schema changes |
| **Todo Tracking** | Track message delivery and response tasks |
| **Acceptance Criteria** | Message delivered and processed successfully |
| **Related Documents** | Messages reference conversations and spawned work |

---

## Objectives

- Provide simple, file-based communication between actors in the Wally system.
- Ensure messages have minimal required routing information for delivery.
- Keep message format lightweight and human-readable.
- Support basic message threading through correlation IDs.
- Maintain compatibility with the mailbox folder architecture.

---

## Mailbox System Basics

### Folder Structure
Each actor has four folders:

| Folder | Purpose | Used by |
|--------|---------|---------|
| **Inbox/** | Unprocessed messages waiting for the actor | Written by `send_message` or `route-outbox`; deleted by `process-mailboxes` |
| **Outbox/** | Actor's responses waiting to be routed to recipients | Written by `process-mailboxes`; deleted by `route-outbox` |
| **Active/** | Reserved for future use (mid-processing state) | Not used in v1 |
| **Pending/** | Reserved for future use (error recovery) | Not used in v1 |

### Message Lifecycle
```
1. Message arrives in actor's Inbox/
   (via send_message action OR via route-outbox delivery)

2. process-mailboxes command runs:
   - Reads all Inbox/ messages for each actor
   - Feeds message content to actor as prompt via ExecuteActorAsync
   - Actor responds (may emit send_message actions ? direct delivery)
   - Saves actor's response as Outbox/ message (to: original sender)
   - Deletes processed Inbox/ files

3. route-outbox command runs:
   - Reads each Outbox/ message
   - Parses to: field from YAML front-matter
   - Copies to target actor's Inbox/
   - Deletes Outbox/ original
```

### Two Delivery Paths

| Path | Mechanism | When |
|------|-----------|------|
| **Direct delivery** | `send_message` action in LLM response | Actor explicitly sends a message during any prompt |
| **Response routing** | `route-outbox` command | System moves actor's response back to the original sender |

### Error Handling
- On `process-mailboxes` failure: Inbox files stay in place (not deleted), error logged, processing continues to next actor
- On `route-outbox` failure (invalid recipient): Outbox file stays in place, warning logged

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| Actor Definitions | Informs | Messages route between defined actors |
| Mailbox Architecture | Implements | Messages follow folder protocol |
| send_message Actions | Spawns | Messages created by send_message action blocks |

---

## Required Sections

### Header
```markdown
---
from: [ActorName]
to: [ActorName]
subject: [BriefSubject]
replyTo: [ActorName]
correlationId: [UUID]
timestamp: [ISO-8601]
status: new
---
```

**Required Fields**:
- `from`: Sending actor name (must match loaded actor)
- `to`: Target actor name (must match loaded actor); comma-separated for multiple recipients
- `subject`: Short description for filename and identification

**Optional Fields**:
- `replyTo`: Actor to receive response (defaults to `from`)
- `correlationId`: UUID linking related messages (auto-generated if missing)
- `timestamp`: ISO-8601 UTC timestamp (auto-generated)
- `status`: Message status (default: `new`)

### Message Content
The main body of the message — free-text Markdown content after the YAML front-matter.

**Guidelines**:
- Write clearly for the target actor
- Include necessary context and background
- Be specific about what you're asking or communicating
- Keep it conversational and human-readable

---

## send_message Action Format

Actors emit this action block to send messages:

```
name: send_message
to: BusinessAnalyst
subject: FeasibilityCheck
replyTo: Engineer
body: |
  Please review the following proposal for business alignment.
  Specifically assess whether the scope matches requirements.
```

`ActionDispatcher.ExecuteSendMessage` converts this to a message file in the target's Inbox/ with YAML front-matter.

---

## Common Message Patterns

### Request for Analysis
```markdown
---
from: Engineer
to: BusinessAnalyst  
subject: RequirementsReview
replyTo: Engineer
correlationId: a1b2c3d4
timestamp: 2024-01-15T14:30:00.000Z
status: new
---

Please review the attached requirements document and provide feedback on:
- Business alignment with stakeholder needs
- Completeness of functional requirements  
- Any missing acceptance criteria

The document is located at: ./Documents/UserAuthRequirements.md
```

### Status Update
```markdown
---
from: Engineer
to: BusinessAnalyst
subject: ImplementationComplete
correlationId: b2c3d4e5
timestamp: 2024-01-15T16:45:00.000Z
status: new
---

The user authentication feature has been completed and deployed to staging.

Key deliverables finished:
- Login/logout functionality
- Password reset workflow
- Multi-factor authentication
- Admin user management

Ready for user acceptance testing.
```

### Work Handoff
```markdown
---
from: BusinessAnalyst
to: RequirementsExtractor
subject: StakeholderInterviewNeeded
replyTo: BusinessAnalyst
correlationId: c3d4e5f6
timestamp: 2024-01-16T09:15:00.000Z
status: new
---

Please conduct an interview with the Product Owner to clarify the following requirements gaps:

1. User role permissions - what can each role access?
2. Data retention policies - how long do we keep user data?
3. Integration requirements - which external systems need access?

Interview should be completed by end of week for project timeline.
```

---

## Formatting Rules

| Element | Format | Example |
|---------|--------|---------|
| File names | `{timestamp}_{correlationId}_{subject}.md` | `2024-01-15T14:30:00.000Z_a1b2c3d4_RequirementsReview.md` |
| Actor names | Exact case match to loaded actors | `BusinessAnalyst`, `Engineer` |
| Timestamps | ISO-8601 UTC | `2024-01-15T14:30:00.000Z` |
| Correlation IDs | Short UUID (8 chars) | `a1b2c3d4` |
| YAML front-matter | Standard YAML fenced with `---` | `key: value` format |
| Message body | Free-text Markdown | After the closing `---` |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Structured key-value parameters in body | Keep body as free-text Markdown |
| Complex message routing or workflows | Simple point-to-point messaging |
| Missing required front-matter fields | Always include from, to, subject |
| Vague or unclear message content | Be specific about requests and context |
| Sending to non-existent actors | Validate target actor exists first |
| Large binary content in messages | Reference external files instead |

---

## File Naming

`{timestamp}_{correlationId}_{subject}.md` — matches the naming convention used by `ActionDispatcher.ExecuteSendMessage`.

Examples:
- `2024-01-15T14:30:00.000Z_a1b2c3d4_RequirementsReview.md`
- `2024-01-15T16:45:30.000Z_b2c3d4e5_ImplementationComplete.md`
- `2024-01-16T09:15:45.000Z_c3d4e5f6_StakeholderInterviewNeeded.md`

### Reply Messages
Replies preserve the original `correlationId` for threading and use a new timestamp in the filename.