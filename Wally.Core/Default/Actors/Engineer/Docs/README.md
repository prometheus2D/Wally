# Engineer � Actor Reference

> Shared conventions are in `Actors/README.md`. This file covers Engineer-specific details only.

## Role-Exclusive Actions

| Action | Scope | What It Does |
|--------|-------|-------------|
| `change_code` | `**` (any file) | Write or overwrite any file: source code, config, project files. Only actor that can touch non-Markdown files. |
| `write_document` | `**/*.md` | Write or overwrite a technical document conforming to the matching template. |

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Requirement clarification, feasibility questions |
| Receives from | `BusinessAnalyst` | Work assignments, review requests |

## Templates

`ProposalTemplate.md`, `TaskTrackerTemplate.md`, `ArchitectureTemplate.md`, `BugTemplate.md`, `TestPlanTemplate.md`
