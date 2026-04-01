# Investigator Workspace

Runtime state folder for `InvestigationLoop`. Files are created on demand by the loop — not pre-shipped.

## Folders

| Folder | Created By | Contents |
|--------|-----------|----------|
| `Docs/` | Loop steps | `InvestigationBrief.md`, `Findings.md`, `Ideas.md`, `OpenQuestions.md`, `InteractionState.md`, `InvestigationLog.md`, `UserResponses.md` |
| `Memory/` | Loop steps | `CurrentSummary.md`, `ProposalDraft.md` |
| `Inbox/` | `route-outbox` | Inbound messages |
| `Outbox/` | `send_message` | Outbound requests |

The loop rebuilds context from these files each iteration. Documentation is the source of truth, not prompt history.

See `Loops/InvestigationLoop.json` for the `documentInputs` that declare every path.