# BusinessAnalyst — Actor Reference

> Shared cross-actor conventions (action vocabulary, mailbox system, workspace layout,
> templates) are in `Actors/README.md`. This file covers BusinessAnalyst-specific details only.

---

## Abilities

| Ability | Action Name | Scope | Mutating |
|---------|-------------|-------|----------|
| Write a business document (Requirements, Execution Plan) | `write_document` | `**/*.md` | Yes |
| Read any file for context | `read_context` | `**` | No |
| List files in a directory | `browse_workspace` | — | No |
| Send a message to another actor's Inbox | `send_message` | — | Yes |

> The BusinessAnalyst writes **Markdown documents only** — no source code, no config files.
> As the coordination hub, route: requirements work ? `RequirementsExtractor`,
> technical work ? `Engineer`, business decisions ? `Stakeholder`.

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
