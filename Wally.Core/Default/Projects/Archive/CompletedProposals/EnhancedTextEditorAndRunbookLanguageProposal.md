# Enhanced Text Editor & Runbook Language — Proposal

**Status**: ? **COMPLETE (Phase 1) / ? CANCELLED (Phase 2)**  
**Author**: System Architecture Team  
**Created**: 2024-01-15  
**Last Updated**: 2025-07-15  
**Completed/Closed**: 2025-07-15

*Template: [../../../../Templates/ProposalTemplate.md](../../../../Templates/ProposalTemplate.md)*

---

## COMPLETION / CANCELLATION SUMMARY

This parent proposal is **closed**:

- **Phase 1 — Text Editor Integration**: ? **COMPLETE** — Scintilla.NET integrated, `ThemedEditorFactory` created, all panels migrated. Archived separately as `TextEditorIntegrationProposal.md`.
- **Phase 2 — Runbook Scripting Language**: ? **CANCELLED** — WallyScript language design is being handled elsewhere. Runbooks continue to operate as batch-file-style sequential command lists. Archived separately as `RunbookScriptingLanguageProposal.md` in `CancelledProposals/`.

### What was delivered (Phase 1)
- `Scintilla.NET` NuGet package integrated (MIT licensed, .NET 8)
- `ThemedEditorFactory.cs` with factory methods and full WallyTheme colour mapping
- JSON, Markdown, WallyRunbook, and PlainText language configurations
- `RunbookEditorPanel` and `TextFileEditorPanel` migrated to Scintilla
- Brace matching, code folding, line numbers, indentation guides

### What was cancelled (Phase 2)
- WallyScript language: `$variables`, `if/else`, `while`, `foreach`, `parallel`, `pipeline`, `try/catch`, `retry`
- `Wally.Core/Scripting/` engine (`ScriptLexer`, `ScriptParser`, `ScriptRunner`)
- Reason: scripting language is being handled elsewhere; `.wrb` files remain simple command lists

---

## ? IMPLEMENTATION ARTIFACTS

| File | Purpose | Status |
|------|---------|--------|
| `Wally.Forms/Controls/ThemedEditorFactory.cs` | Factory methods + theme mapping + language configs | ? Created |
| `Wally.Forms/Controls/Editors/TextFileEditorPanel.cs` | Generic Scintilla-backed text editor | ? Created |
| `Wally.Forms/Controls/Editors/RunbookEditorPanel.cs` | Migrated to Scintilla with `.wrb` syntax highlighting | ? Modified |
| `Wally.Forms/Wally.Forms.csproj` | Added `Scintilla.NET` NuGet reference | ? Modified |

---

**ARCHIVED**: This proposal is closed and has been moved to the archive folder.
