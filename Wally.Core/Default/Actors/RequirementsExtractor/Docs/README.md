# RequirementsExtractor — Actor Reference

> Shared cross-actor conventions (ability system, mailbox protocol, workspace layout,
> templates) are in `Actors/README.md`. This file covers RequirementsExtractor-specific details only.

---

## Role-Exclusive Actions

| Action | Scope | What It Does |
|--------|-------|-------------|
| `write_requirements` | `**/*.md` | Write a structured requirements document to the workspace. **This is the only write action** for this actor — no proposals, no implementation plans, no architecture docs, no source code. Output must conform exactly to `Templates/RequirementsTemplate.md`. |

> **Output path convention**: `.wally/Projects/<ProjectName>/Requirements/<FeatureName>.md`
> Always use `read_context` to read `Templates/RequirementsTemplate.md` before writing.
> If the input is too vague to produce a well-formed requirement, ask a clarifying question
> or use `send_message` to request more detail — do not guess.

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
