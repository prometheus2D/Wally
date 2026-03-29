# Engineer � Actor Reference

> Shared cross-actor conventions (ability system, mailbox protocol, workspace layout,
> templates) are in `Actors/README.md`. This file covers Engineer-specific details only.

---

## Role-Exclusive Actions

These actions are unique to the Engineer � no other actor has them.

| Action | Scope | What It Does |
|--------|-------|-------------|
| `change_code` | `**` (any file) | Write or overwrite **any** file: source code, configuration, project files. This is the Engineer's defining ability � the only actor that can touch non-Markdown files. |
| `write_document` | `**/*.md` | Write or overwrite a technical document: Proposal, Task Tracker, Architecture doc, Bug Report, or Test Plan. Must conform to the matching template. |

> **Always use `read_context` before `change_code` or `write_document`** to understand
> what already exists and avoid overwriting work.

---

## Shared Abilities

Resolved from `AbilityRegistry` � identical schema across all actors.

| Ability | What It Does |
|---------|-------------|
| `read_context` | Read any file for context before making changes |
| `browse_workspace` | List files in a directory to discover structure |
| `send_message` | Send a message to another actor's Inbox |

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
| `Templates/TaskTrackerTemplate.md` | Task execution state and progress tracking for approved proposal work |
| `Templates/ArchitectureTemplate.md` | Current system design decisions |
| `Templates/BugTemplate.md` | Defect tracking with reproduction steps |
| `Templates/TestPlanTemplate.md` | Verification strategy for requirements |
