# Actor System — Shared Reference

> This file lives in the `Actors/` folder and is automatically injected into **every** actor's
> prompt as shared context. It is the single source of truth for cross-actor conventions.
> Actor-specific details (role-exclusive actions, mailbox routing rows, templates) live in each
> actor's own `Docs/README.md`.

---

## Actor Roster

| Actor | Role | Exclusive Actions | Shared Abilities |
|-------|------|--------------------|------------------|
| `Engineer` | Senior software engineer — all technical work | `change_code` (any file), `write_document` (.md) | `read_context`, `browse_workspace`, `send_message` |
| `BusinessAnalyst` | BA + PM — bridges stakeholder needs and engineering | `write_document` (.md) | `read_context`, `browse_workspace`, `send_message` |
| `RequirementsExtractor` | Requirements specialist — structured req docs only | `write_requirements` (.md) | `read_context`, `browse_workspace`, `send_message` |
| `Stakeholder` | Business voice — defines needs and success criteria | *(none — read-only)* | `read_context`, `browse_workspace`, `send_message` |

---

## Ability System

Actor capabilities are split into two categories:

### Shared Abilities (defined in `AbilityRegistry`)

These are universal capabilities that every actor can opt into by listing the name in
their `"abilities"` array in `actor.json`. The schema (parameters, pathPattern, isMutating)
is defined **once** in the registry and cannot be overridden per-actor — only the description
can be customised for role-specific wording.

| Ability | What It Does | Mutating |
|---------|-------------|----------|
| `read_context` | Read any file in the workspace for context before acting | No |
| `browse_workspace` | List files in a directory to discover structure and avoid duplication | No |
| `send_message` | Write a message to another actor's `Inbox/` for handoff or collaboration | Yes |

### Role-Exclusive Actions (defined inline in `actor.json`)

These are capabilities unique to one or two actors. They are fully declared in the actor's
`"actions"` array with their own schema, path restrictions, and descriptions.

| Action | What It Does | Who Has It | Mutating |
|--------|-------------|------------|----------|
| `change_code` | Write or overwrite **any** file — source, config, project files | Engineer only | Yes |
| `write_document` | Write or overwrite a **Markdown document** (`.md`) | Engineer, BusinessAnalyst | Yes |
| `write_requirements` | Write or overwrite a **Markdown requirements doc** (`.md`) | RequirementsExtractor only | Yes |

> **`change_code` is exclusive to the Engineer.** No other actor may write source code,
> configuration files, or non-Markdown project files.

> **Mutating actions** (`isMutating: true`) require the active LLM wrapper to be in Agent
> mode (`CanMakeChanges = true`). They are silently skipped in read-only mode.

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

The block is parsed by `ActionDispatcher`. Only actions in the actor's resolved abilities +
actions list are executed; all others are rejected and logged as warnings.

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

Use the `send_message` ability. The dispatcher validates the target actor is loaded,
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
            README.md           ? this file (shared across all actors)
            <ActorName>/
                actor.json      ? RBA prompts, abilities, actions, allowed wrappers/loops
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
