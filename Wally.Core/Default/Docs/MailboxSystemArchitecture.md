# Mailbox System Architecture

> *"An actor that can only talk to itself is not an agent — it's a script."*

*Template: [../Templates/ArchitectureTemplate.md](../Templates/ArchitectureTemplate.md)*

---

## Core Principle

Every actor owns a private, file-based mailbox — four folders on disk that define the complete lifecycle of a message. No actor may directly invoke another. All cross-actor communication flows through the mailbox: an actor emits a `send_message` action block; `ActionDispatcher` writes the message file into the target's `Inbox/`; `MailboxRouter` picks it up and drives execution.

| Benefit | Detail |
|---------|--------|
| Decoupling | Actor A has no compile-time or runtime reference to Actor B |
| Auditability | Every message is a file on disk; the full chain is inspectable at any time |
| Fault tolerance | Failed messages land in `Pending/` and can be retried via `repair` |
| Daemon mode | `wally watch` runs `MailboxRouter` continuously without coupling to any UI |
| **Stable concurrency** | **Batch processing ensures all actors see stable inbox state during iteration** |

---

## Folder Structure

Each actor's directory contains four mailbox folders:

```
.wally/Actors/<ActorName>/
    Inbox/      — new messages arrive here
    Outbox/     — responses written here after execution
    Pending/    — failed messages waiting for retry
    Active/     — message currently being processed
```

All four folders are created by `WallyHelper.CreateMailboxFolders` and are guaranteed to exist after `wally setup` or `wally load`.

---

## **Batch Concurrency Model**

To ensure stable concurrency when multiple actors are processing messages simultaneously, the mailbox system operates in **discrete batch iterations**:

### **Batch Processing Cycle**

```
BATCH ITERATION N:
1. All actors snapshot their current Inbox/ contents (immutable batch)
2. All actors process their batch concurrently:
   - Messages moved Inbox/ ? Active/ (per actor, atomically)
   - Actor execution via ExecuteActorAsync()  
   - Outputs accumulated in memory/temp files
3. After ALL actors complete their batch:
   - All Outbox/ messages committed atomically
   - All reply messages written to target Inbox/ folders
   - Active/ files cleaned up
4. Next iteration begins with fresh Inbox/ snapshots
```

### **Concurrency Guarantees**

| Guarantee | Implementation |
|-----------|----------------|
| **Stable inbox view** | Each actor processes a snapshot of inbox state at iteration start |
| **No mid-iteration interference** | New messages arrive in Inbox/ but don't affect current batch |
| **Atomic output commitment** | All outbox messages written simultaneously after all actors finish |
| **Consistent message ordering** | Messages within a batch are processed by timestamp, across batches by iteration |

### **Failure Isolation**

- If any actor fails during batch processing, **only that actor's** messages move to `Pending/`
- Other actors in the batch complete normally and commit their outputs
- Failed messages are retried in the next batch iteration via `repair` or auto-retry

---

## Message Format

Every message is a Markdown file with YAML front-matter.

**File naming**: `{timestamp}-{fromActor}-{subject}.md`
- `timestamp`: UTC ISO-8601, colons replaced with `-`, e.g. `2024-01-15T14-30-00Z`
- `fromActor`: sender actor name
- `subject`: single-word or hyphenated topic, e.g. `FeasibilityCheck`, `CodeReview-AuthModule`

**File content**:

```markdown
---
from: Engineer
to: BusinessAnalyst
subject: FeasibilityCheck
replyTo: Engineer
correlationId: a1b2c3d4
batchId: batch-2024-01-15T14-30-00Z
---

Please review the following proposal for business alignment...
[message body — free-text Markdown]
```

**Front-matter fields**:

| Field | Required | Description |
|-------|----------|-------------|
| `from` | Yes | Sending actor name (must match a loaded actor) |
| `to` | Yes | Target actor name (must match a loaded actor) |
| `subject` | Yes | Short description; used in file name and logs |
| `replyTo` | No | Actor name that should receive the response; defaults to `from` |
| `correlationId` | No | Opaque string linking messages in a chain; auto-generated if absent |
| **`batchId`** | **Auto** | **Batch identifier for grouping related processing cycles** |

---

## Message Lifecycle (Batch Mode)

```
BATCH ITERATION START:
  ? MailboxRouter.StartBatchIteration()
  ? All actors: snapshot current Inbox/ ? BatchContext
  ? 
CONCURRENT ACTOR PROCESSING:
  ? Actor A: move batch messages Inbox/ ? Active/A/
  ? Actor B: move batch messages Inbox/ ? Active/B/  
  ? Actor C: move batch messages Inbox/ ? Active/C/
  ?
  ? Parallel: ExecuteActorAsync(A, messages...), ExecuteActorAsync(B, messages...), ExecuteActorAsync(C, messages...)
  ? Outputs accumulated in memory (not written to disk yet)
  ?
BATCH COMMIT PHASE:
  ? All actors finished ? MailboxRouter.CommitBatch()
  ? Atomic write: all Outbox/ responses
  ? Atomic write: all reply messages to target Inbox/ folders
  ? Cleanup: delete processed files from Active/
  ? 
NEXT ITERATION:
  ? New messages may have arrived in Inbox/ during processing
  ? Repeat cycle with fresh snapshots
```

**Failure path**: On any exception during actor processing, that actor's messages move from `Active/` ? `Pending/` with error header. Other actors in the batch continue processing. Failed messages are included in the next batch iteration after `repair`.

---

## `send_message` Action

Every actor with mailbox capability declares this action. `ActionDispatcher` resolves the target actor's `Inbox/` path and validates the actor is loaded before writing. Messages written during batch processing are staged and committed atomically at batch end.

**Action block format** (emitted by the LLM in a response):

```action
name: send_message
to: BusinessAnalyst
subject: FeasibilityCheck
replyTo: Engineer
body: |
  Please review the attached proposal for business alignment.
  Specifically assess whether the scope matches the original requirements.
```

**Dispatcher behaviour (Batch Mode)**:
1. Validates `to` actor exists in loaded workspace — error result if not
2. Generates `correlationId` (UUID) and `batchId` if not supplied
3. **Stages** message for atomic commit at batch end (not written immediately)
4. Returns `OK — message staged for {to}` on success

---

## Mailbox Loop/Iteration Architecture

### **BatchContext**

The `BatchContext` class manages the stable concurrency model:

```csharp
public class BatchContext
{
    public string BatchId { get; }
    public DateTime StartTime { get; }
    public Dictionary<string, List<WallyMessage>> ActorBatches { get; }
    public List<WallyMessage> StagedOutboxMessages { get; }
    public ConcurrentDictionary<string, TaskCompletionSource> ActorCompletions { get; }
}
```

### **MailboxRouter Batch Processing**

```csharp
public async Task RunBatchIterationAsync(CancellationToken cancellationToken)
{
    // Phase 1: Snapshot all inboxes (stable view)
    var batchContext = CreateBatchContext();
    
    // Phase 2: Concurrent actor processing
    var actorTasks = actors.Select(actor => 
        ProcessActorBatchAsync(actor, batchContext, cancellationToken));
    await Task.WhenAll(actorTasks);
    
    // Phase 3: Atomic commit of all outputs
    await CommitBatchOutputsAsync(batchContext, cancellationToken);
    
    // Phase 4: Cleanup active files
    await CleanupBatchAsync(batchContext);
}
```

---

## Actor Mailbox Policies

| Actor | Sends To | Expected Receives From | Batch Behavior |
|-------|----------|----------------------|----------------|
| `Engineer` | `BusinessAnalyst`, `RequirementsExtractor` | `BusinessAnalyst`, `Stakeholder` | Processes technical reviews; outputs staged until batch commit |
| `BusinessAnalyst` | `Engineer`, `Stakeholder`, `RequirementsExtractor` | `Engineer`, `Stakeholder`, `RequirementsExtractor` | Coordination hub; sees stable inbox view during triage |
| `RequirementsExtractor` | `BusinessAnalyst`, `Engineer` | `Stakeholder`, `BusinessAnalyst` | Extracts requirements from stable document state |
| `Stakeholder` | `BusinessAnalyst` | `BusinessAnalyst` | Read-only; business communications staged for batch delivery |

---

## Authority Model

| Component | Responsibility | Batch Integration |
|-----------|----------------|-------------------|
| `ActionDispatcher` | Validates `send_message` actor, **stages** message for batch commit | Messages not written immediately |
| `MailboxRouter` | Orchestrates batch iterations; drives lifecycle (Inbox ? Active ? Outbox/Pending) | **Batch coordinator** |
| `BatchContext` | Manages stable snapshots and staged outputs for concurrent processing | **New component** |
| `MailboxWatcher` | Wraps `FileSystemWatcher`; triggers batch iterations on message arrival | Batch-aware triggering |
| `WallyHelper` | Creates and validates mailbox folder structure | Unchanged |
| `WallyWorkspace` | Calls `EnsureAllMailboxFolders` on every load | Unchanged |

---

## Patterns

**? Pattern**: Use batch processing for stable concurrency — all actors see the same inbox state during iteration.
**? Anti-pattern**: Process messages immediately on arrival — leads to race conditions and inconsistent state.

**? Pattern**: Stage outputs during processing, commit atomically at batch end.
**? Anti-pattern**: Write outputs immediately during actor processing — other actors may see partial state.

**? Pattern**: Message body is free-text Markdown; structured coordination data goes in front-matter fields only.
**? Anti-pattern**: Message body contains structured key-value parameters beyond the front-matter.

**? Pattern**: `repair` logs the file path and error header only; body content is never surfaced in console output.
**? Anti-pattern**: `repair` command prints message body to console.

---

## Diagnostics

| Observable | Location | Meaning | Batch Context |
|------------|----------|---------|---------------|
| Files in `Inbox/` | `Actors/<Name>/Inbox/` | Messages waiting for next batch iteration | **Batch boundary** |
| Files in `Active/` | `Actors/<Name>/Active/` | Messages being processed in current batch | **Per-batch isolation** |
| Files in `Pending/` | `Actors/<Name>/Pending/` | Failed messages from previous batches | **Cross-batch retry** |
| Files in `Outbox/` | `Actors/<Name>/Outbox/` | Completed responses from committed batches | **Atomic output** |
| `[BatchRouter]` log entries | Session log | Batch start/commit/failure per iteration | **Batch lifecycle** |
| Batch metrics | Performance counters | Messages/actor/batch, iteration timing | **Concurrency insights** |

---

## Design Principles

1. **File is the unit of communication** — no in-memory message queues.
2. **Every message is addressable by file path** — no opaque IDs in the routing layer.
3. **`send_message` is the only cross-actor invocation mechanism** — no direct actor-to-actor calls.
4. **Failure state is always on disk** — no silent discards.
5. **Batch processing ensures stable concurrency** — all actors see consistent inbox state during iteration.
6. **Atomic output commitment** — all messages from a batch are written simultaneously.
7. **`correlationId` and `batchId` provide traceability** — across message chains and processing iterations.
8. **Staged writes during processing** — outputs are accumulated and committed at batch end.
