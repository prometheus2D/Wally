# Enhanced Text Editor & Runbook Language — Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2024-01-15
**Last Updated**: 2024-01-16

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Two related but independent gaps limit expert-user productivity in Wally:

1. **No professional text editor** — `ThemedRichTextBox` provides no syntax highlighting, code folding, auto-completion, or multi-cursor editing. Users editing JSON, Markdown, runbooks, and actor definitions have a worse experience than any modern text editor.
2. **Runbooks are flat command lists** — `.wrb` files execute one command per line with no variables, conditionals, loops, or error handling. Complex multi-actor workflows require multiple runbooks chained manually or custom one-off pipelines.

---

## Resolution

Two independent workstreams delivered in priority order. Each is detailed in its own proposal.

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Integrate professional text editor component | 5-7 days | None |
| 2 | Design and implement runbook scripting language | 8-12 days | Async execution complete |

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [TextEditorIntegrationProposal](./TextEditorIntegrationProposal.md) | Child — Phase 1 | Editor component research, integration, and panel migration |
| [RunbookScriptingLanguageProposal](./RunbookScriptingLanguageProposal.md) | Child — Phase 2 | Language design, parser, execution engine |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depended on by | Phase 2 scripting engine requires async execution; **also requires `HandleRunTyped` to accept optional `TextWriter` for parallel output buffering** |
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Depended on by | Phase 2 orchestration patterns use mailbox protocol |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Runbook scripting complements autonomous loops |

---

## Impact

| System | Change | Risk Level |
|--------|--------|------------|
| `Wally.Forms.csproj` | Add `Scintilla.NET` NuGet package (Phase 1) | Low |
| `Wally.Forms/Controls/ThemedEditorFactory.cs` | **Create new file** with factory methods (Phase 1) | Medium |
| `Wally.Forms/Controls/Editors/*.cs` | Replace RichTextBox with professional editor (Phase 1) | Medium |
| `Wally.Core/WallyRunbook.cs` | Enhanced parsing for scripted format (Phase 2) | Low |
| `Wally.Core/Scripting/` | New scripting engine (Phase 2) | High |
| `Wally.Core/WallyCommands.cs` | Integration with script execution; phase-1 placeholder resolution (Phase 2) | Medium |
| `Wally.Core/WallyCommands.cs` (async) | `HandleRunTyped` optional `TextWriter` — coordinate with AsyncExecutionProposal (Phase 2) | Medium |

---

## Cross-Phase Performance Budget

| Metric | Phase 1 Budget | Phase 2 Budget | Combined Cap |
|--------|---------------|----------------|--------------|
| Startup time regression | < 200 ms | < 50 ms | < 250 ms total |
| Form load regression | < 200 ms | Negligible | < 200 ms |
| First editor render | < 300 ms | n/a | < 300 ms |
| Script parse time (10 KB `.wrb`) | n/a | < 50 ms | < 50 ms |

> Baselines measured before Phase 1 work begins. Phase 2 scripting overhead measured against the post-Phase-1 baseline.

---

## Rollback Procedures

### Phase 1 Rollback
1. Remove the `Scintilla.NET` NuGet package reference from `Wally.Forms.csproj`
2. Revert `RunbookEditorPanel.cs` (and any other migrated panels) to `ThemedRichTextBox`
3. Delete `ThemedEditorFactory.cs`
4. Feature flags must gate all panel migrations — revert is a config change if flags are in place

### Phase 2 Rollback
1. `WallyRunbook.LoadFromFile` format detection falls through to `simple` mode — set `ForceSimpleFormat = true` config flag
2. `Wally.Core/Scripting/` folder can remain; the execution path is only entered when the script format is detected
3. Disabling the scripting engine requires only the config flag — no file deletions needed in production

> **Prerequisite**: Both phases must be gated by a feature flag before merging to master. Rollback is a config change, not a code change.

---

## Integration Testing Strategy

The following integration scenarios span both phases and must be covered before the parent proposal is marked complete:

| Scenario | Phase | Test Type |
|----------|-------|-----------|
| `RunbookEditorPanel` opens a `.wrb` file with syntax highlighting | Phase 1 | Manual smoke |
| Enhanced `.wrb` script (with `$var`, `if`) is opened in editor with WallyScript highlighting | Both | Manual smoke |
| Simple `.wrb` executes identically before and after Phase 1 | Phase 1 | Automated regression |
| Simple `.wrb` executes identically before and after Phase 2 | Phase 2 | Automated regression |
| WallyScript `parallel` block triggers correctly themed parallel-branch output | Both | Manual smoke |
| Form load time measured against cross-phase performance budget | Both | Automated benchmark |

---

## Benefits

- **Phase 1**: Expert-grade editing experience across all file types; syntax highlighting, folding, find/replace, multi-cursor
- **Phase 2**: Complex multi-actor workflows expressible in a single runbook; variables, loops, conditionals, parallel execution, error handling

---

## Risks

- **Component licensing** — mitigated by restricting candidates to MIT / commercial-free libraries
- **WinForms integration complexity** — mitigated by POC phase before full migration
- **Language design scope creep** — mitigated by focusing on actor orchestration first, general programming constructs second
- **Backward compatibility** — mitigated by auto-detecting simple vs enhanced format in `.wrb` files
- **Cross-phase `TextWriter` interface change** — `HandleRunTyped` must accept optional `TextWriter` before Phase 2 `parallel` can ship; mitigated by coordinating with AsyncExecutionProposal early

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Phase 1 editor component research and POC | High | ?? Not Started | @frontend | 2024-01-20 | See child proposal |
| Phase 2 runbook language design review | High | ?? Not Started | @architect | 2024-01-25 | See child proposal |
| Validate backward compatibility strategy | Medium | ?? Not Started | @engineer | 2024-01-22 | Existing `.wrb` files unchanged |
| Establish pre-Phase-1 performance baseline | High | ?? Not Started | @qa | 2024-01-17 | Required for cross-phase budget tracking |
| Add feature flags for Phase 1 panel migrations | High | ?? Not Started | @frontend | 2024-01-20 | Gate all migrations; enables config-only rollback |
| Add feature flag / config option for Phase 2 scripting engine | High | ?? Not Started | @developer | 2024-01-25 | `ForceSimpleFormat` fallback |
| Define integration test plan (cross-phase scenarios) | Medium | ?? Not Started | @qa | 2024-01-22 | See Integration Testing Strategy table |
| Coordinate `TextWriter` parameter change with AsyncExecutionProposal | High | ?? Not Started | @developer | 2024-01-22 | Required before Phase 2 `parallel` block can ship |

---

## Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] Both child proposals approved
- [ ] Phase dependencies validated
- [ ] Effort estimates confirmed by engineering team
- [ ] Risk mitigation strategies approved
- [ ] Pre-Phase-1 performance baseline captured
- [ ] Feature flags in place for both phases

#### Should Have (Preferred for Quality)
- [ ] Cross-phase performance budget met (see table above)
- [ ] Integration testing strategy executed and passing
- [ ] Rollback procedures validated in a non-production environment

#### Completion Checklist
- [ ] Both child proposals implemented
- [ ] End-to-end testing completed (including cross-phase integration scenarios)
- [ ] Documentation updated
- [ ] `HandleRunTyped` `TextWriter` change merged from AsyncExecutionProposal
- [ ] Status updated to "Implemented"