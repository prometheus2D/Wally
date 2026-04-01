# RequirementsExtractor — Actor Reference

> Shared conventions are in `Actors/README.md`. This file covers RequirementsExtractor-specific details only.

## Role-Exclusive Actions

| Action | Scope | What It Does |
|--------|-------|-------------|
| `write_requirements` | `**/*.md` | Write a structured requirements document conforming to `RequirementsTemplate.md`. Only write action for this actor. |

> If the input is too vague, ask a clarifying question via `send_message` — do not guess.

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Completion reports, extracted doc path |
| Receives from | `BusinessAnalyst` | Work assignments |

## Templates

`RequirementsTemplate.md`
