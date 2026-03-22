# Project Archive

This folder contains completed and archived project documentation.

## Structure

```
Archive/
??? CompletedProposals/     # Implemented proposals that are now complete
??? ImplementationPlans/    # Completed implementation plans
```

## Completed Proposals

| Proposal | Completed Date | Summary |
|----------|----------------|---------|
| AsyncExecutionProposal.md | 2025-07-15 | ? `ExecuteAsync` at all 4 layers, sync wrappers, `ConfigureAwait(false)`, end-to-end cancellation with `process.Kill` |
| EnhancedTextEditorAndRunbookLanguageProposal.md | 2025-07-15 | ? Phase 1 (Scintilla.NET) complete; Phase 2 (scripting) cancelled — handled elsewhere |
| WorkspaceMemoryProposal.md | 2025-07-15 | ? Persistent last-workspace and recent-workspaces across Forms and Console |
| TextEditorIntegrationProposal.md | 2025-07-15 | ? Scintilla.NET integration, ThemedEditorFactory, panel migration |
| ChatDefaultsManagerProposal.md | 2025-07-15 | ? ConfigEditorPanel with all workspace settings, resolved defaults display |
| ScrollbarAndCommandArgParsingProposal.md | 2024-01-15 | ? Unified argument parsing across CLI and Forms, removed System.CommandLine dependency |

## Cancelled Proposals

| Proposal | Cancelled Date | Reason |
|----------|----------------|--------|
| RunbookScriptingLanguageProposal | 2025-07-15 | Handled elsewhere; `.wrb` files remain simple batch-style command lists. *(Proposal file was removed; not archived.)* |

## Completed Implementation Plans

| Plan | Completed Date | Summary |
|------|----------------|---------|
| WorkspaceMemoryImplementationPlan.md | 2025-07-15 | ? All 5 phases delivered: Core store, Forms auto-load, Recent menu, Console interactive, Console one-shot |

## Guidelines

- **Completed Proposals**: Move proposals here when they are fully implemented and validated
- **Implementation Plans**: Move completed implementation plans here for historical reference

## Active Work

For active proposals and implementation plans, see:
- `../Proposals/` - Active proposals under development or review
- `../ImplementationPlans/` - Active implementation plans being executed

---

*This archive preserves the historical record of project decisions and implementations.*