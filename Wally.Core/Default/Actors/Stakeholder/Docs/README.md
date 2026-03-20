# Stakeholder — Actor Reference

> Shared cross-actor conventions (ability system, mailbox protocol, workspace layout,
> templates) are in `Actors/README.md`. This file covers Stakeholder-specific details only.

---

## Role-Exclusive Actions

*None.* The Stakeholder has **zero exclusive actions** — it cannot write any file in the
workspace. Its only side-effect is `send_message` (a shared ability), and that must always
target `BusinessAnalyst`.

---

## Shared Abilities

Resolved from `AbilityRegistry` — identical schema across all actors.

| Ability | What It Does |
|---------|-------------|
| `read_context` | Read any document to review it for business alignment |
| `browse_workspace` | List files in a directory to discover what documents exist |
| `send_message` | Send a message to the BusinessAnalyst's Inbox |

> **`send_message` must always target `BusinessAnalyst`** — never `Engineer` or
> `RequirementsExtractor` directly. All business input is routed through the BA.
> Speak in terms of problems, outcomes, and value — never technical solutions.

---

## Mailbox Routing

| Direction | Actor | Typical Subject |
|-----------|-------|-----------------|
| Sends to | `BusinessAnalyst` | Business needs, priorities, feedback, scope challenges |
| Receives from | `BusinessAnalyst` | Status updates, scope decisions, questions |
