# Actor System — Shared Reference

> This file lives in the `Actors/` folder and is automatically injected into **every** actor's
> prompt as shared context. It is the single source of truth for cross-actor conventions.
> Actor-specific details (individual abilities, mailbox routing rows, templates) live in each
> actor's own `Docs/README.md`.

---

## Actor Roster

| Actor | Role | Write Scope | Mailbox Policy |
|-------|------|-------------|----------------|
| `Engineer` | Senior software engineer — all technical work | Any file (`**`) via `change_code`; `.md` docs via `write_document` | Sends to BA / RE; receives from BA |
| `BusinessAnalyst` | BA + PM — bridges stakeholder needs and engineering | `.md` docs only via `write_document` | Coordination hub — sends to all; receives from all |
| `RequirementsExtractor` | Requirements specialist — structured req docs only | `.md` docs only via `write_requirements` | Sends to BA / Engineer; receives from Stakeholder / BA |
| `Stakeholder` | Business voice — defines needs and success criteria | Read-only (no file writes) | Sends to BA only; receives from BA only |

---

## Action Vocabulary

All actions share this vocabulary. An actor may only invoke actions it has declared in its `actor.json`.

| Action Name | What It Does | Who Has It | Mutating |
|-------------|-------------|------------|----------|
| `change_code` | Write or overwrite **any** file — source, config, project files | Engineer only | Yes |
| `write_document` | Write or overwrite a **Markdown document** (`.md`) | Engineer, BusinessAnalyst | Yes |
| `write_requirements` | Write or overwrite a **Markdown requirements doc** (`.md`) | RequirementsExtractor only | Yes |
| `read_context` | Read any file for context before acting | All actors | No |
| `browse_workspace` | List files in a directory | All actors | No |
| `send_message` | Write a message to another actor's `Inbox/` | All actors | Yes |

> **`change_code` is exclusive to the Engineer.** No other actor may write source code,
> configuration files, or non-Markdown project files. Attempts to do so will be rejected
> by `ActionDispatcher`.

> **Mutating actions** (`isMutating: true`) require the active LLM wrapper to be in Agent
> mode (`CanMakeChanges = true`). They are silently skipped in read-only (Copilot/review) mode.

---

## Action Block Format

When the LLM wants to invoke an action it emits a fenced block in its response:

````
```action
name: <action_name>
<param>: <single-line value>
<param>: |
  Multi-line value
  indented by two spaces
```
````

The block is parsed by `ActionDispatcher`. Only actions declared in the actor's `actor.json`
are executed; all others are rejected and logged as warnings.

---

## Mailbox System

Every actor has four mailbox folders inside its directory:

```
.wally/Actors/<ActorName>/
    Inbox/      ? new messages arrive here
    Outbox/     ? responses written here after execution
    Pending/    ? failed messages waiting for retry
    Active/     ? message currently being processed
```

### Sending a Message

Use the `send_message` action. The dispatcher validates the target actor is loaded,
generates a `correlationId`, and writes the message file to the target's `Inbox/`.

```action
name: send_message
to: BusinessAnalyst
subject: FeasibilityCheck
body: |
  Please review the attached proposal for business alignment.
replyTo: Engineer
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `to` | Yes | Target actor name — must be a loaded actor |
| `subject` | Yes | Short topic identifier, PascalCase or hyphenated |
| `body` | Yes | Free-text Markdown — the full prompt the target actor receives |
| `replyTo` | No | Actor to receive the response (defaults to the sender) |

### Routing Policy

| From ? To | Permitted | Notes |
|-----------|-----------|-------|
| Engineer ? BusinessAnalyst | ? | Requirement clarification, technical proposals |
| Engineer ? RequirementsExtractor | ? | Raw input for extraction |
| BusinessAnalyst ? Engineer | ? | Work assignments, technical review requests |
| BusinessAnalyst ? RequirementsExtractor | ? | Extraction work assignments |
| BusinessAnalyst ? Stakeholder | ? | Business decisions, scope questions |
| RequirementsExtractor ? BusinessAnalyst | ? | Completion reports |
| RequirementsExtractor ? Engineer | ? | Technical context requests |
| Stakeholder ? BusinessAnalyst | ? | Business needs, priorities, feedback |
| Any ? Engineer (direct from Stakeholder) | ? | Route through BusinessAnalyst |
| Any ? RequirementsExtractor (direct from Stakeholder) | ? | Route through BusinessAnalyst |

> Full protocol detail — message file format, YAML front-matter, lifecycle states,
> failure handling, and diagnostics — is in `Docs/MailboxSystemArchitecture.md`.

---

## Workspace Folder Layout

```
<WorkSource>/                   ? repo root (WorkSource)
    .wally/                     ? workspace folder
        wally-config.json
        Actors/
            <ActorName>/
                actor.json      ? RBA prompts, abilities, allowed wrappers/loops
                Docs/           ? actor-private docs (injected into this actor's prompt only)
                Inbox/
                Outbox/
                Pending/
                Active/
        Docs/                   ? workspace-wide docs (injected into every actor's prompt)
        Templates/              ? document templates (ProposalTemplate.md, etc.)
        Projects/               ? project store: Epochs ? Sprints ? Tasks
        Loops/
        Wrappers/
        Runbooks/
        Logs/
```

---

## Document Templates

| Template | Used By | Purpose |
|----------|---------|---------|
| `Templates/ProposalTemplate.md` | Engineer | New technical ideas or approaches |
| `Templates/ImplementationPlanTemplate.md` | Engineer | Step-by-step execution of an approved proposal |
| `Templates/ArchitectureTemplate.md` | Engineer | Current system design decisions |
| `Templates/BugTemplate.md` | Engineer | Defect tracking with reproduction steps |
| `Templates/TestPlanTemplate.md` | Engineer | Verification strategy for requirements |
| `Templates/RequirementsTemplate.md` | RequirementsExtractor, BusinessAnalyst | Structured, traceable, testable requirements |
| `Templates/ExecutionPlanTemplate.md` | BusinessAnalyst | Project coordination and delivery sequencing |

---

## Conventions

- **Document paths**: `{WorkSource}/.wally/Projects/<ProjectName>/...`
- **Requirements files**: `.wally/Projects/<ProjectName>/Requirements/<FeatureName>.md`
- **Proposal files**: `.wally/Projects/Proposals/<FeatureName>Proposal.md`
- **Requirement IDs**: `REQ-001`, `REQ-002`, ... — numbered sequentially per document
- **Message subjects**: PascalCase or hyphenated, e.g. `FeasibilityCheck`, `CodeReview-AuthModule`
- **correlationId**: auto-generated 8-char hex prefix — preserved in all reply chains
