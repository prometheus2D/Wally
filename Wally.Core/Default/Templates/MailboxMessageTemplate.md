# Mailbox Message Template

> Reference: Mailbox Messages define simple inter-actor communication within the Wally file-based mailbox system.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Actors communicating through the mailbox system |
| **Scope** | Message structure, basic routing metadata, free-text content |
| **Out of Scope** | MailboxRouter implementation; complex workflows; structured parameters |
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
- Maintain compatibility with the four-folder mailbox architecture.

---

## Mailbox System Basics

### Folder Structure
Each actor has four folders that define the complete message lifecycle:

| Folder | Purpose |
|--------|---------|
| **Inbox/** | New messages arrive here |
| **Active/** | Message currently being processed |  
| **Outbox/** | Responses written here after execution |
| **Pending/** | Failed messages waiting for retry |

### Message Lifecycle
```
Actor A emits send_message action
  ? ActionDispatcher writes Inbox/B/{timestamp}-A-subject.md  
  ? MailboxRouter moves file ? Active/B/
  ? Actor B processes message
  ? Response written ? Outbox/B/
  ? (if replyTo) Reply written ? Inbox/replyTo/
```

### Failure Handling
- On error: message moved from Active/ ? Pending/ with error header
- Use `wally repair` to move Pending/ messages back to Inbox/ for retry

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| Actor Definitions | Informs | Messages route between defined actors |
| Mailbox Architecture | Implements | Messages follow four-folder protocol |
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
---

# [Subject]

**From**: [ActorName]  
**To**: [ActorName]  
**Reply To**: [ActorName if different from from]  

*Correlation ID: [correlationId]*
```

**Required Fields**:
- `from`: Sending actor name (must match loaded actor)
- `to`: Target actor name (must match loaded actor)  
- `subject`: Short description for filename and identification

**Optional Fields**:
- `replyTo`: Actor to receive response (defaults to `from`)
- `correlationId`: UUID linking related messages (auto-generated if missing)

### Message Content
The main body of the message - free-text Markdown content.

**Guidelines**:
- Write clearly for the target actor
- Include necessary context and background
- Be specific about what you're asking or communicating
- Keep it conversational and human-readable

### Expected Response
For messages that need a reply:

**Response Needed**: Yes/No  
**Response Type**: [Analysis | Decision | Status | Acknowledgment]  
**Deadline**: [When response is needed]

### Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Deliver message to target inbox | High | ? Complete | @system | 2024-01-15 | File written successfully |
| Process incoming message | High | ?? In Progress | @target-actor | 2024-01-15 | Actor analyzing content |
| Generate response | Medium | ?? Not Started | @target-actor | 2024-01-15 | Pending analysis completion |
| Send reply message | Medium | ?? Not Started | @target-actor | 2024-01-16 | Via send_message action |

**Priority**: `High | Medium | Low`  
**Status**: `?? Blocked | ?? In Progress | ? Complete | ?? Not Started`

### Acceptance Criteria

#### Must Have
- [ ] Message file written to target actor's Inbox/ folder
- [ ] Required YAML front-matter fields populated
- [ ] Message content is clear and actionable
- [ ] File follows naming convention: `{timestamp}-{from}-{subject}.md`

#### Should Have  
- [ ] Correlation ID included for message threading
- [ ] Response expectations clearly stated if reply needed
- [ ] Context provided for target actor to understand request

#### Completion Checklist
- [ ] Message successfully moved through lifecycle (Inbox ? Active ? Outbox)
- [ ] Response generated if required
- [ ] Any errors handled via Pending/ folder and repair process

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

ActionDispatcher converts this to a message file in the target's Inbox/.

---

## Common Message Patterns

### Request for Analysis
```markdown
---
from: Engineer
to: BusinessAnalyst  
subject: RequirementsReview
replyTo: Engineer
---

# Requirements Review Request

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
---

# Implementation Status Update

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
---

# Stakeholder Interview Required

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
| File names | `{timestamp}-{from}-{subject}.md` | `2024-01-15T14-30-00Z-Engineer-FeasibilityCheck.md` |
| Actor names | Exact case match to loaded actors | `BusinessAnalyst`, `Engineer` |
| Timestamps | ISO-8601 UTC, colons?hyphens | `2024-01-15T14-30-00Z` |
| Correlation IDs | UUID format | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| YAML front-matter | Standard YAML | `key: value` format |
| Message body | Free-text Markdown | No structured parameters |

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

`{timestamp}-{fromActor}-{subject}.md` — ISO-8601 UTC timestamp with colons replaced by hyphens, sender actor name, hyphenated subject.

Examples:
- `2024-01-15T14-30-00Z-Engineer-FeasibilityCheck.md`
- `2024-01-15T16-45-30Z-BusinessAnalyst-RequirementsReview.md`  
- `2024-01-16T09-15-45Z-Stakeholder-StatusUpdate.md`

### Reply Messages
Replies include the original correlationId and use the reply timestamp in the filename, not the original message timestamp.