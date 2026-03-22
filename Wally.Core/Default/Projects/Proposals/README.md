# Active Proposals

This folder contains proposals that are currently being developed, under review, or approved for implementation. It also contains **discussion documents** — exploratory roundtables that surface open questions and drive decisions. Resolved discussions spawn proposals; they are archived when all questions are closed.

## Current Proposals

| Proposal | Status | Priority | Owner | Description |
|----------|--------|----------|-------|-------------|
| [AsyncExecutionProposal.md](./AsyncExecutionProposal.md) | Draft | High | @engineer | Async execution patterns for actor operations |
| [AutonomousBotGapsProposal.md](./AutonomousBotGapsProposal.md) | Under Review | High | @architect | Gaps analysis for autonomous bot functionality |
| [AutonomyLoopProposal.md](./AutonomyLoopProposal.md) | Draft | Medium | @engineer | Self-sustaining autonomy loop implementation |
| [ChatDefaultsManagerProposal.md](./ChatDefaultsManagerProposal.md) | Draft | Low | @ui-engineer | Chat interface defaults management |
| [DocumentationWorkflowProposal.md](./DocumentationWorkflowProposal.md) | Draft | Medium | @architect | Automated documentation workflow for actors |
| [EnhancedTextEditorAndRunbookLanguageProposal.md](./EnhancedTextEditorAndRunbookLanguageProposal.md) | Draft | High | @architect | Parent: professional text editor + runbook scripting language |
| [MailboxProtocolProposal.md](./MailboxProtocolProposal.md) | Draft | High | @engineer | Actor-to-actor communication protocol |
| [RunbookScriptingLanguageProposal.md](./RunbookScriptingLanguageProposal.md) | Draft | High | @developer | WallyScript: variables, loops, conditionals, orchestration for `.wrb` files |
| [RunbookSyntaxProposal.md](./RunbookSyntaxProposal.md) | Draft | High | @architect | Variables, shell lines, `if`/`else`, `loop`/`open` — simple Turing-complete runbook syntax |
| [TextEditorIntegrationProposal.md](./TextEditorIntegrationProposal.md) | Draft | High | @frontend | ScintillaNET/alternative editor integration into Wally Forms |

## Current Discussions

Discussion documents are open roundtables. They capture competing options, design tensions, and unresolved questions. They are **not** proposals — they exist to drive decisions. A discussion document is complete when every question is resolved or explicitly deferred to a proposal.

| Discussion | Status | Priority | Facilitator | Description |
|------------|--------|----------|-------------|-------------|
| [RunbookSyntaxDiscussion.md](./RunbookSyntaxDiscussion.md) | In Progress | High | @ron | 5 open questions: shell prefix, CWD, loop file extension, `each` cap, `$out` clobber handling |

## Document Templates

| Template | Purpose |
|----------|---------|
| [ProposalTemplate](../../Templates/ProposalTemplate.md) | New feature or system proposals |
| [DiscussionTemplate](../../Templates/DiscussionTemplate.md) | Open-question roundtables; spawns proposals when resolved |
| [BugTemplate](../../Templates/BugTemplate.md) | Defect investigation and resolution |

## Proposal Lifecycle

1. **Draft** - Initial proposal being written or refined
2. **Under Review** - Proposal is complete and being reviewed by stakeholders
3. **Approved** - Proposal approved and ready for implementation planning
4. **In Progress** - Implementation has started
5. **Complete** - Implementation finished ? moved to `../Archive/CompletedProposals/`

## Discussion Lifecycle

1. **Open** - Questions identified; owners assigned
2. **In Progress** - Questions actively being researched and resolved
3. **Resolved** - All questions answered; decisions logged; proposals spawned
4. **Archived** - Moved to `../Archive/` with link to spawned proposals

## Recently Completed

| Proposal | Completed | Summary |
|----------|-----------|---------|
| ScrollbarAndCommandArgParsingProposal | 2024-01-15 | ? Unified argument parsing, removed System.CommandLine dependency |

## Guidelines

- Use the [ProposalTemplate.md](../../Templates/ProposalTemplate.md) for new proposals
- Use the [DiscussionTemplate.md](../../Templates/DiscussionTemplate.md) for design roundtables and open questions
- Include todo tracking with clear ownership and deadlines
- Define measurable acceptance criteria
- Update status regularly during development

---

*For completed and archived proposals, see [../Archive/](../Archive/)*