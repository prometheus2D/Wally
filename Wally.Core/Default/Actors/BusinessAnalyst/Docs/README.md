# BusinessAnalyst — Actor Reference

> Shared cross-actor conventions (ability system, mailbox protocol, workspace layout,
> templates) are in `Actors/README.md`. This file covers BusinessAnalyst-specific details only.

---

## Role-Exclusive Actions

| Action | Scope | What It Does |
|--------|-------|-------------|
| `write_document` | `**/*.md` | Write or overwrite a business document: Requirements, Execution Plan, project status, or coordination artefact. **Markdown files only** — the BusinessAnalyst may never write source code or configuration files. Must conform to the matching template. |

> **The BusinessAnalyst is the coordination hub.** Route: requirements work ?
> `RequirementsExtractor`, technical work ? `Engineer`, business decisions ? `Stakeholder`.
> Always use `read_context` before `write_document` to check what already exists.

---

## Shared Abilities

Resolved from `AbilityRegistry` — identical schema across all actors.

| Ability | What It Does |
|---------|-------------|
| `read_context` | Read any file for context before making changes |
| `browse_workspace` | List files in a directory to discover structure |
| `send_message` | Send a message to another actor's Inbox |

---

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `Engineer` | Work assignments, technical review requests |
| Sends to | `RequirementsExtractor` | Extraction work assignments |
| Sends to | `Stakeholder` | Business decisions, scope questions |
| Receives from | `Engineer` | Feasibility questions, completion reports |
| Receives from | `RequirementsExtractor` | Completion reports |
| Receives from | `Stakeholder` | Business needs, priorities, feedback |

---

## Templates

| Template | Use For |
|----------|---------|
| `Templates/RequirementsTemplate.md` | Structured, traceable, testable requirements |
| `Templates/ExecutionPlanTemplate.md` | Project coordination and delivery sequencing |
