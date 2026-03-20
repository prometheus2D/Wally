# Engineer — Actor Reference

> Shared cross-actor conventions (action vocabulary, mailbox system, workspace layout,
> templates) are in `Actors/README.md`. This file covers Engineer-specific details only.

---

## Abilities

| Ability | Action Name | Scope | Mutating |
|---------|-------------|-------|----------|
| Write source code, config, or any project file | `change_code` | `**` (any path) | Yes |
| Write a technical document | `write_document` | `**/*.md` | Yes |
| Read any file for context | `read_context` | `**` | No |
| List files in a directory | `browse_workspace` | — | No |
| Send a message to another actor's Inbox | `send_message` | — | Yes |

> **`change_code` is the Engineer's exclusive ability.** No other actor may write source
> code or non-Markdown files. Always use `read_context` before `change_code` or
> `write_document` to avoid overwriting work.

---

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Requirement clarification, feasibility questions |
| Sends to | `RequirementsExtractor` | Raw unstructured input for extraction |
| Receives from | `BusinessAnalyst` | Work assignments, review requests |

---

## Templates

| Template | Use For |
|----------|---------|
| `Templates/ProposalTemplate.md` | New technical ideas or approaches |
| `Templates/ImplementationPlanTemplate.md` | Step-by-step execution of an approved proposal |
| `Templates/ArchitectureTemplate.md` | Current system design decisions |
| `Templates/BugTemplate.md` | Defect tracking with reproduction steps |
| `Templates/TestPlanTemplate.md` | Verification strategy for requirements |
