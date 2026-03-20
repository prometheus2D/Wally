# Stakeholder — Actor Reference

> Shared cross-actor conventions (action vocabulary, mailbox system, workspace layout,
> templates) are in `Actors/README.md`. This file covers Stakeholder-specific details only.

---

## Abilities

| Ability | Action Name | Scope | Mutating |
|---------|-------------|-------|----------|
| Read any document for business alignment review | `read_context` | `**` | No |
| Send a message to the BusinessAnalyst's Inbox | `send_message` | — | Yes |

> The Stakeholder is **strictly read-only on all workspace files**.
> `send_message` must always target `BusinessAnalyst` — never `Engineer` or
> `RequirementsExtractor` directly. All business input is routed through the BA.
> Speak in terms of problems, outcomes, and value — never technical solutions.

---

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Business needs, priorities, feedback, scope challenges |
| Receives from | `BusinessAnalyst` | Status updates, scope decisions, questions |
