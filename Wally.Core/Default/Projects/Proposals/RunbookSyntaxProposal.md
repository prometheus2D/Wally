# Runbook Syntax — Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2025-01-01
**Last Updated**: 2025-07-15

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Runbook files (`.wrb`) are flat lists of Wally commands. They are good for simple sequences but cannot express reusable step blocks or shell integration. Any workflow that needs "run this shell tool, pass the output to an actor, or repeat a pattern for different contexts" requires a custom C# workaround.

---

## Resolution

Add two constructs to the `.wrb` format. Every existing runbook works unchanged.

| Construct | What it does |
|-----------|-------------|
| `shell <command>` | Run an OS shell command as an independent process |
| `loop { }` | Define a reusable block with its own body and one optional shot-slot |
| `call` | Execute the most recently defined loop (shot-slot = no-op) |
| `call { }` | Execute the most recently defined loop, running the shot body at `open` |

Blocks are delimited by **`{ }`** — opening brace on the same line as the keyword, closing brace on its own line. Indentation inside a block is for readability only; it carries no syntactic weight. This is the **only** block-delimiting syntax in the runbook language. There is no indentation-based scoping, no tab counting.

The runbook also has a **context closure**: the last actor named with `-a ActorName` stays active for subsequent commands in the same scope that omit `-a`. Each `loop { }` and `call { }` creates its own scope; the enclosing scope's active actor is inherited but can be overridden locally.

Naming, cross-file reuse, visibility modifiers, variables, conditionals, and iteration are all **deferred**.

---

## The Loop Model

A `loop { }` is best understood as a **class with two bodies**:

| Part | Syntax | Role |
|------|--------|------|
| **Loop body** | `loop { … }` | The statements that run every time this loop is called. Always present. |
| **Shot body** | `call { … }` | A single-invocation block injected at the `open` slot. Optional per call. |
| **Shot slot** | `open` | Keyword inside the loop body. Marks where the shot body runs. |

This structure is the same regardless of whether the loop runs once (single-shot) or iterates (future). The `{ }` syntax is consistent in both cases. A loop without `open` simply has no injection point — the shot body on the `call { }` is silently unused.

### Why `{ }` and not indentation

`{ }` braces are the **only** block delimiter in the runbook language. There is no indentation-based scoping. Inside any block — whether a loop body, a shot body, or a nested block inside either — indentation is cosmetic. The same brace syntax applies at every depth. A developer reading a `.wrb` file never needs to count spaces or tabs; every block boundary is explicit.

### Why this model matters

- **Single-shot Wally loops** (e.g. `run -l LoopName`) already have a fixed body of steps. The `loop { }` block is the runbook equivalent — a body of steps to execute, defined inline. A loop that only ever runs once still needs a body, and that body is still delimited with `{ }`.
- **The `call { }` shot body** is what varies per invocation. It lets the caller inject specific steps into the loop's `open` slot without changing the loop definition.
- **Nesting** is natural: the shot body of a `call { }` is itself a complete runbook block — delimited by `{ }` like any other block. It can contain another `loop { }` with its own body and its own `open`, and another `call { }` to drive that inner loop. Every level of nesting follows the same two-body model.

---

## Nesting — The Core Extensibility Mechanism

The most powerful aspect of the model is that **every block is a first-class runbook block**. There is no special context at any depth. This means:

```
outer loop { }       ? has a loop body (always runs)
  outer call { }     ? provides a shot body (injected at outer open)
    inner loop { }   ? nested inside the shot body; has its own loop body
      inner call { } ? provides a shot body for the inner loop
        …and so on, to any depth
```

Each level:
- Defines its own `loop { }` with its own body and optional `open`
- Provides its own `call { }` shot body that gets injected at the `open` of the loop it drives
- Inherits `ActiveActor` from its enclosing scope; can override locally

The C# executor holds a **scope frame** per block. The frame records `CurrentLoop` (the most recently defined `loop { }`) and `ActiveActor` (the last `-a Name` seen). When a `call { }` executes, it pushes a new frame, runs the loop body, and when it reaches `open`, executes the shot body inside that same frame. Because the shot body is a runbook block, it can push its own frames for any loops it defines and calls.

This is unbounded by design. There is no artificial depth limit. The same `loop { }` / `call { }` / `open` grammar applies at level 1 and at level 10.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [RunbookScriptingLanguageProposal](./RunbookScriptingLanguageProposal.md) | Sibling | Full WallyScript grammar; named loops, variables, iteration live there when needed |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Single-actor agent loop; runbooks orchestrate across multiple actors |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depends on | Required for cancellable loop execution |

---

## Phases

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | `shell` lines alongside Wally commands | 1-2 days | None |
| 2 | `loop { }` / `call` / `call { }` / `open` with brace-delimited structure; nested blocks to unlimited depth | 2-3 days | Phase 1 |

---

## Concepts

- `shell <cmd>`: Runs the remainder of the line as an **independent OS process**. CWD is always the workspace WorkSource root — reset for every `shell` line. No state persists between shell lines. Output is printed to the console. No injection into conversation history. Same model as a `Makefile` — each line is its own process.
- `loop { }`: Defines an anonymous reusable block. The **loop body** is the content between `{` and `}` — any number of Wally commands, `shell` lines, and at most one `open`. When parsed, the loop becomes the **current loop reference** for the enclosing scope. It is not executed at definition time.
- `open`: A bare keyword on its own line inside a loop body. Marks the **shot slot** — where the call's shot body runs. If the `call` supplies no `{ }` block, `open` is a no-op. One `open` per loop body.
- `call`: Executes the most recently defined `loop { }` in the current scope. No shot body — `open` is a no-op.
- `call { }`: Executes the most recently defined `loop { }`, injecting the **shot body** (the content between `{` and `}`) at the `open` slot. The shot body is a full runbook block — it can contain `shell`, `run`, `loop { }`, and `call` statements. Nesting is unlimited.
- **Active actor** (context closure): The last actor named with `-a ActorName` in the current scope stays active. Subsequent `run` commands that omit `-a` use it automatically. Each `loop { }` and `call { }` block inherits and can override locally.
- **Workspace as context**: Every command is independently grounded in the workspace — actor definitions, docs, templates, project files, file system. Context does not flow between steps through history injection.

---

## Syntax

### Level 0 — the happy path (works today, unchanged)

```wrb
# description: Quick code review
run "Review the auth module" -a Engineer
run "Summarize findings" -a BusinessAnalyst
```

### Level 1 — shell lines

Each `shell` line spawns an independent OS process with CWD set to WorkSource root.

```wrb
shell git log --oneline -10
run "Summarize the recent history" -a Engineer
```

```wrb
shell dotnet build
run "Review the build result" -a Engineer
```

### `shell` process rules

- Each `shell` line is a fresh process — no shared state with any other line.
- CWD is always WorkSource root.
- `cd` on a `shell` line is harmless but has no effect on subsequent lines.
- Exit code is not checked by default. *(Error handling is deferred.)*

### Level 2 — loop with shot body

The **loop body** is what always runs. The **shot body** on `call { }` is what varies per invocation.

```wrb
loop {
    run "Initial analysis" -a BusinessAnalyst
    open
    run "Final synthesis" -a BusinessAnalyst
}

call {
    run "Security review" -a SecurityExpert
}
```

The loop body runs fully: `Initial analysis`, then the shot body (`Security review`), then `Final synthesis`.

Calling without a shot body — `open` becomes a no-op:

```wrb
loop {
    run "Initial analysis" -a BusinessAnalyst
    open
    run "Final synthesis" -a BusinessAnalyst
}

call
```

A loop that never needs to inject anything still uses `{ }` for its body — the `open` is simply omitted:

```wrb
loop {
    run "Run every time, no injection point needed"
    run "And this too"
}

call
```

Multiple calls — each with a different shot body, each using the same loop body:

```wrb
loop {
    run "Review from your perspective"
    open
    run "Anything to add?"
}

call {
    run "Flag any blocking technical issues" -a Engineer
}

call {
    run "Check for security concerns" -a SecurityExpert
}
```

### Level 3 — nested loop inside a shot body

The shot body of a `call { }` is a full runbook block. It can contain its own `loop { }` and `call { }`, each with their own loop body and shot body. The inner `loop { }` uses the same `{ }` syntax as the outer one — there is no special syntax for nesting.

```wrb
loop {
    run "Outer loop — initial review" -a Engineer
    open
    run "Outer loop — synthesis" -a BusinessAnalyst
}

call {
    # shot body: a full runbook block containing its own loop structure
    loop {
        run "Inner loop — detailed analysis" -a Architect
        open
        run "Inner loop — confirm" -a Architect
    }

    call {
        run "Security deep dive" -a SecurityExpert
    }
}
```

Execution order for the outer call:
1. `Outer loop — initial review` (outer loop body, before `open`)
2. Inner loop executes — its own body with its own shot body injected at the inner `open`
3. `Outer loop — synthesis` (outer loop body, after `open`)

### Level 4 — three levels deep

The same model continues without limit. Each level adds its own `loop { }` inside the previous level's shot body:

```wrb
loop {
    run "Level 1 — start" -a ProjectManager
    open
    run "Level 1 — wrap up" -a ProjectManager
}

call {
    loop {
        run "Level 2 — technical review" -a Engineer
        open
        run "Level 2 — sign off" -a Engineer
    }

    call {
        loop {
            run "Level 3 — security analysis" -a SecurityExpert
            open
            run "Level 3 — risk summary" -a SecurityExpert
        }

        call {
            run "Level 3 shot — specific vulnerability check"
        }
    }
}
```

Execution flows inward through the nesting, then unwinds. At every level the rule is identical: run the loop body; at `open`, execute the shot body; continue the loop body after `open`.

---

## Block Syntax Rules

- `{` opens a block on the **same line** as its keyword: `loop {`, `call {`.
- `}` closes a block on its **own line**.
- Content between `{` and `}` is indented for readability — indentation has no syntactic meaning.
- Blank lines inside a block are ignored.
- `open` is a bare keyword on its own line inside a `loop { }` body — no braces. One `open` per loop body; a second `open` is a parser error.
- `open` outside a `loop { }` body is a parser error with line number.
- `open` inside a `call { }` shot body is a parser error — only loop bodies have shot slots.
- `call` with no `{ }` is valid — `open` in the loop becomes a no-op.
- `call` with no preceding `loop { }` in scope is a parser error with line number.
- `loop { }` with no following `call` is valid and silent — no warning.
- Multiple `call`s after one `loop { }` all reference the same loop. Defining a new `loop { }` shifts the current reference; all subsequent `call`s use the new one.
- Unmatched `{` or `}` is a parser error with line number.
- A `call { }` shot body is a complete runbook block and may itself contain `loop { }` / `call { }` pairs to any depth.

---

## C# Object Model

The parser produces a tree of `RunbookStatement` objects. `RunbookLoop` is the key class:

```csharp
// A loop { } definition — has a body and an optional shot slot (open).
class RunbookLoop : RunbookStatement
{
    List<RunbookStatement> Body { get; }   // the loop body — always runs
    bool HasOpenSlot { get; }              // true if the body contains an `open`
}

// A call — references the current loop ref; optional shot body.
class RunbookCall : RunbookStatement
{
    List<RunbookStatement>? ShotBody { get; }  // null = bare call; list = call { } with contents
    // ShotBody may itself contain RunbookLoop and RunbookCall nodes to arbitrary depth
}

// A shell line.
class RunbookShell : RunbookStatement
{
    string Command { get; }
}

// A Wally command (run, runbook, etc.)
class RunbookCommand : RunbookStatement
{
    string Line { get; }
}

// open — only valid inside RunbookLoop.Body
class RunbookOpen : RunbookStatement { }
```

At execution time, the runner holds a **scope frame** per block containing:
- `CurrentLoop` — the most recently defined `RunbookLoop` in this scope
- `ActiveActor` — the most recently named `-a ActorName` in this scope or any enclosing scope

When `RunbookCall` executes, it pushes a new scope frame (inheriting `ActiveActor`), runs `Loop.Body` step by step, and when it reaches `RunbookOpen` it executes `ShotBody` in place. Because `ShotBody` is itself a `List<RunbookStatement>`, it can contain `RunbookLoop` and `RunbookCall` nodes, each of which pushes their own frames — giving unlimited, uniform nesting.

---

## Active Actor and Context Closure

The last `-a ActorName` seen in a scope stays active for all `run` commands in that scope that omit `-a`. All other context comes from the workspace.

```wrb
run "Review the architecture" -a Architect
run "Any further concerns?"             # still Architect

loop {
    run "First pass"                    # inherits Architect from outer scope
    run "Deep dive" -a SecurityExpert   # overrides locally in loop body
    open
    run "Confirm findings"              # still SecurityExpert inside loop body
}

call {
    run "Security check"                # SecurityExpert (loop body scope)
    run "Follow-up" -a Engineer         # overrides in shot body

    loop {
        run "Inner pass" -a Engineer    # Engineer from enclosing shot body scope
        open
        run "Inner summary"             # still Engineer
    }

    call {
        run "Inner shot step" -a Architect   # overrides in inner shot body
    }
}

run "Synthesize" -a BusinessAnalyst     # back in outer scope
```

**The rule at every level**: active actor = last `-a Name` seen in the current scope or any enclosing scope.

---

## A Realistic Runbook

```wrb
# description: Release readiness check
# enabled: true

shell git log main --oneline -20
run "Review the recent changes and flag concerns" -a Architect
run "Any architectural red flags?"

loop {
    run "Review from your perspective"
    open
    run "Anything to add?"
}

call {
    run "Flag any blocking technical issues" -a Engineer
    run "Confirm no regressions"
}

call {
    run "Check for security concerns" -a SecurityExpert
    run "Rate the risk level"
}

run "Synthesize all reviews into a go/no-go verdict" -a BusinessAnalyst
```

Both `call { }` blocks use the same loop body. Each provides a different shot body at `open`. Each runs independently. All commands are grounded in the workspace — nothing is forwarded by the runner.

---

## Backward Compatibility

| Scenario | Behaviour |
|----------|-----------|
| Existing `.wrb` with only Wally commands and `#` comments | Unchanged — `shell`, `loop`, `call`, `open` do not appear in existing files |
| Existing `Loops/*.json` files | Unchanged — still load and run via existing `run -l Name` mechanism |
| `open` outside a `loop { }` body | Parser error with line number |
| `call { }` shot body on a loop with no `open` | Shot body silently unused; execution continues; no warning |

---

## Runbook Execution Logging

Every runbook execution is recorded in the session log with a unique instance GUID.

### Log entries per execution

| Category | When written | Key fields |
|----------|-------------|------------|
| `RunbookStart` | Before first statement | `runbookInstanceId`, `runbookName`, `format`, `lineCount`, `timestamp` |
| `RunbookStep` | Before each statement | `runbookInstanceId`, `stepIndex`, `keyword`, `timestamp` |
| `RunbookShell` | After each `shell` line | `runbookInstanceId`, `stepIndex`, `command`, `exitCode`, `durationMs` |
| `RunbookEnd` | After last statement | `runbookInstanceId`, `runbookName`, `stepCount`, `durationMs` |
| `RunbookError` | On failure or parser error | `runbookInstanceId`, `stepIndex`, `message` |

A fresh `Guid.NewGuid()` (`runbookInstanceId`) is generated per execution. Nested runbook calls each get their own GUID.

---

## Impact

| File | Change | Risk |
|------|--------|------|
| `Wally.Core/WallyRunbook.cs` | `RawSource` already stored; update `DetectScriptFormat` to recognise `shell`, `loop`, `call`, `open` keywords | Low |
| `Wally.Core/WallyCommands.cs` | Add `HandleRunbookScript` — brace-aware parser producing `RunbookStatement` tree; recursive executor with scope frames; `HandleRunbook` routes to it when `Format == "script"` | Medium |
| `Wally.Core/Scripting/RunbookStatement.cs` *(new)* | `RunbookStatement` base; `RunbookLoop`, `RunbookCall`, `RunbookShell`, `RunbookCommand`, `RunbookOpen` classes | Low |
| `Wally.Core/Logging/SessionLogger.cs` | Add `LogRunbookStart`, `LogRunbookStep`, `LogRunbookShell`, `LogRunbookEnd`, `LogRunbookError`; `LogEntry` gets `RunbookInstanceId` field | Low |

### Execution model

```
parse RawSource:
    brace-match loop { } bodies ? RunbookLoop (Body = list of statements, possibly nested)
    brace-match call { } bodies ? RunbookCall (ShotBody = list of statements, possibly containing RunbookLoop/RunbookCall)
    bare call            ? RunbookCall (ShotBody = null)
    shell <cmd>          ? RunbookShell
    open                 ? RunbookOpen (only valid inside RunbookLoop.Body)
    else                 ? RunbookCommand

execute (scope frame: CurrentLoop, ActiveActor):
    RunbookLoop   ? set CurrentLoop = this loop; do not execute body yet
    RunbookCall   ? push scope frame (inherit ActiveActor);
                    run CurrentLoop.Body step by step;
                      at RunbookOpen ? execute ShotBody in same scope frame (if supplied)
                        ShotBody may contain RunbookLoop/RunbookCall ? push additional frames recursively
                    pop scope frame
    RunbookShell  ? spawn process; CWD = WorkSource root; print output
    RunbookCommand? DispatchCommand(env, SplitArgs(Line))
    RunbookOpen   ? placeholder; resolved by RunbookCall at runtime (never executed standalone)
```

---

## Benefits

- `{ }` block syntax is unambiguous, editor-friendly, and familiar to any developer
- The two-body model (loop body + shot body) maps cleanly to a C# class hierarchy — straightforward to parse, test, and extend
- Nesting is unlimited and consistent — every level uses the same `loop { }` / `call { }` / `open` pattern with `{ }` delimiters
- `loop { }` syntax is the same for single-shot bodies and future iterating loops — no special cases
- A single-shot loop body still uses `{ }` — there is no ambiguity about when braces are required
- Existing runbooks work unchanged
- The workspace is the context — no fragile history-forwarding between steps
- Named loops, variables, and iteration can be added later without changing this foundation

---

## Risks

- **`shell` runs as the user**: CWD is WorkSource root; no privilege escalation.
- **One `open` per loop body**: Enforced by the parser. Multiple injection points require nested `loop { }` / `call { }` pairs.
- **`call` with no preceding `loop`**: Parser error — caught at parse time.
- **Unmatched braces**: Parser error with line number — caught at parse time.
- **Deep nesting**: Permitted but impractical beyond 3–4 levels for readability. No runtime limit — authors are responsible for sensible structure.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Update `DetectScriptFormat` in `WallyRunbook` for `shell`/`loop`/`call`/`open` (Phase 1) | High | ?? Not Started | @developer | 2025-01-20 | `RawSource` already stored |
| Implement `HandleRunbookScript` — `shell` dispatch (Phase 1) | High | ?? Not Started | @developer | 2025-01-20 | Independent process per line; CWD = WorkSource root; print output |
| Implement `RunbookStatement` class hierarchy (Phase 2) | High | ?? Not Started | @developer | 2025-01-29 | `RunbookLoop`, `RunbookCall`, `RunbookShell`, `RunbookCommand`, `RunbookOpen` |
| Implement brace-aware parser ? `RunbookStatement` tree (Phase 2) | High | ?? Not Started | @developer | 2025-01-29 | Brace-matching; `RunbookLoop.Body` and `RunbookCall.ShotBody` populated correctly; `ShotBody` may itself contain nested `RunbookLoop`/`RunbookCall` |
| Implement recursive executor with scope frames (Phase 2) | High | ?? Not Started | @developer | 2025-01-29 | `CurrentLoop` and `ActiveActor` per scope frame; shot body executed in same frame; inner loops push additional frames |
| Add runbook execution logging to `SessionLogger` and `WallyCommands` | High | ?? Not Started | @developer | 2025-01-20 | `RunbookStart`/`Step`/`Shell`/`End`/`Error`; `runbookInstanceId = Guid.NewGuid()` per execution |
| Backward compatibility regression tests | High | ?? Not Started | @qa | 2025-02-05 | `Format == "simple"` runbooks must go through existing `HandleRunbook` unchanged |
| Write 3 default example runbooks (shell+loop; loop+open+multi-call; nested 3-level) | Medium | ?? Not Started | @developer | 2025-02-05 | Ship in `Wally.Core/Default/Runbooks/` |
| Security review: shell CWD | High | ?? Not Started | @security | 2025-01-25 | Confirm WorkSource as CWD; document user owns the risk |

---

## Acceptance Criteria

### Must Have

- [ ] `shell <line>` executes OS command; output printed to console; CWD = WorkSource root; each line is an independent process
- [ ] `loop { }` parsed into `RunbookLoop` with correct `Body` and `HasOpenSlot`
- [ ] `call` and `call { }` parsed into `RunbookCall` with `ShotBody` null or populated correctly
- [ ] Executor runs loop body in full; injects shot body at `open` when present; no-op when absent
- [ ] A `call { }` shot body may contain `loop { }` / `call { }` pairs; nested execution is correct to unlimited depth
- [ ] One `open` per loop body enforced; `open` in shot body or outside loop body is a parser error
- [ ] `call` references most-recently-defined `loop { }`; new `loop { }` shifts the reference
- [ ] Multiple `call`s after one `loop { }` each execute independently
- [ ] Active actor closure: last `-a Name` in scope stays active; nested blocks inherit and can override
- [ ] Every runbook execution logged with `runbookInstanceId` GUID; `RunbookStart` and `RunbookEnd` at minimum
- [ ] All existing `.wrb` and `.json` files execute identically (zero regression)

### Should Have

- [ ] Parser error messages include line numbers
- [ ] 3 default example runbooks shipped with workspace setup (simple shell, multi-call loop, nested 3-level)
- [ ] Security review for `shell` CWD complete

### Completion Checklist

- [ ] All Must Have criteria met
- [ ] Backward compatibility tests green
- [ ] Status updated to "Approved"

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [RunbookScriptingLanguageProposal](./RunbookScriptingLanguageProposal.md) | Sibling | Named loops, variables, iteration live here when needed |
| [AutonomyLoopProposal](./AutonomyLoopProposal.md) | Sibling | Single-actor agent loop |
| [AsyncExecutionProposal](./AsyncExecutionProposal.md) | Depends on | Async dispatch for loop execution |
| [RunbookSyntaxDiscussion](./RunbookSyntaxDiscussion.md) | Spawned by | Design decisions that shaped this proposal |
