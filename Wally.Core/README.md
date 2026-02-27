# Wally.Core

Domain library — no CLI, no UI. Contains everything needed to host a Wally workspace in any .NET 8 application.

## Key types

### `WallyWorkspace`

Owns the workspace layout on disk:

```
<WorkspaceFolder>/              e.g. ".wally/"
    wally-config.json
    Actors/
        <ActorName>/            one folder per actor
            actor.json          name, rolePrompt, criteriaPrompt, intentPrompt
```

Call `WallyWorkspace.Load(workspaceFolder)` or `LoadFrom(path)`.
Each actor folder produces exactly one `CopilotActor` — no cartesian-product expansion.
Call `ReloadActors()` to re-read actor folders from disk mid-session without a full reload.

### Default workspace template

The application ships a `Default/` folder alongside the executable that contains the
canonical workspace template (`wally-config.json` + `Actors/`).
When scaffolding a new workspace the template is copied verbatim (no-overwrite) so
every new workspace starts with the same files.

Config resolution follows a three-tier fallback:
1. **Workspace-local** (`<workspaceFolder>/wally-config.json`)
2. **Template** (`<exeDir>/Default/wally-config.json`)
3. **Hard-coded** (`new WallyConfig()`)

### `WallyEnvironment`

Thin runtime host over a `WallyWorkspace`. Exposes workspace lifecycle and actor execution.

```csharp
var env = new WallyEnvironment();

// Scaffold or load in the exe directory (default)
env.SetupLocal();

// Or target a specific folder
env.SetupLocal(@"C:\repos\MyApp\.wally");

// Run all actors once
var responses = env.RunActors("Explain this module");
// responses: "Developer: <response>", "Tester: <response>"

// Run all actors iteratively — combined responses feed back each iteration
var final = env.RunActorsIterative("Improve error handling", (i, responses) =>
    Console.WriteLine($"Iteration {i}: {string.Join(", ", responses)}"));

// Run a single named actor iteratively
string result = env.RunActorIterative("Refactor to clean architecture", "Developer",
    maxIterationsOverride: 5,
    onIteration: (i, response) => Console.WriteLine($"[{i}] {response}"));
```

The iterative loop logic lives directly inside `WallyEnvironment`. On each iteration the previous
response is passed back through `Actor.ProcessPrompt` so the actor's full RBA context (Role,
AcceptanceCriteria, Intent) is re-applied before the next `Act` call.
The loop stops early when the actor returns an empty response.

> **Note:** File context is handled automatically by Copilot CLI based on the working directory.

### `WallyConfig`

Loaded from / saved to `wally-config.json`. Defines:

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `Actors` | Subfolder inside the workspace that holds actor directories |
| `MaxIterations` | `10` | Maximum iterations for iterative actor runs |

### RBA (Role, AcceptanceCriteria, Intent)

Each actor carries three prompt components:

| Component | JSON key | Description |
|---|---|---|
| **Role** | `rolePrompt` | The persona the AI adopts |
| **AcceptanceCriteria** | `criteriaPrompt` | Success criteria the output must meet |
| **Intent** | `intentPrompt` | The goal or task the actor pursues |