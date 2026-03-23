# Runbook Syntax — Discussion

**Status**: Resolved
**Facilitator**: Ron (Owner)
**Participants**: Ron, Will Wright, Chris Sawyer, Notch
**Created**: 2025-01-01
**Last Updated**: 2025-07-15
**Target Resolution**: 2025-01-20 ? Resolved 2025-07-15

*Template: [../../Templates/DiscussionTemplate.md](../../Templates/DiscussionTemplate.md)*

---

## Context

Runbook files (`.wrb`) are flat command lists. We are adding shell integration and reusable loops with an injection point (`open`). This discussion records decisions as they are made, one question at a time.

The key design problems resolved here are:

- How to delimit block bodies in a syntax-unambiguous, editor-friendly way.
- How to model a loop that can run once (single-shot) or iterate (future) without separate syntax for each case.
- How to allow the caller of a loop to inject a variable body at a specific slot (`open`) without changing the loop definition.
- How to nest loop constructs to arbitrary depth using the same model at every level.

All questions are now resolved. This discussion is archived. The spawned proposal is [RunbookSyntaxProposal.md](./RunbookSyntaxProposal.md).

---

## Statements & Positions

### S1: Backward compatibility is non-negotiable — Ron
**From**: Ron
**Statement**: A runbook that is just a list of `run` commands must keep working with zero changes and zero new knowledge.
**Implications**: No required header syntax. No required keywords. A file with only Wally commands is a valid runbook forever.

### S2: Simple but explicit — Ron
**From**: Ron
**Statement**: When there are two options and one is simpler and one is more explicit, prefer the explicit one even if slightly redundant. Clarity beats brevity in a file you might not read for three months.
**Implications**: Loop files use the same `loop Name` header as inline declarations, even though the filename already provides the name.

### S3: Valid syntax always runs — Ron
**From**: Ron
**Statement**: If the syntax is correct it should execute without complaint, even if the result is a no-op. The language does not warn about dead code any more than C# or Java does.
**Implications**: `call` with a body targeting a loop with no `open` is silently unused. A loop never called is silently ignored. No warnings.

### S4: Start simple, add complexity only when there is a concrete need — Ron
**From**: Ron
**Statement**: Variables, conditionals, and iteration are not needed for the first real use case. The language should start as: Wally commands + shell lines + loops with an open slot. Everything else is deferred until a real workflow demands it.
**Implications**: Variables (`name = value`), `if`/`else`, and `each` are removed from the current proposal. They may be added later but only driven by an actual use case, not anticipation.

### S5: Braces delimit block structure — Ron *(supersedes indentation-based draft)*
**From**: Ron
**Statement**: Block bodies in the runbook language are delimited by `{ }` — opening brace on the same line as the keyword (`loop {`, `call {`), closing brace on its own line. Indentation inside a block is for human readability only; it has no syntactic meaning. This applies equally to `loop { }` bodies and `call { }` injection blocks. `open` remains a bare keyword — it does not take braces.
**Implications**: No indentation sensitivity. No tab/space normalisation needed. Unmatched braces are a parser error with line number. The parser needs a brace-matching stack, not an indent-depth stack. `loop { }` body syntax is the same whether the loop runs once (single-shot) or iterates in future — no special syntax for either case.

### S6: The workspace is the context — Ron *(supersedes earlier draft)*
**From**: Ron
**Statement**: Context does not flow between runbook steps through conversation history injection. The workspace — actor definitions, docs, templates, project state, file system — is the shared context that every command reads from and writes to. Each `run` command is independently grounded in the workspace. Each `shell` command acts on the workspace. There is no forwarding of previous output, no history accumulation between steps, no synthetic user-turn injection. The runbook is a sequence of independent workspace operations, exactly like commands in a terminal session.
**Implications**: `shell` output is not injected into `env.History`. Actor responses from one `run` step are not forwarded to the next. The workspace files and state are the connective tissue — if an actor's output is relevant to the next step, it should be written to the workspace (e.g. a doc, a task file) and the next actor reads it from there.

### S7: Nested calls are supported — Ron
**From**: Ron
**Statement**: A `call { }` injection block can contain any valid runbook statement, including another `loop { }` / `call` pair. There is no artificial limit on nesting depth.
**Implications**: The runner is fully recursive. A `call { }` body is not a special restricted context — it is just a runbook block resolved at the time `open` is reached.

### S8: Active actor is a context closure, not a variable — Ron
**From**: Ron
**Statement**: The last actor explicitly named with `-a ActorName` in a scope stays active for subsequent `run` commands in that scope that omit `-a`. This is scope-based implicit resolution — not a variable assignment. A `loop { }` or `call { }` block inherits the enclosing scope's active actor and can override it locally without affecting the outer scope.
**Implications**: The runner tracks active actor per scope frame. No variable syntax is introduced.

### S9: Loops are anonymous — naming and cross-file reuse are deferred — Ron
**From**: Ron
**Statement**: Loops do not have names. `loop { }` defines a block and becomes the current loop reference for the scope. There is no registry, no name resolution, no cross-file references. Multiple `call`s after the same `loop { }` each execute independently.
**Implications**: The runner holds one loop reference slot per scope. Named loops, `Loops/` file references, and visibility modifiers are deferred to a later phase.

### S10: `call` references the most recently defined `loop { }` in scope — Ron
**From**: Ron
**Statement**: A `loop { }` definition sets the current loop reference for its scope. Any `call` or `call { }` in that scope executes whichever `loop { }` was most recently defined. Defining a new `loop { }` updates the reference; all subsequent `call`s use the new one.
**Implications**: One loop reference slot per scope. `call` with no preceding `loop { }` is a parser error. `loop { }` never called is valid and silent.

### S11: `open` is always a bare keyword — no braces, no body — Ron
**From**: Ron
**Statement**: `open` is a single keyword on its own line inside a `loop { }` body. It never takes braces or an indented body. If `call` supplies no `{ }` block, `open` is a no-op. One `open` per loop body; a second `open` is a parser error.
**Implications**: The parser treats `open` as a single token. No ambiguity. `open` outside a `loop { }` body is a parser error.

### S12: Brace-aware execution — no whole-file AST — Ron *(supersedes indent-stack draft)*
**From**: Ron
**Statement**: The runner scans `RawSource` for `loop { }` bodies using brace-matching, builds an ordered statement list, then executes statements in sequence. Each Wally command passes through the existing `DispatchCommand` unchanged. No AST, no separate lexer pass. `RawSource` is already stored in `WallyRunbook`; no new fields needed.
**Implications**: The parser needs a brace-matching stack (simpler and more standard than an indent-depth stack). `WallyCommands` gets `HandleRunbookScript`; `HandleRunbook` routes to it when `Format == "script"`.

### S13: Each `shell` line is an independent process — Ron
**From**: Ron
**Statement**: Every `shell` line spawns a new OS process. No state is shared between shell lines. CWD is always reset to WorkSource root for every shell line. Same model as a `Makefile`.
**Implications**: `cd` in a `shell` line is harmless but has no effect on subsequent lines.

### S14: Every runbook execution is logged with a unique instance GUID — Ron
**From**: Ron
**Statement**: Every runbook execution generates a fresh `Guid.NewGuid()` (`runbookInstanceId`) at the point `HandleRunbook` or `HandleRunbookScript` is called. This GUID is stamped on every log entry for that execution.
**Implications**: `SessionLogger` gets five new methods. `LogEntry` gets a nullable `RunbookInstanceId` field. Nested runbook calls each get their own GUID.

### S15: `loop { }` has two bodies — the loop body and the shot body — Ron
**From**: Ron
**Statement**: A `loop { }` block is best understood as a class with two distinct bodies. The **loop body** is the `{ }` content on the `loop` keyword — the steps that always run when the loop is called. The **shot body** is the `{ }` content on `call { }` — the steps injected at the `open` slot for that specific invocation. These two bodies are independent: the loop body is defined once, the shot body varies per call. A `call` with no `{ }` means no shot body — `open` is a no-op. This two-body model applies at every level of nesting: a shot body is a full runbook block and can contain its own `loop { }` (with its own loop body and `open`) and its own `call { }` (with its own shot body).
**Implications**: The C# parser produces a `RunbookLoop` class with a `Body` list and a `HasOpenSlot` flag, and a `RunbookCall` class with a nullable `ShotBody` list. The executor runs the loop body in sequence; when it reaches `RunbookOpen` it executes the shot body in place. Scope frames hold `CurrentLoop` and `ActiveActor`. A new file `Wally.Core/Scripting/RunbookStatement.cs` defines the statement class hierarchy. This model is consistent whether the loop runs once or iterates — no special syntax for either case.

### S16: `{ }` braces are the universal block delimiter — no tab-based scoping — Ron
**From**: Ron
**Statement**: The `{ }` brace syntax is the single, unambiguous way to delimit any block in the runbook language — whether it is a loop body, a shot body, or a nested structure inside either. Tabs and indentation inside any block are purely cosmetic. The language is brace-scoped, not indent-scoped. A loop that does not need to iterate still defines its body with `{ }`. A single-shot call that injects a fixed set of steps into `open` still uses `call { }`. This is not special syntax for iteration — it is the only block syntax.
**Implications**: Authors who read a `.wrb` file never need to count spaces or tabs to understand structure. Every block boundary is explicit. The same mental model applies to the outermost runbook body, to a loop's body, to a call's shot body, and to any loop or call nested inside a shot body.

### S17: Nesting depth is unlimited and every level uses the same model — Ron
**From**: Ron
**Statement**: A shot body supplied by `call { }` is a complete runbook block. It can contain `run` commands, `shell` lines, another `loop { }` with its own body and its own `open`, and another `call` or `call { }` to drive that inner loop. The inner loop's shot body is itself a complete runbook block that can contain another `loop { }`, and so on. There is no limit on nesting depth. Every level uses exactly the same two-body model: `loop { }` defines the body that always runs; `call { }` provides the body injected at `open`. No level has special syntax.
**Implications**: The C# executor is fully recursive. The scope stack grows one frame per `call { }` execution. `CurrentLoop` and `ActiveActor` are resolved per frame. A deeply nested runbook is no different from a shallow one — it is just more frames on the stack. Runbook authors can build arbitrarily complex orchestration from a small set of orthogonal primitives.

---

## Open Questions

*(none — all questions resolved; see Decisions Log below)*

---

## Decisions Log

| Question | Decision | Rationale | Date | Owner |
|----------|----------|-----------|------|-------|
| `call { }` shot body targeting loop with no `open` | Silent discard, no warning | Syntactically valid constructs always run cleanly; no dead-code warnings | 2025-01-01 | @ron |
| Variables, conditionals, iteration in v1 | Deferred | Not needed for the first real use case; add only when a concrete workflow demands it | 2025-01-01 | @ron |
| Block structure syntax | `{ }` braces — opening brace same line as keyword, closing brace own line | Unambiguous; no indentation sensitivity; familiar to any developer | 2025-01-01 | @ron |
| How shell/step output reaches subsequent commands | Workspace is the context — no history forwarding between steps | Each command is independently grounded in the workspace | 2025-01-01 | @ron |
| Nested `call { }` inside a shot body | Supported — full nesting, no depth limit | Shot body is a full runbook block; runner is fully recursive | 2025-01-01 | @ron |
| How loops reference actors without hardcoding | Active actor context closure — last `-a Name` in scope stays active; inherited by nested blocks | No variable syntax needed; scope-based resolution handles actor parameterisation naturally | 2025-01-01 | @ron |
| Loop naming and visibility | Deferred — loops are anonymous in v1 | Not needed yet; adds naming, resolution, and registry complexity before any concrete need exists | 2025-01-01 | @ron |
| Which `loop { }` does `call` reference when multiple are defined | Most recently defined `loop { }` in current scope | Consistent "last loaded reference" model; predictable from reading order | 2025-01-01 | @ron |
| `open` default body | None — `open` is a bare keyword; no-op when caller supplies no `{ }` block | Simpler parser and mental model | 2025-01-01 | @ron |
| Parser architecture | Brace-matching scan ? `RunbookStatement` tree ? recursive executor with scope frames | Clean class hierarchy; straightforward to parse, test, and extend | 2025-01-01 | @ron |
| `shell` process model | Independent process per line — CWD always WorkSource root | Simpler implementation; consistent with Makefile model; no state leakage | 2025-01-01 | @ron |
| Runbook execution logging | Instance GUID + structured log entries per execution | Every run independently traceable; fits existing `SessionLogger` category model | 2025-01-01 | @ron |
| Loop structure model | Two-body model: loop body (always runs) + shot body (injected at `open` per call) | Maps cleanly to `RunbookLoop`/`RunbookCall` C# classes; consistent at every nesting level | 2025-01-01 | @ron |
| Tab vs. brace scoping | `{ }` braces only — tabs are cosmetic | Eliminates indentation sensitivity; brace-matching is simpler and unambiguous | 2025-07-15 | @ron |
| Nesting model universality | Unlimited depth; every level uses identical two-body model | Shot body is always a full runbook block; no special rules at any depth | 2025-07-15 | @ron |

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| All discussion questions | High | ? Complete | @ron | 2025-01-20 | All resolved; see Decisions Log |

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [RunbookSyntaxProposal.md](./RunbookSyntaxProposal.md) | Spawned | All resolved decisions fed into this proposal |
| [RunbookScriptingLanguageProposal.md](./RunbookScriptingLanguageProposal.md) | Sibling | Named loops, variables, iteration — deferred to here if ever needed |
