# Stakeholder — Actor Reference

> Shared conventions are in `Actors/README.md`. This file covers Stakeholder-specific details only.

## Role-Exclusive Actions

*None.* The Stakeholder is read-only. Its only side-effect is `send_message`, which must always target `BusinessAnalyst`.

> Speak in terms of problems, outcomes, and value — never technical solutions.

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Business needs, priorities, feedback |
| Receives from | `BusinessAnalyst` | Status updates, scope decisions, questions |
