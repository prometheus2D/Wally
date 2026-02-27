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

### `CopilotActor`

Default actor implementation. Invokes `gh copilot -p` directly using `ProcessStartInfo.ArgumentList` (no shell, no escaping). Working directory is set to WorkSource so Copilot sees the target codebase.

```csharp
var actor = env.GetActor("Developer");
actor.ModelOverride = "claude-sonnet-4";   // one-shot override
string response = actor.Act("Explain this module");
// actor.ModelOverride is now null — next call uses DefaultModel
```

### `WallyLoop`

Iterative execution loop with three core pieces:

| Piece | Purpose |
|---|---|
| **StartPrompt** | Prompt for the first iteration |
| **ContinuePrompt** | Prompt for iterations 2+ (falls back to previous result if null) |
| **EndIdentifier** | `Func<string, bool>` — returns `true` when stop word detected. Defaults to `"LOOP COMPLETED"` |

```csharp
var loop = new WallyLoop(
    action:         prompt => actor.Act(prompt),
    startPrompt:    actor.GeneratePrompt("Refactor error handling"),
    continuePrompt: actor.GeneratePrompt("Continue. Respond with LOOP COMPLETED when done."),
    maxIterations:  5
);
loop.Run();
// loop.Results       — all iteration results
// loop.ExecutionCount — how many iterations ran
// loop.StoppedByDeclaration — true if stop word detected, false if max hit
```

### `WallyWorkspace`

Owns the workspace layout on disk:

```
<WorkSource>/                   e.g. C:\repos\MyApp
    .wally/                     WorkspaceFolder
        wally-config.json
        Actors/
            <ActorName>/
                actor.json      name, rolePrompt, criteriaPrompt, intentPrompt
```

- **WorkSource** — the root of the user's codebase (parent of `.wally/`). Controls `gh copilot` working directory.
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
| `DefaultModel` | `"gpt-4.1"` | Model passed to `gh copilot --model`. |
| `Models` | `[...]` | Available model identifiers. |
| `MaxIterations` | `10` | Default iteration cap for `WallyLoop`. |

Config resolution: workspace-local ? shipped template ? hard-coded defaults.

### RBA (Role, AcceptanceCriteria, Intent)

Each actor carries three prompt components:

| Component | JSON key | Description |
|---|---|---|
| **Role** | `rolePrompt` | The persona the AI adopts |
| **AcceptanceCriteria** | `criteriaPrompt` | Success criteria the output must meet |
| **Intent** | `intentPrompt` | The goal the actor pursues |