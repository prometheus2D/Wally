# Runbook Syntax — Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2025-01-01
**Last Updated**: 2025-01-01

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

Runbook files (`.wrb`) are flat lists of Wally commands. They are good for simple sequences but cannot express reusable step blocks or shell integration. Any workflow that needs "run this shell tool, pass the output to an actor, or repeat a pattern for different contexts" requires a custom C# workaround.

---

## Resolution

Add two constructs to the `.wrb` format. Every existing runbook works unchanged.

| Construct | What it does |
|-----------|-------------|
| `shell <command>` | Run an OS shell command inline with Wally commands |
| `loop` / `call` / `open` | Define an anonymous reusable block with one optional injection point |

Structure is expressed with **indentation** — the same mental model as Python. No `end` keyword, no braces, no names, no registry. A block is anything indented under a `loop` or `call` header.

The runbook also has a **context closure**: the last actor explicitly named with `-a ActorName` stays active for subsequent commands in the same scope that omit `-a`. Each `loop` / `call` block creates its own scope; the enclosing scope's active actor is inherited but can be overridden locally.

Naming, cross-file reuse, visibility modifiers, variables, conditionals, and iteration are all **deferred** — not needed for the first real use case.

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
| 2 | `loop` / `call` / `open` with indent-based structure | 2-3 days | Phase 1 |

---

## Concepts

- `shell <cmd>`: Runs the remainder of the line as an **independent OS process**. CWD is always the workspace WorkSource root — reset fresh for every `shell` line. No state persists between shell lines: environment variables, `cd`, or any other shell-session state set in one line has no effect on the next. Output is printed to the console. No injection into conversation history. This is the same model as a `Makefile` — each line is its own process.
- `loop`: Defines an anonymous block. Everything indented under it is the body. The loop is not executed when defined — it becomes the **current loop reference** for the scope.
- `open`: A single optional injection point inside a loop body. If the `call` supplies an indented block, that block runs here. If not, `open` is a no-op.
- `call`: Executes the **most recently defined `loop`** in the current scope. Everything indented under `call` fills the `open` slot. A `call` with no indented body is valid. Multiple `call`s in a row all reference the same most-recently-defined loop.
- **Active actor** (context closure): The last actor named with `-a ActorName` in the current scope stays active. Subsequent `run` commands that omit `-a` use it automatically. Nested blocks inherit and can override locally.
- **Workspace as context**: Every command — `run`, `shell`, or otherwise — is independently grounded in the workspace (actor definitions, docs, templates, project files, file system). Context does not flow between steps through history injection. If one step's output is relevant to a later step, the actor writes it to the workspace and the later actor reads it from there.

---

## Syntax

### Level 0 — the happy path (works today, unchanged)

```wrb
# description: Quick code review
run "Review the auth module" -a Engineer
run "Summarize findings" -a BusinessAnalyst
```

### Level 1 — shell lines

Each `shell` line spawns an independent OS process with CWD set to the workspace WorkSource root. No state is shared between shell lines — `cd`, environment variables, and any other session state are gone after each line. Shell lines act on the workspace (creating files, running tools, reading state) and print their output to the console.

```wrb
shell git log --oneline -10
run "Summarize the recent history" -a Engineer
```

```wrb
shell dotnet build
run "Review the build result" -a Engineer
```

The Engineer reads the workspace — project files, actor docs, build artefacts — not forwarded shell output. If shell output needs to explicitly reach an actor, write a step that saves it to a workspace file first.

### `shell` process rules

- Each `shell` line is a fresh process — no shared state with any other line.
- CWD is always WorkSource root.
- `cd` on a `shell` line is harmless but has no effect on subsequent lines.
- Exit code is not checked by default — the runbook continues regardless. *(Error handling is deferred.)*

### Level 2 — loops

A `loop` defines a block. `call` immediately below it executes it. Indentation is the only structure — no names, no keywords to close the block.

```wrb
loop
    run "Initial analysis" -a BusinessAnalyst
    open
    run "Final synthesis" -a BusinessAnalyst

call
    run "Security review" -a SecurityExpert
```

The `call` body fills the `open` slot. Calling without a body:

```wrb
loop
    run "Initial analysis" -a BusinessAnalyst
    open
    run "Final synthesis" -a BusinessAnalyst

call
```

`open` is a no-op — the loop runs without injection.

### Level 3 — nested loops

A `call` body is just another runbook block at a deeper indent. It can contain a `loop` / `call` pair of its own.

```wrb
loop
    run "Initial review" -a Engineer
    open
    run "Synthesis" -a BusinessAnalyst

call
    loop
        run "Detailed analysis" -a Architect
        open

    call
        run "Security deep dive" -a SecurityExpert
```

---

## Active Actor and Context Closure

The last `-a ActorName` seen in a scope stays active. Any `run` that omits `-a` uses it. This is the only form of "forwarding" in the runbook language — it tracks which actor to use, nothing else. All other context comes from the workspace.

```wrb
run "Review the architecture" -a Architect
run "Any further concerns?"             # still Architect
run "Summarize" -a BusinessAnalyst
run "Confirm the summary is complete"   # now BusinessAnalyst
```

A `loop` or `call` block inherits the enclosing scope's active actor and can override locally without affecting the outer scope.

```wrb
run "Set the scene" -a Architect

loop
    run "First pass"                    # inherits Architect
    run "Deep dive" -a SecurityExpert   # overrides locally
    open
    run "Confirm findings"              # still SecurityExpert inside loop

call
    run "Security check"                # SecurityExpert from loop scope
    run "Follow-up" -a Engineer         # overrides for this call body

run "Synthesize" -a BusinessAnalyst     # back in outer scope: BusinessAnalyst
```

**The rule**: the active actor is the last `-a Name` seen in the current scope or any enclosing scope. Natural reading order of the file is the only mechanism.

---

## A Realistic Runbook

```wrb
# description: Release readiness check
# enabled: true

shell git log main --oneline -20
run "Review the recent changes in the workspace and flag concerns" -a Architect
run "Any architectural red flags?"

loop
    run "Review from your perspective"
    open
    run "Anything to add?"

call
    run "Flag any blocking technical issues" -a Engineer
    run "Confirm no regressions"

call
    run "Check for security concerns" -a SecurityExpert
    run "Rate the risk level"

run "Synthesize all reviews into a go/no-go verdict" -a BusinessAnalyst
```

Each command is independently grounded in the workspace. The `shell` line runs a git command against the workspace. Each `run` step draws on actor definitions, project docs, and workspace state — not on what the previous step printed. Both `call` blocks reference the same `loop` — the most recently defined one — and each runs independently with its own injected block and active actor scope.

---

## Indentation Rules

- **4 spaces or 1 tab** — both accepted; consistent within a file is good practice.
- Blank lines inside a block are ignored — they do not end the current block.
- A **non-blank** line at column 0 ends all open blocks in the current scope.
- A `call` body can contain any valid runbook statement, including another `loop` / `call` pair. There is no depth limit.
- `open` outside a `loop` body is a parser error with line number.
- A `loop` with no following `call` is valid — it defines a block and does nothing. No warning.
- Multiple `call`s after a `loop` all reference the same loop — the most recently defined one in the current scope. Defining a new `loop` updates the current reference; all subsequent `call`s then reference the new one.
- A `call` with no preceding `loop` in scope is a parser error with line number.
- `open` is always a bare keyword on its own line — no indented body, no default block. If the `call` supplies nothing, `open` is a no-op. Parser error if `open` appears with indented content beneath it.

---

## Backward Compatibility

| Scenario | Behaviour |
|----------|-----------|
| Existing `.wrb` with only Wally commands and `#` comments | Unchanged — `shell`, `loop`, `call`, `open` do not appear in existing files |
| Existing `Loops/*.json` files | Unchanged — still load and run via existing `run -l Name` mechanism |
| `open` outside a `loop` body | Parser error with line number |
| `call` body on a loop with no `open` | Body silently unused; execution continues; no warning |

---

## Runbook Execution Logging

Every runbook execution — whether simple-format or script-format — is recorded in the session log. Each execution is a distinct instance with its own GUID so individual runs can be traced, correlated, and audited independently of the session.

### Log entries per execution

| Category | When written | Key fields |
|----------|-------------|------------|
| `RunbookStart` | Immediately before the first line executes | `runbookInstanceId`, `runbookName`, `format`, `lineCount`, `timestamp` |
| `RunbookStep` | Before each line (command, shell, loop entry, call entry) | `runbookInstanceId`, `stepIndex`, `line`, `keyword`, `timestamp` |
| `RunbookShell` | After each `shell` line completes | `runbookInstanceId`, `stepIndex`, `command`, `exitCode`, `durationMs`, `timestamp` |
| `RunbookEnd` | After the last line completes normally | `runbookInstanceId`, `runbookName`, `stepCount`, `durationMs`, `timestamp` |
| `RunbookError` | When a step fails or a parser error occurs | `runbookInstanceId`, `stepIndex`, `line`, `message`, `timestamp` |

### `runbookInstanceId`

A fresh `Guid.NewGuid()` is generated at the start of every `HandleRunbook` / `HandleRunbookScript` call. It is included in every log entry for that execution. This makes it possible to:

- Filter the session log to a single run across rotated log files
- Correlate nested runbook calls (each level has its own instance GUID)
- Reconstruct the exact sequence of steps in a completed or failed run

### Log entry format (extends existing `LogEntry`)

```json
{
  "timestamp": "2025-01-20T14:32:05.123Z",
  "sessionId": "a1b2c3d4...",
  "category": "RunbookStart",
  "runbookInstanceId": "f7e6d5c4-b3a2-1098-7654-321098fedcba",
  "runbookName": "ReleaseReadinessCheck",
  "format": "script",
  "lineCount": 18
}
```

```json
{
  "timestamp": "2025-01-20T14:32:05.450Z",
  "sessionId": "a1b2c3d4...",
  "category": "RunbookShell",
  "runbookInstanceId": "f7e6d5c4-b3a2-1098-7654-321098fedcba",
  "stepIndex": 1,
  "command": "git log main --oneline -20",
  "exitCode": 0,
  "durationMs": 312
}
```

### Impact on existing logger

| File | Change | Risk |
|------|--------|------|
| `Wally.Core/Logging/SessionLogger.cs` | Add `LogRunbookStart`, `LogRunbookStep`, `LogRunbookShell`, `LogRunbookEnd`, `LogRunbookError` methods; `LogEntry` gets `RunbookInstanceId` field | Low |
| `Wally.Core/WallyCommands.cs` | Generate `runbookInstanceId = Guid.NewGuid()` at start of `HandleRunbook` and `HandleRunbookScript`; pass to all logger calls within that execution | Low |

---

## Impact

| File | Change | Risk |
|------|--------|------|
| `Wally.Core/WallyRunbook.cs` | Preserve raw lines with indentation in `RawLines` (alongside existing `Commands`); update `DetectScriptFormat` to recognise `shell`, `loop`, `call`, `open` | Low |
| `Wally.Core/WallyCommands.cs` | Add `HandleRunbookScript` — line-by-line runner with indent-depth stack, loop reference slot, and active actor slot; `HandleRunbook` routes to it when `Format == "script"`; `shell` dispatches to OS, prints output, no history injection | Medium |
| `Wally.Core/Logging/SessionLogger.cs` | Add `LogRunbookStart`, `LogRunbookStep`, `LogRunbookShell`, `LogRunbookEnd`, `LogRunbookError` methods; `LogEntry` gets `RunbookInstanceId` field | Low |
| `Wally.Core/WallyCommands.cs` | Generate `runbookInstanceId = Guid.NewGuid()` at start of `HandleRunbook` and `HandleRunbookScript`; pass to all logger calls within that execution | Low |

No new files are required for Phase 1 and 2. The line-by-line runner reads one line at a time, checks the trimmed keyword prefix, and either handles it directly (new keywords) or passes it unchanged to the existing `DispatchCommand` (Wally commands). No history forwarding between steps — the workspace is the shared context.

### Line-by-line execution model

```
for each raw line in RawLines:
    indent  = measure leading whitespace
    keyword = first token of trimmed line

    if keyword == "shell"  ? run OS command; print output to console
    if keyword == "loop"   ? record body lines as current loop ref for scope
    if keyword == "call"   ? run current loop ref, passing indented body as open-slot block
    if keyword == "open"   ? mark injection point — filled by call body if present
    else                   ? DispatchCommand(env, SplitArgs(line))  ? unchanged path
```

Blank lines inside a block are ignored. A non-blank line at column 0 ends all open blocks.

---

## Benefits

- Existing runbooks work unchanged
- `shell` makes any OS tool a first-class runbook step — acting on the workspace, not injecting into AI context
- `loop` + `open` enables reusable patterns without naming, registry, or cross-file machinery
- Multiple `call`s on the same `loop` makes reuse obvious from structure alone
- Active actor closure means you name an actor once and it flows through the block naturally
- The workspace is the context — no fragile history-forwarding between steps; actors are grounded in files and state, not in what the previous step printed
- Plain text — readable, diffable, storable in version control
- Named loops, variables, and iteration can be added later without changing this foundation

---

## Risks

- **`shell` runs as the user**: CWD is the project WorkSource root; no privilege escalation beyond what the user already has in a terminal.
- **One `open` per loop**: Enforced by the parser. Multiple injection points require nested `loop`/`call` pairs.
- **Indentation sensitivity**: Mixed tabs and spaces normalised at load time (4 spaces per tab).
- **`call` referencing which `loop`**: `call` always references the most recently defined `loop` in the current scope. Defining a new loop shifts the reference. This is predictable and consistent with how any language handles the "last assigned" reference.
- **`shell` process isolation**: Each `shell` line is an independent process. `cd` between lines is harmless but ineffective. CWD is always reset to WorkSource root.

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Update `WallyRunbook.LoadFromFile` to preserve raw lines with indentation in `RawLines` (Phase 1) | High | ?? Not Started | @developer | 2025-01-20 | Alongside existing `Commands`; update `DetectScriptFormat` for `shell`/`loop`/`call`/`open` |
| Implement `HandleRunbookScript` in `WallyCommands` — `shell` dispatch (Phase 1) | High | ?? Not Started | @developer | 2025-01-20 | Line-by-line; `shell` runs OS command and prints output; CWD = WorkSource root; no history injection |
| Implement `loop` / `call` / `open` indent-state stack in `HandleRunbookScript` (Phase 2) | High | ?? Not Started | @developer | 2025-01-29 | Anonymous loop ref slot per scope; open injection; fully recursive via call body |
| Implement active actor scope tracking in `HandleRunbookScript` (Phase 2) | High | ?? Not Started | @developer | 2025-01-29 | Track last `-a Name` per scope frame; inherit on enter; restore on exit |
| Backward compatibility regression tests | High | ?? Not Started | @qa | 2025-02-05 | `Format == "simple"` runbooks must go through existing `HandleRunbook` unchanged |
| Write 2 default example runbooks (shell+loop, loop+open) | Medium | ?? Not Started | @developer | 2025-02-05 | Ship in `Wally.Core/Default/Runbooks/` |
| Security review: shell CWD | High | ?? Not Started | @security | 2025-01-25 | Confirm WorkSource as CWD; document user owns the risk |
| Add runbook execution logging to `SessionLogger` and `WallyCommands` | High | ?? Not Started | @developer | 2025-01-20 | `RunbookStart`/`Step`/`Shell`/`End`/`Error` categories; `runbookInstanceId = Guid.NewGuid()` per execution; add `RunbookInstanceId` to `LogEntry` |

---

## Acceptance Criteria

### Must Have

- [ ] `shell <line>` executes OS command; output printed to console; CWD is WorkSource root; no injection into `env.History`
- [ ] `loop` / `call` / `open` works with indent-based anonymous structure; `call` references most-recently-defined `loop` in current scope; new `loop` shifts the reference
- [ ] `open` slot: block supplied ? runs at open; no block ? no-op; one `open` per loop enforced
- [ ] Multiple `call`s after one `loop` each execute independently
- [ ] Active actor closure: last `-a Name` in scope stays active; `run` without `-a` uses it; nested blocks inherit and can override
- [ ] All existing `.wrb` and `.json` files execute identically (zero regression)
- [ ] Every runbook execution logged: `RunbookStart` (with `runbookInstanceId` GUID and timestamp) and `RunbookEnd` at minimum; `RunbookShell` and `RunbookError` for script-format runs

### Should Have

- [ ] Parser error messages include line numbers
- [ ] 2 default example runbooks shipped with workspace setup
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
