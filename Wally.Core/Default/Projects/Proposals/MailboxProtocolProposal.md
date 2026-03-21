# Mailbox Protocol — Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2024-01-10
**Last Updated**: 2024-01-10

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`Inbox/Outbox/Pending/Active` folders are scaffolded for every actor but no code reads, writes, or routes them. Multi-agent handoff is structurally defined but behaviourally unimplemented. Actors cannot trigger each other without tight pipeline coupling.

---

## Resolution

Implement a simple file-based inter-actor message protocol that works like email. Messages are processed as single-action Wally loops that trigger all actors to process their inboxes. Use standard email formatting for easy metadata parsing. Outbox messages are stored as "emails waiting to be sent" for future delivery implementation.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [AutonomousBotGapsProposal](./AutonomousBotGapsProposal.md) | Parent | Extracted from parent as Phase 3 |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depends on | Message processing uses `ExecuteActorAsync` |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Message processing triggers through loop system |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Email-like mailbox processing with unified loops | 3-5 days | Async execution complete |

---

## Concepts

- **Message processing as Wally loop**: Single-action loop that triggers all actors to process their inboxes
- **Actions are loops**: No distinction — actions are just loops that do one thing and don't continue
- **Email-like format**: Standard email headers (To, From, Subject) for easy metadata parsing
- **Outbox as "unsent email"**: Messages stored in standard email format, ready for future delivery
- **Unified trigger**: One message processing action triggers all actors simultaneously

---

## Email-Like Message Format

### Standard Email Headers
```markdown
To: BusinessAnalyst
From: Engineer  
Subject: Requirements Review Request
Reply-To: Engineer
Message-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Date: 2024-01-15T14:30:00Z

Please review the attached requirements document and provide feedback on business alignment...
```

### File Structure (Same as Email)
```
{timestamp}-{fromActor}-{subject}.md

Example: 2024-01-15T14-30-00Z-Engineer-RequirementsReview.md
```

**Email-Style Metadata in File**:
- **To**: Target actor (required)
- **From**: Sending actor (required)  
- **Subject**: Brief description (required)
- **Reply-To**: Who should receive reply (optional, defaults to From)
- **Message-ID**: Correlation UUID (auto-generated)
- **Date**: ISO-8601 timestamp (auto-generated)

---

## Message Processing Loop Architecture

### Single Message Processing Action/Loop
```json
{
  "name": "ProcessMailboxes", 
  "description": "Trigger all actors to process their inbox messages",
  "actorName": "System",
  "maxIterations": 1,
  "startPrompt": "Process all pending mailbox messages",
  "actions": ["process_all_mailboxes"]
}
```

### How It Works
1. **Trigger**: `wally run --loop ProcessMailboxes` OR daemon detects new messages
2. **Single action**: `process_all_mailboxes` action executes
3. **All actors process**: Each actor with inbox messages gets executed
4. **Email-like flow**: Inbox ? Active ? Outbox (like email client)

### No Distinction Between Actions and Loops
- `send_message` = single-action loop that sends one message
- `process_mailboxes` = single-action loop that processes all inboxes  
- `analyze_requirements` = single-action loop that analyzes requirements
- All use same infrastructure, just different `maxIterations` and continuation logic

---

## Implementation Approach

### Enhanced `send_message` Action (Single-Action Loop)
```csharp
// In ActionDispatcher.cs - creates email-like messages
case "send_message":
    var to = GetRequiredParameter("to");
    var subject = GetRequiredParameter("subject");
    var body = GetRequiredParameter("body");
    var from = currentActor.Name;
    var replyTo = GetOptionalParameter("replyTo") ?? from;
    
    var messageId = Guid.NewGuid().ToString();
    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
    var filename = $"{timestamp}-{from}-{subject.Replace(" ", "")}.md";
    
    var emailMessage = CreateEmailLikeMessage(to, from, subject, body, replyTo, messageId);
    WriteToActorInbox(to, filename, emailMessage);
    return $"Message sent to {to}: {subject}";

private string CreateEmailLikeMessage(string to, string from, string subject, string body, string replyTo, string messageId)
{
    return $@"To: {to}
From: {from}
Subject: {subject}
Reply-To: {replyTo}
Message-ID: {messageId}
Date: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}

{body}";
}
```

### `process_all_mailboxes` Action (Single-Action Loop)
```csharp
case "process_all_mailboxes":
    var results = new List<string>();
    
    foreach (var actor in workspace.Actors)
    {
        var inboxPath = Path.Combine(actor.DirectoryPath, "Inbox");
        if (!Directory.Exists(inboxPath)) continue;
        
        var messages = Directory.GetFiles(inboxPath, "*.md");
        foreach (var messagePath in messages)
        {
            var result = ProcessSingleMessage(actor, messagePath);
            results.Add(result);
        }
    }
    
    return $"Processed {results.Count} messages across all actors";
```

### Runbooks Stay Simple
Runbooks just run Wally commands:
```json
{
  "name": "DailyMailboxCheck",
  "steps": [
    {"command": "wally run --loop ProcessMailboxes"},
    {"command": "wally run --actor Monitor \"Report mailbox status\""}
  ]
}
```

---

## Email-Like File Lifecycle

```
Actor A: wally run --action send_message
  ? Creates email-like message file
Actor B's Inbox/{timestamp}-A-{subject}.md (email format)
  ? wally run --loop ProcessMailboxes OR daemon trigger  
Move Inbox/ ? Active/ (like email client moving to "processing")
  ? Parse email headers, execute actor with message body
Actor B processes message, may send replies via send_message
  ? Response written to Outbox/ (email ready to be sent)
Active/ cleaned up (email processing complete)
```

### Outbox Messages (Unsent Emails)
- **Same format**: To, From, Subject, Message-ID, Date headers
- **Same structure**: Ready to be moved directly to target actor's Inbox/
- **Future delivery**: Code will eventually move Outbox/ messages to target Inbox/ folders
- **Audit trail**: Shows what each actor has "sent" but not yet delivered

---

## Unified Command Structure

### Single `run` Command for Everything
```bash
# Current behavior - actor execution
wally run "analyze user requirements"
wally run --actor Engineer "review this code"

# Loop execution (actions are just single-iteration loops)  
wally run --loop ProcessMailboxes        # Message processing loop
wally run --loop AnalyzeRequirements     # Analysis loop
wally run --action send_message          # Single-action loop

# Runbook execution
wally run --runbook DailyWorkflow        # Runs sequence of wally commands
```

### Message Processing Integration
```bash
# Manual message processing
wally run --loop ProcessMailboxes

# Daemon mode (future)
wally watch --loop ProcessMailboxes --interval 30s

# Single actor mailbox
wally run --actor Engineer --loop ProcessMailboxes
```

---

## Impact

| File | Change | Risk Level |
|------|--------|------------|
| `Wally.Core/ActionDispatcher.cs` | Add `send_message` and `process_all_mailboxes` actions | Low |
| `Wally.Core/WallyCommands.cs` | Enhance unified `run` command routing | Low |  
| `Wally.Core/WallyLoop.cs` | Clarify that actions are single-iteration loops | Low |
| `Wally.Console/Program.cs` | Update `run` command option parsing | Low |

---

## Benefits

- **Email-familiar format**: Everyone understands To, From, Subject headers
- **Easy metadata parsing**: Standard email headers make code parsing simple
- **Unified architecture**: Actions and loops use same infrastructure  
- **Simple runbooks**: Just sequences of `wally run` commands
- **Future-ready outbox**: Messages stored as "unsent emails" for delivery
- **Single trigger**: One action processes all actor mailboxes simultaneously

---

## Risks

- **File parsing complexity**: Email header parsing needs to be robust
- **Message ordering**: Multiple messages may process simultaneously
- **Outbox accumulation**: Undelivered messages may accumulate over time

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Implement email-like message format creation | High | ?? Not Started | @developer | 2024-01-16 | Standard email headers |
| Add `send_message` action with email formatting | High | ?? Not Started | @developer | 2024-01-17 | Single-action loop |
| Create `process_all_mailboxes` action | High | ?? Not Started | @developer | 2024-01-18 | Trigger all actors |
| Enhance `run` command for unified routing | Medium | ?? Not Started | @developer | 2024-01-19 | --loop, --action options |
| Add email header parsing utilities | Medium | ?? Not Started | @developer | 2024-01-20 | To, From, Subject extraction |
| Test email-like message flow end-to-end | Low | ?? Not Started | @qa | 2024-01-21 | Inbox ? Active ? Outbox |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] `send_message` creates email-formatted messages with standard headers
- [ ] `process_all_mailboxes` triggers all actors to process their inboxes
- [ ] Email headers (To, From, Subject, Message-ID, Date) easily parseable by code
- [ ] Outbox messages stored in same email format for future delivery
- [ ] `wally run --loop` and `wally run --action` work identically (actions are single-iteration loops)

#### Should Have (Preferred for Quality)  
- [ ] Unified `run` command routes correctly to loops, actions, and runbooks
- [ ] Email header parsing robust and error-tolerant
- [ ] File naming prevents conflicts and maintains chronological order
- [ ] Message processing integrates seamlessly with existing execution infrastructure

#### Completion Checklist
- [ ] All message functionality works through standard Wally loop infrastructure
- [ ] Email format consistent and easily readable by both humans and code
- [ ] Outbox messages ready for future automatic delivery implementation
- [ ] Actions and loops unified under same architectural pattern

---

## Open Questions & Recommendations

### 1. **Email Header Standards**
**Question**: Should we follow RFC 5322 email standards exactly or simplified version?
**Recommendation**: Simplified but compatible — use standard header names (To, From, Subject) but don't require all RFC fields.

### 2. **Message-ID Format**
**Question**: Use GUID, timestamp-based, or RFC-compliant Message-ID?
**Recommendation**: GUID for simplicity, with format like `{guid}@wally.local` for email compatibility.

### 3. **Reply Threading**
**Question**: Should replies include In-Reply-To header for threading?
**Recommendation**: Yes, add `In-Reply-To: {original-message-id}` for conversation threading.

### 4. **Outbox Delivery Timing**
**Question**: When should future outbox delivery be implemented?
**Recommendation**: Separate proposal after this phase — outbox provides audit trail and staging area for now.

**Email Header Template**:
```markdown
To: {targetActor}
From: {sourceActor}
Subject: {subject}
Reply-To: {replyToActor}
Message-ID: {guid}@wally.local
Date: {iso8601timestamp}
In-Reply-To: {originalMessageId}  // Only for replies

{messageBody}
