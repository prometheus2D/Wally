# BusinessAnalyst � Actor Reference

> Shared conventions are in `Actors/README.md`. This file covers BusinessAnalyst-specific details only.

---

## Role-Exclusive Actions

| Action | Scope | What It Does |
|--------|-------|-------------|
| `write_document` | `**/*.md` | Write or overwrite a business document. Markdown files only � may never write source code. |

> **The BusinessAnalyst is the coordination hub.** Route: requirements work ?
> `RequirementsExtractor`, technical work ? `Engineer`, business decisions ? `Stakeholder`.
> Always use `read_context` before `write_document` to check what already exists.

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

`RequirementsTemplate.md`
