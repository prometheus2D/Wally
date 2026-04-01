# Actor System — Shared Reference

> Injected into every actor's prompt as shared context.
> Actor-specific details live in each actor's `Docs/README.md`.

---

## Actor Roster

| Actor | Role | Exclusive Actions |
|-------|------|-------------------|
| `Engineer` | Senior software engineer — all technical work | `change_code`, `write_document` |
| `BusinessAnalyst` | BA + PM — bridges stakeholder needs and engineering | `write_document` |
| `RequirementsExtractor` | Requirements specialist | `write_requirements` |
| `Stakeholder` | Business voice — defines needs | *(read-only)* |

All actors share: `read_context`, `browse_workspace`, `send_message`.

---

## Action Block Format

```
```action
name: <action_name>
<param>: <value>
```
```

Parsed by `ActionDispatcher`. Only actions in the actor's resolved list are executed.

---

## Mailbox Folders

```
.wally/Actors/<ActorName>/
    Inbox/      → inbound messages
    Outbox/     → outbound messages
    Pending/    → failed/deferred messages
    Active/     → message being processed
```

Use `send_message` to send. `route-outbox` delivers. `process-mailboxes` consumes.

---

## Workspace Layout

```
<WorkSource>/
    .wally/
        wally-config.json
        Actors/         → actor folders + this README
        Docs/           → workspace-wide docs
        Templates/      → document templates
        Projects/       → proposals, task trackers
        Loops/          → loop definitions
        Wrappers/       → LLM wrapper configs
        Runbooks/       → .wrb command sequences
        Logs/           → session logs
```

---

## Templates

| Template | Purpose |
|----------|---------|
| `ProposalTemplate.md` | New ideas or approaches |
| `TaskTrackerTemplate.md` | Decompose proposals into executable tasks |
| `ArchitectureTemplate.md` | System design decisions |
| `BugTemplate.md` | Defect tracking |
| `TestPlanTemplate.md` | Verification strategy |
| `RequirementsTemplate.md` | Structured requirements |

---

## Conventions

- **Proposal files**: `.wally/Projects/Proposals/<FeatureName>Proposal.md`
- **Requirement IDs**: `REQ-001`, `REQ-002`, ...
- **Message subjects**: PascalCase, e.g. `FeasibilityCheck`
- **correlationId**: 8-char hex, preserved in reply chains
