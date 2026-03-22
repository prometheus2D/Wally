# Runbook Scripting Language — Proposal

**Status**: ? **CANCELLED — Handled elsewhere**  
**Author**: System Architecture Team  
**Created**: 2024-01-15  
**Last Updated**: 2025-07-15  
**Cancelled**: 2025-07-15

*Template: [../../../../Templates/ProposalTemplate.md](../../../../Templates/ProposalTemplate.md)*

---

## CANCELLATION SUMMARY

This proposal has been **cancelled**. The WallyScript language design (variables, control flow, parallel execution, orchestration primitives) is being handled elsewhere outside the Wally project scope. 

Runbooks (`.wrb` files) will continue to operate as simple command-line / batch-file-style sequential command lists — one command per line, executed in order, with comment and placeholder support. This is the current implemented behaviour and no changes are planned.

### What was proposed
- Extend `.wrb` into a full scripting language with `$variables`, `if/else`, `while`, `foreach`, `function`, `parallel`, `pipeline`, `try/catch`, `retry` blocks
- Custom `ScriptLexer` ? `ScriptParser` ? `ScriptRunner` engine in `Wally.Core/Scripting/`
- Mailbox-aware orchestration primitives (`send-message`, `wait-for-reply`)

### Why cancelled
- Scripting language design and implementation is being handled elsewhere
- Current batch-file-style `.wrb` format is sufficient for existing runbook use cases
- Complexity vs. value trade-off: the simple format covers the primary use case (sequencing Wally commands)

### What remains
- `.wrb` files continue to work as they do today — sequential command execution
- Scintilla editor with `.wrb` syntax highlighting (from TextEditorIntegration) remains
- `HandleRunTypedAsync` with `TextWriter? output` parameter remains available for future use

---

**ARCHIVED**: This proposal has been cancelled and moved to the archive folder.
