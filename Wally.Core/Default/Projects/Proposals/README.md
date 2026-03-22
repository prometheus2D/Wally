# Active Proposals

This folder contains proposals that are currently being developed, under review, or approved for implementation.

## Current Proposals

| Proposal | Status | Priority | Owner | Description |
|----------|--------|----------|-------|-------------|
| [AsyncExecutionProposal.md](./AsyncExecutionProposal.md) | Draft | High | @engineer | Async execution patterns for actor operations |
| [AutonomousBotGapsProposal.md](./AutonomousBotGapsProposal.md) | Under Review | High | @architect | Gaps analysis for autonomous bot functionality |
| [AutonomyLoopProposal.md](./AutonomyLoopProposal.md) | Draft | Medium | @engineer | Self-sustaining autonomy loop implementation |
| [DocumentationWorkflowProposal.md](./DocumentationWorkflowProposal.md) | Draft | Medium | @architect | Automated documentation workflow for actors |
| [EnhancedTextEditorAndRunbookLanguageProposal.md](./EnhancedTextEditorAndRunbookLanguageProposal.md) | In Progress | High | @architect | Parent: Phase 1 (text editor) ? complete; Phase 2 (runbook scripting) pending |
| [MailboxProtocolProposal.md](./MailboxProtocolProposal.md) | In Progress | High | @engineer | Actor-to-actor communication — `send_message` ? implemented; routing/watching pending |
| [RunbookScriptingLanguageProposal.md](./RunbookScriptingLanguageProposal.md) | Draft | High | @developer | WallyScript: variables, loops, conditionals, orchestration for `.wrb` files |

## Proposal Lifecycle

1. **Draft** - Initial proposal being written or refined
2. **Under Review** - Proposal is complete and being reviewed by stakeholders
3. **Approved** - Proposal approved and ready for implementation planning
4. **In Progress** - Implementation has started
5. **Complete** - Implementation finished ? moved to `../Archive/CompletedProposals/`

## Recently Completed

| Proposal | Completed | Summary |
|----------|-----------|---------|
| WorkspaceMemoryProposal | 2025-07-15 | ? Persistent last-workspace and recent-workspaces across Forms and Console |
| TextEditorIntegrationProposal | 2025-07-15 | ? Scintilla.NET integration, ThemedEditorFactory, panel migration |
| ChatDefaultsManagerProposal | 2025-07-15 | ? ConfigEditorPanel with all workspace settings, resolved defaults display |
| ScrollbarAndCommandArgParsingProposal | 2024-01-15 | ? Unified argument parsing, removed System.CommandLine dependency |

## Guidelines

- Use the [ProposalTemplate.md](../../Templates/ProposalTemplate.md) for new proposals
- Include todo tracking with clear ownership and deadlines  
- Define measurable acceptance criteria
- Update status regularly during development

---

*For completed and archived proposals, see [../Archive/](../Archive/)*