# Runbook Syntax — Discussion

**Status**: In Progress
**Facilitator**: Ron (Owner)
**Participants**: Ron, Will Wright, Chris Sawyer, Notch
**Created**: 2025-01-01
**Last Updated**: 2025-01-01
**Target Resolution**: 2025-01-20

*Template: [../../Templates/DiscussionTemplate.md](../../Templates/DiscussionTemplate.md)*

---

## Context

Runbook files (`.wrb`) are flat command lists. We are adding shell integration and reusable loops with an injection point (`open`). This discussion records decisions as they are made, one question at a time.

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

### S5: Indentation defines structure — Ron
**From**: Ron
**Statement**: Block structure is expressed by indentation — the same mental model as Python. No closing keywords (`end`, `}`). A line back at column 0 ends the current block.
**Implications**: The parser is indent-aware. Mixed tabs/spaces are normalised. `open` at the wrong indent level is a parser error.

### S6: The workspace is the context — Ron *(supersedes earlier draft)*
**From**: Ron
**Statement**: Context does not flow between runbook steps through conversation history injection. The workspace — actor definitions, docs, templates, project state, file system — is the shared context that every command reads from and writes to. Each `run` command is independently grounded in the workspace. Each `shell` command acts on the workspace. There is no forwarding of previous output, no history accumulation between steps, no synthetic user-turn injection. The runbook is a sequence of independent workspace operations, exactly like commands in a terminal session.
**Implications**: `shell` output is not injected into `env.History`. Actor responses from one `run` step are not forwarded to the next. The workspace files and state are the connective tissue — if an actor's output is relevant to the next step, it should be written to the workspace (e.g. a doc, a task file) and the next actor reads it from there. This removes the whole question of "how does shell output reach the next actor" — it reaches them through the workspace, not through the runner.

### S7: Nested calls are supported from Phase 2 — Ron
**From**: Ron
**Statement**: A `call` body can contain any valid runbook statement, including other `call`s. Indentation depth handles the nesting naturally. There is no artificial limit on nesting depth.
**Implications**: The parser must be fully recursive from Phase 2. A `call` body is not a special restricted context — it is just a runbook block at a deeper indent level.

### S8: Active actor is a context closure, not a variable — Ron
**From**: Ron
**Statement**: The last actor explicitly named with `-a ActorName` in a scope stays active for subsequent `run` commands in that scope that omit `-a`. This is scope-based implicit resolution — like `this` in a method — not a variable assignment. A loop or call block inherits the enclosing scope's active actor and can override it locally without affecting the outer scope. The shorthand works in loop bodies and call bodies the same way it works at the top level.
**Implications**: The runner tracks active actor per scope frame. No variable syntax is introduced. Actor parameters to loops are not needed — the caller sets the active actor in the call body and the loop body reads it.

### S9: Loops are anonymous — naming and cross-file reuse are deferred — Ron
**From**: Ron
**Statement**: Loops do not have names in the current proposal. A `loop` defines a block; `call` immediately below it executes it. There is no registry, no name resolution, no visibility modifiers, no cross-file references. Multiple `call`s after the same `loop` definition each execute it independently. This is purely structural — the same as an anonymous function or a code block in any language.
**Implications**: S9 (loop visibility / access modifiers) is superseded. Named loops, `public`/`private`, `Loops/` file references, and qualified call syntax are all deferred to a later phase. The parser needs only an anonymous loop stack — no registry of any kind.

### S10: `call` references the most recently defined `loop` in scope — Ron
**From**: Ron
**Statement**: A `loop` definition sets the current loop reference for its scope. Any `call` in that scope executes whichever `loop` was most recently defined — the same "last loaded reference" model that applies to active actors, last shell output, and every other implicit context in the language. Defining a new `loop` updates the reference; all subsequent `call`s use the new one. This is consistent with how all languages handle implicit references — there is nothing special about it.
**Implications**: The parser/runner holds one loop reference slot per scope. Multiple `call`s after a single `loop` all reuse it. A `call` with no `loop` in scope is a parser error. A `loop` never followed by a `call` is valid and silent — same as any unused definition.

### S11: `open` is always a bare keyword — no default body — Ron
**From**: Ron
**Statement**: `open` is a single keyword on its own line. It never has an indented body. If the `call` supplies no block, `open` is a no-op — nothing runs there. There is no default-block syntax.
**Implications**: The parser never needs to distinguish "open with default" from "open without default". `open` is always one token. Simpler parser, simpler mental model.

### S12: Line-by-line execution — no whole-file parser — Ron
**From**: Ron
**Statement**: The runbook runner reads lines sequentially, one at a time. Each line is checked for a keyword prefix (`shell`, `loop`, `call`, `open`). If it has one, the new code handles it. Otherwise the line is passed unchanged to the existing `WallyCommands.DispatchCommand`. An indent-depth stack tracks scope for `loop`/`call` blocks. No AST, no lexer pass, no whole-file parse step — the existing line-by-line execution model is extended, not replaced.
**Implications**: `WallyRunbook.LoadFromFile` must preserve raw lines with indentation intact (`RawLines`). The existing `Commands` list (trimmed, indentation-stripped) continues to serve the simple-format path. `WallyCommands` gets a new `HandleRunbookScript` method that routes to the line-by-line script runner when `Format == "script"`. Blank lines inside a block are ignored and do not end the block.

### S13: Each `shell` line is an independent process — Ron
**From**: Ron
**Statement**: Every `shell` line spawns a new OS process. No state is shared between shell lines — environment variables set in one line are gone on the next, `cd` in one line has no effect on the next, and there is no persistent shell session across lines. CWD is always reset to the workspace WorkSource root for every shell line.
**Implications**: `cd` in a `shell` line is valid syntax but pointless — it will not affect subsequent lines. The runner simply starts a fresh process for each `shell` line with CWD set to WorkSource root. No multi-line shell block syntax is needed or supported. This is the same behaviour as a `Makefile` where each recipe line is its own shell.

### S14: Every runbook execution is logged with a unique instance GUID — Ron
**From**: Ron
**Statement**: Every runbook execution — simple or script format — generates a fresh `Guid.NewGuid()` (`runbookInstanceId`) at the point `HandleRunbook` or `HandleRunbookScript` is called. This GUID is stamped on every log entry for that execution: start, each step, each shell command, end, and any errors. The GUID is separate from the session GUID — a single session can contain many runbook executions, each independently traceable.
**Implications**: `SessionLogger` gets five new methods: `LogRunbookStart`, `LogRunbookStep`, `LogRunbookShell`, `LogRunbookEnd`, `LogRunbookError`. `LogEntry` gets a `RunbookInstanceId` field (nullable string — only populated for runbook entries). `WallyCommands` generates the instance GUID at execution start and threads it through all logger calls for that run. Nested runbook calls each get their own GUID.

---

## Open Questions

*(none currently)*

---

## Decisions Log

| Question | Decision | Rationale | Date | Owner |
|----------|----------|-----------|------|-------|
| `call` body targeting loop with no `open` | Silent discard, no warning | Syntactically valid constructs always run cleanly; no dead-code warnings | 2025-01-01 | @ron |
| Variables, conditionals, iteration in v1 | Deferred | Not needed for the first real use case; add only when a concrete workflow demands it | 2025-01-01 | @ron |
| Block structure syntax | Indentation — no closing keywords | Reads like Python; less ceremony; a line at column 0 ends any open block | 2025-01-01 | @ron |
| How shell/step output reaches subsequent commands | Workspace is the context — no history forwarding between steps | Each command is independently grounded in the workspace; output flows through workspace files, not the runner | 2025-01-01 | @ron |
| Nested `call` inside a `call` body | Supported — full nesting, no depth limit | A call body is just a runbook block at a deeper indent; the parser is fully recursive | 2025-01-01 | @ron |
| How loops reference actors without hardcoding | Active actor context closure — last `-a Name` in scope stays active; inherited by nested blocks | No variable syntax needed; scope-based resolution handles actor parameterisation naturally | 2025-01-01 | @ron |
| Loop naming and visibility | Deferred — loops are anonymous in v1 | Not needed yet; adds naming, resolution, and registry complexity before any concrete need exists | 2025-01-01 | @ron |
| Which `loop` does `call` reference when multiple are defined | Most recently defined `loop` in current scope | Consistent "last loaded reference" model; predictable from reading order | 2025-01-01 | @ron |
| `open` default body | None — `open` is always a bare keyword; no-op when caller supplies nothing | Simpler parser and mental model; nothing to distinguish | 2025-01-01 | @ron |
| Parser architecture: whole-file AST vs line-by-line | Line-by-line — existing execution model extended, not replaced | No AST needed; new keywords dispatched inline; Wally commands pass through `DispatchCommand` unchanged | 2025-01-01 | @ron |
| `shell` process model: persistent session vs independent process | Independent process per line — CWD always WorkSource root | Simpler implementation; consistent with Makefile model; no state leakage between lines | 2025-01-01 | @ron |
| Runbook execution logging | Instance GUID + structured log entries per execution | Every run independently traceable; fits existing `SessionLogger` category model; negligible overhead | 2025-01-01 | @ron |
