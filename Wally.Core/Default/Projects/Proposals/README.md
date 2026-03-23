# Active Proposals

This folder contains proposals that are currently being developed, under review, or approved for implementation.

## Current Proposals

| Proposal | Status | Priority | Owner | Description |
|----------|--------|----------|-------|-------------|
| [AutonomousBotGapsProposal.md](./AutonomousBotGapsProposal.md) | In Progress (Phases 1-3 Complete, Phase 4 In Progress) | High | @architect | Parent: async ?, autonomy loop ?, mailbox ?; Phase 4 documentation workflow in progress |
| [AutonomyLoopProposal.md](./AutonomyLoopProposal.md) | ? Implemented | High | @engineer | `WallyAgentLoop` with stop conditions + feedback modes |
| [MailboxProtocolProposal.md](./MailboxProtocolProposal.md) | ? Implemented | High | @engineer | Actor-to-actor communication — `send_message` ? Outbox; `process-mailboxes` + `route-outbox` |
| [DocumentationWorkflowProposal.md](./DocumentationWorkflowProposal.md) | In Progress (Phases 1-3 Complete) | Medium | @architect | Documentation reflection loop — loop def ?, actor prompts ?, workflow guide ?; end-to-end validation pending |
| [UnifiedExecutionModelProposal.md](./UnifiedExecutionModelProposal.md) | ? Approved | High | @lead-engineer | Data-driven action dispatch from actor.json; retire dead WallyPipeline — Implementation Plan ready |

## Proposal Lifecycle

1. **Draft** - Initial proposal being written or refined
2. **Under Review** - Proposal is complete and being reviewed by stakeholders
3. **Approved** - Proposal approved and ready for implementation planning
4. **In Progress** - Implementation has started
5. **Complete** - Implementation finished ? moved to `../Archive/CompletedProposals/`
6. **Cancelled** - Proposal cancelled or superseded ? moved to `../Archive/CancelledProposals/`

## Recently Completed

| Proposal | Completed | Summary |
|----------|-----------|---------|
| AsyncExecutionProposal | 2025-07-15 | ? `ExecuteAsync` at all layers, sync wrappers, `ConfigureAwait(false)`, end-to-end cancellation |
| EnhancedTextEditorAndRunbookLanguageProposal | 2025-07-15 | ? Phase 1 (Scintilla.NET) complete; Phase 2 (scripting) cancelled — handled elsewhere |
| WorkspaceMemoryProposal | 2025-07-15 | ? Persistent last-workspace and recent-workspaces across Forms and Console |
| TextEditorIntegrationProposal | 2025-07-15 | ? Scintilla.NET integration, ThemedEditorFactory, panel migration |
| ChatDefaultsManagerProposal | 2025-07-15 | ? ConfigEditorPanel with all workspace settings, resolved defaults display |
| ScrollbarAndCommandArgParsingProposal | 2024-01-15 | ? Unified argument parsing, removed System.CommandLine dependency |

## Recently Cancelled

| Proposal | Cancelled | Reason |
|----------|-----------|--------|
| RunbookScriptingLanguageProposal | 2025-07-15 | Handled elsewhere; `.wrb` files remain simple command lists. *(Proposal file was removed; not archived.)* |

## Guidelines

- Use the [ProposalTemplate.md](../../Templates/ProposalTemplate.md) for new proposals
- Include todo tracking with clear ownership and deadlines  
- Define measurable acceptance criteria
- Update status regularly during development

---

*For completed and archived proposals, see [../Archive/](../Archive/)*