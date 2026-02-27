# Wally.Core

Domain library — no CLI, no UI. Contains everything needed to host a Wally workspace in any .NET 8 application.

## Key types

### `WallyWorkspace`

Owns the workspace layout on disk. The workspace model is built around two directories:

- **WorkSource** — the root of the user's codebase (e.g. `C:\repos\MyApp`). This is the directory whose files provide context to `gh copilot`.
- **WorkspaceFolder** — the `.wally/` folder inside the WorkSource that holds config and actor definitions.

```
<WorkSource>/                   e.g. C:\repos\MyApp
    .wally/                     WorkspaceFolder
        wally-config.json
        Actors/
            <ActorName>/        one folder per actor
                actor.json      name, rolePrompt, criteriaPrompt, intentPrompt
```

Call `WallyWorkspace.Load(workspaceFolder)` or `LoadFrom(path)`.
Each actor folder produces exactly one `CopilotActor` — no cartesian-product expansion.
Call `ReloadActors()` to re-read actor folders from disk mid-session without a full reload.

### WorkSource

`WallyWorkspace.WorkSource` is the parent of the `.wally/` workspace folder. It controls
the working directory used when launching `gh copilot`, determining which files and
directories Copilot CLI sees for context. `SourcePath` is a convenience alias that
resolves to `WorkSource`.

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

// Or target a specific WorkSource (codebase root) — .wally/ is created inside it.
// If the directory doesn't exist, it is created automatically.
env.SetupLocal(@"C:\repos\MyApp");

// Works with new directories too — both the WorkSource and .wally/ are created
env.SetupLocal(@"C:\repos\NewProject");

// Isolate the workspace in a subfolder (e.g. one level deeper than the exe)
// CLI equivalent: wally setup -w workspace
env.SetupLocal("workspace");

// Run all actors once
var responses = env.RunActors("Explain this module");
// responses: "[Role: Developer]\n<response>", "[Role: Tester]\n<response>"
```

The iterative loop logic lives directly inside `WallyEnvironment`. On each iteration the previous
response is passed back through `Actor.ProcessPrompt` so the actor's full RBA context (Role,
AcceptanceCriteria, Intent) is re-applied before the next `Act` call.
The loop stops early when the actor returns an empty response.

### `WallyConfig`

Loaded from / saved to `wally-config.json`. Defines:

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `"Actors"` | Subfolder inside the workspace that holds actor directories. |
| `DefaultModel` | `"gpt-4.1"` | LLM model passed via `--model` to all actors. Null = Copilot default. |
| `Models` | `[…]` | List of available/allowed model identifiers for this workspace. |
| `MaxIterations` | `10` | Maximum iterations for iterative actor runs. |

### `CopilotActor`

The default actor implementation. Invokes `gh copilot -p` directly using
`ProcessStartInfo.ArgumentList` (no shell, no escaping issues). The process working
directory is set to WorkSource so Copilot sees the target codebase. When
`WallyConfig.DefaultModel` is set, `--model <id>` is added to the invocation.

Stdin is redirected and immediately closed to prevent `gh copilot` from waiting for
interactive input.

#### Per-run model override

Set `Actor.ModelOverride` before calling `Act()` to use a different model for a single run.
The override is automatically cleared after the call completes. Passing `"default"` as the
override explicitly falls back to the configured `DefaultModel`.

```csharp
var actor = env.GetActor("Developer");
actor.ModelOverride = "claude-sonnet-4";   // one-shot override
string response = actor.Act("Explain this module");
// actor.ModelOverride is now null — next call uses DefaultModel again
```

### RBA (Role, AcceptanceCriteria, Intent)

Each actor carries three prompt components:

| Component | JSON key | Description |
|---|---|---|
| **Role** | `rolePrompt` | The persona the AI adopts |
| **AcceptanceCriteria** | `criteriaPrompt` | Success criteria the output must meet |
| **Intent** | `intentPrompt` | The goal or task the actor pursues |