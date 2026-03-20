# RequirementsExtractor — Actor Reference

> Shared cross-actor conventions (action vocabulary, mailbox system, workspace layout,
> templates) are in `Actors/README.md`. This file covers RequirementsExtractor-specific details only.

---

## Abilities

| Ability | Action Name | Scope | Mutating |
|---------|-------------|-------|----------|
| Write a structured requirements document | `write_requirements` | `**/*.md` | Yes |
| Read any file for context | `read_context` | `**` | No |
| List files in a directory | `browse_workspace` | — | No |
| Send a message to another actor's Inbox | `send_message` | — | Yes |

> `write_requirements` is the **only write ability** for this actor — no proposals,
> no implementation plans, no architecture docs, no source code.
> Output path convention: `.wally/Projects/<ProjectName>/Requirements/<FeatureName>.md`
> Read `Templates/RequirementsTemplate.md` before writing to ensure conformance.
> If the input is too vague, ask a clarifying question or use `send_message` rather than guessing.

---

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Completion reports, extracted doc path |
| Sends to | `Engineer` | Technical context requests |
| Receives from | `BusinessAnalyst` | Work assignments |
| Receives from | `Stakeholder` | Raw input (relayed via BA) |

---

## Templates

| Template | Use For |
|----------|---------|
| `Templates/RequirementsTemplate.md` | All requirements documents — must conform exactly |

---

## Domain Context

Place actor-private reference material in this folder to enrich extractions:

- Domain glossaries
- Existing requirements standards the team follows
- Stakeholder interview guides or question frameworks
- Example requirements documents for the project
