# Wally.Core

Domain library for Wally. No CLI, no UI — embed in any .NET 8 application.

## Key Types

### `Actor`

Abstract base class for all actors. Each actor carries RBA components (Role, AcceptanceCriteria, Intent) and generates structured prompts.

**Prompt generation** — `GeneratePrompt(userPrompt)` wraps user input inside the actor's RBA context:

```csharp
string prompt = actor.GeneratePrompt("Add input validation");
// Output:
// # Actor: Developer
// ## Role
// Act as an expert software developer...
// ## Acceptance Criteria
// Code must compile without errors...
// ## Intent
// Implement the requested feature...
//
// ## Prompt
// Add input validation
```

**Pipeline** — `Act(prompt)` runs: Setup ? ProcessPrompt ? (ApplyCodeChanges | Respond).

Documentation files in `.wally/Docs/` and `.wally/Actors/<Name>/Docs/` are accessible to `gh copilot` via `--add-dir` (native file access). They are not injected into prompts.

### `CopilotActor`

Default actor implementation. Invokes `gh copilot -p` directly using `ProcessStartInfo.ArgumentList` (no shell, no escaping). Working directory is set to WorkSource so Copilot sees the target codebase. `--add-dir` grants read access to the entire WorkSource tree.

```csharp
var actor = env.GetActor("Developer");
actor.ModelOverride = "claude-sonnet-4";   // one-shot override
string response = actor.Act("Explain this module");
// actor.ModelOverride is now null — next call uses DefaultModel
```

### `WallyLoop`

Iterative execution loop. Each `gh copilot -p` call is stateless — the loop carries context forward explicitly by embedding the previous response in the next prompt.

| Piece | Purpose |
|---|---|
| **Action** | `Func<string, string>` — receives a prompt, returns a result |
| **StartPrompt** | Prompt for the first iteration |
| **ContinuePrompt** | `Func<string, string>` — receives previous result, returns next prompt. Falls back to using the result directly when null. |
| **MaxIterations** | Hard ceiling on iterations (default: 10) |

Stop keywords detected in the response:
- `[LOOP COMPLETED]` — task finished successfully
- `[LOOP ERROR]` — actor detected an error

```csharp
var loop = new WallyLoop(
    action:         prompt => actor.Act(prompt),
    startPrompt:    "Refactor error handling",
    continuePrompt: prev => $"Continue from:\n{prev}\nRespond with [LOOP COMPLETED] when done.",
    maxIterations:  5
);
loop.Run();
// loop.Results         — all iteration results
// loop.ExecutionCount  — how many iterations ran
// loop.StopReason      — Completed, Error, or MaxIterations
```

### `WallyWorkspace`

Owns the workspace layout on disk:

```
<WorkSource>/                   e.g. C:\repos\MyApp
    .wally/                     WorkspaceFolder
        wally-config.json
        Docs/                   Workspace-level documentation
        Actors/
            <ActorName>/
                actor.json      name, rolePrompt, criteriaPrompt, intentPrompt
                Docs/           Actor-private documentation
        Logs/                   Session logs
```

- **WorkSource** — the root of the user's codebase (parent of `.wally/`). Controls `gh copilot` working directory and `--add-dir` scope.
- **WorkspaceFolder** — the `.wally/` folder holding config and actor definitions.

### `WallyEnvironment`

Runtime host over a `WallyWorkspace`. Manages workspace lifecycle, actor execution, and session logging.

```csharp
var env = new WallyEnvironment();
env.SetupLocal(@"C:\repos\MyApp");

// Single run
var responses = env.RunActors("Explain this module");

// Or target a specific actor
var response = env.RunActor("Add validation", "Developer");
```

### `WallyConfig`

Loaded from `wally-config.json`:

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `"Actors"` | Actor directory name inside workspace. |
| `LogsFolderName` | `"Logs"` | Session log directory name. |
| `DocsFolderName` | `"Docs"` | Workspace-level documentation directory name. |
| `LogRotationMinutes` | `2` | Minutes per log file. `0` = single file. |
| `DefaultModel` | `"gpt-4.1"` | Model passed to `gh copilot --model`. |
| `Models` | `[...]` | Available model identifiers. |
| `MaxIterations` | `10` | Default iteration cap for `WallyLoop`. |

Config resolution: workspace-local ? shipped template ? hard-coded defaults.

### `SessionLogger`

Structured per-session logger. Writes JSON entries to rotating log files under `.wally/Logs/<session>/`.

- Buffers entries in memory until `Bind()` is called (on workspace load)
- Thread-safe (serialized via lock)
- Log categories: `Command`, `Prompt`, `ProcessedPrompt`, `Response`, `CliError`, `Info`, `Error`

### RBA (Role, AcceptanceCriteria, Intent)

Each actor carries three prompt components:

| Component | JSON key | Description |
|---|---|---|
| **Role** | `rolePrompt` | The persona the AI adopts |
| **AcceptanceCriteria** | `criteriaPrompt` | Success criteria the output must meet |
| **Intent** | `intentPrompt` | The goal the actor pursues |