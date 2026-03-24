# Next Steps — Prioritised Work Queue

**Last Updated**: 2025-07-17  

---

## Active Work

### Phase 4: Documentation Workflow Automation (In Progress)

**Parent**: [AutonomousBotGapsProposal](./Proposals/AutonomousBotGapsProposal.md) ? [AutonomousBotImplementationPlan](./ImplementationPlans/AutonomousBotImplementationPlan.md)  
**Proposal**: [DocumentationWorkflowProposal](./Proposals/DocumentationWorkflowProposal.md)

| Deliverable | Status | Notes |
|-------------|--------|-------|
| `DocumentationReflection.json` loop definition | ? Complete | `MaxIterations: 5`, `StopKeyword: "DOCUMENTATION_COMPLETE"`, actor: BusinessAnalyst |
| BusinessAnalyst prompt enhancements | ? Complete | rolePrompt, criteriaPrompt, intentPrompt enhanced for doc lifecycle awareness |
| Engineer `allowedLoops` update | ? Complete | Added `DocumentationReflection` |
| `DocumentationWorkflowGuide.md` | ? Complete | Usage patterns, best practices, troubleshooting |
| End-to-end validation with real data | ?? Not Started | Run `wally run "" -l DocumentationReflection` against current workspace |
| Convergence detection validation | ?? Not Started | Verify StopKeyword and MaxIterations behaviour |

**Next action**: Run the documentation reflection loop against the current workspace to validate end-to-end behaviour.

---

## Pending: Archive Housekeeping

*No pending archive work — all implemented proposals are now archived.*

---

## Completed Recently

| Item | Completed | Summary |
|------|-----------|---------|
| Unified Execution Model implemented | 2025-07-17 | `HasRoleAction` deleted; `actor.Actions[]` now authoritative at runtime; `WallyLoop.cs` deleted; `ActionDefinition` + `ActionParameterDefinition` model classes created |
| Unified Execution Model proposal drafted | 2025-07-17 | Architecture review: data-driven actions, dead code removal, execution unification |
| Mailbox system architecture doc updated | 2025-07-17 | Removed stale "not implemented" warning; updated to ? COMPLETE |
| DocumentationWorkflowProposal fleshed out | 2025-07-17 | Full proposal with problem statement, resolution, phases, loop definition, acceptance criteria |
| DocumentationReflection loop created | 2025-07-17 | Agent loop with convergence detection for documentation auditing |
| Actor prompts enhanced | 2025-07-17 | BA: documentation lifecycle awareness; Engineer: allowedLoops updated |
| DocumentationWorkflowGuide written | 2025-07-17 | Usage guide with quick start, best practices, troubleshooting |
| All tracking documents reconciled | 2025-07-17 | AutonomousBotGapsProposal, AutonomousBotImplementationPlan, Proposals README updated |

---

## Future Work (Not Yet Planned)

- **Move/archive command** — `move_document` action for actors to relocate files between folders (needed for auto-archival)
- **RequirementsExtractor documentation awareness** — add `DocumentationReflection` to allowedLoops (low priority)
- **Performance benchmarking** — verify no regression in single-user scenarios across all phases
- **Integration test suite** — automated tests for agent loop, mailbox, and documentation workflow
