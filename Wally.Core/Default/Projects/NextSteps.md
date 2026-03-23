# Next Steps — Prioritised Work Queue

**Last Updated**: 2025-07-17  

---

## Active Work

### ?? Unified Execution Model (Draft — Needs Review)

**Proposal**: [UnifiedExecutionModelProposal](./Proposals/UnifiedExecutionModelProposal.md)  
**Priority**: High — addresses architectural debt that blocks clean extensibility

This is a structural refactor identified during architecture review. Three key findings:

1. **`ActionDispatcher` is hardcoded** — adding a new action requires modifying C# source, even though `actor.json` already declares full action metadata (path patterns, parameters, mutability) that the code ignores
2. **`HasRoleAction` hardcodes actor names** — checks `actor.Name.Equals("Engineer")` instead of reading the actor's declared `actions[]` array
3. **`WallyPipeline` in `WallyLoop.cs` is dead code** — all pipeline execution goes through `RunPipelineAsync`

| Phase | Description | Effort | Status |
|-------|-------------|--------|--------|
| 1 | Data-driven action dispatch from `actor.Actions[]` | 2-3 days | ?? Not Started |
| 2 | Delete dead `WallyPipeline` class | 0.5 day | ?? Not Started |
| 3 | (Future) Unify execution routing | 3-5 days | Deferred |

**Next action**: Review and approve the proposal, then create an implementation plan.

---

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

The following implemented proposals are still in `Projects/Proposals/` and should be moved to `Projects/Archive/CompletedProposals/` once Phase 4 validation is complete:

| Proposal | Status | Blocked By |
|----------|--------|------------|
| AutonomyLoopProposal.md | ? Implemented | Parent (AutonomousBotGaps) not yet fully complete |
| MailboxProtocolProposal.md | ? Implemented | Parent (AutonomousBotGaps) not yet fully complete |

---

## Completed Recently

| Item | Completed | Summary |
|------|-----------|---------|
| Unified Execution Model proposal drafted | 2025-07-17 | Architecture review: data-driven actions, dead code removal, execution unification |
| Mailbox system architecture doc updated | 2025-07-17 | Removed stale "not implemented" warning; updated to ? COMPLETE |
| DocumentationWorkflowProposal fleshed out | 2025-07-17 | Full proposal with problem statement, resolution, phases, loop definition, acceptance criteria |
| DocumentationReflection loop created | 2025-07-17 | Agent loop with convergence detection for documentation auditing |
| Actor prompts enhanced | 2025-07-17 | BA: documentation lifecycle awareness; Engineer: allowedLoops updated |
| DocumentationWorkflowGuide written | 2025-07-17 | Usage guide with quick start, best practices, troubleshooting |
| All tracking documents reconciled | 2025-07-17 | AutonomousBotGapsProposal, AutonomousBotImplementationPlan, Proposals README updated |

---

## Future Work (Not Yet Planned)

- **Unified execution routing** (Phase 3 of UnifiedExecutionModelProposal) — merge single-shot, pipeline, and agent-loop into one execution path. Deferred until Phase 1 (data-driven actions) is stable.
- **Move/archive command** — `move_document` action for actors to relocate files between folders (needed for auto-archival)
- **RequirementsExtractor documentation awareness** — add `DocumentationReflection` to allowedLoops (low priority)
- **Performance benchmarking** — verify no regression in single-user scenarios across all phases
- **Integration test suite** — automated tests for agent loop, mailbox, and documentation workflow
