# Wally.Core

Domain library — no CLI, no UI. Contains everything needed to host a Wally workspace in any .NET 8 application.

## Key types

### `WallyWorkspace`

Owns the workspace layout on disk:

```
<ParentFolder>/
    <ProjectFolderName>/        ? codebase  (default: "Project")
    <WorkspaceFolderName>/      ? Wally     (default: ".wally")
        wally-config.json
        Agents/
            <AgentName>/        ? one folder per agent
                role.txt        ? Role prompt  (optional header: # Tier: task)
                criteria.txt    ? AcceptanceCriteria prompt
                intent.txt      ? Intent prompt
```

Call `WallyWorkspace.Load(parentFolder)` or `LoadFrom(path)` (accepts the parent, workspace, or project folder).
Each agent folder produces exactly one `WallyActor` — no cartesian-product expansion.
Call `ReloadAgents()` to re-read agent folders from disk mid-session without a full reload.

### `WallyEnvironment`

Thin runtime host over a `WallyWorkspace`. Exposes workspace lifecycle, reference management, and actor execution.

```csharp
var env = new WallyEnvironment();

// Scaffold or load in the exe directory (default)
env.SetupLocal();

// Or target a specific folder
env.SetupLocal(@"C:\repos\MyApp");

env.AddFolderReference(@".\Project\src");

// Run all agents once
var responses = env.RunActors("Explain this module");
// responses keyed by agent name: "Developer: <response>", "Tester: <response>"

// Run all agents iteratively — combined responses feed back each iteration
var final = env.RunActorsIterative("Improve error handling", (i, responses) =>
    Console.WriteLine($"Iteration {i}: {string.Join(", ", responses)}"));

// Run a single named agent iteratively
string result = env.RunActorIterative("Refactor to clean architecture", "Developer",
    maxIterationsOverride: 5,
    onIteration: (i, response) => Console.WriteLine($"[{i}] {response}"));
```

The iterative loop logic lives directly inside `WallyEnvironment`. On each iteration the previous
response is passed back through `Actor.ProcessPrompt` so the agent's full RBA context (Role,
AcceptanceCriteria, Intent, file/folder references) is re-applied before the next `Act` call.
The loop stops early when the actor returns an empty response.

### `WallyConfig`

Loaded from / saved to `wally-config.json`. Defines:

| Property | Default | Description |
|---|---|---|
| `WorkspaceFolderName` | `.wally` | Workspace subfolder name |
| `ProjectFolderName` | `Project` | Project subfolder name |
| `AgentsFolderName` | `Agents` | Agents subfolder name inside workspace |
| `MaxIterations` | `10` | Default cap for `RunActorsIterative` and `RunActorIterative` |

RBA definitions are **not** stored in JSON — they live entirely in agent folders on disk.

### `AgentDefinition`

Loaded from one agent folder. Holds:
- `Name` — the folder name
- `FolderPath` — absolute path to the agent folder
- `Role`, `Criteria`, `Intent` — each loaded from its `.txt` file, with optional `Tier` parsed from the `# Tier:` header

### `Actor` (abstract)

Pipeline: `Setup() ? ProcessPrompt() ? ShouldMakeChanges() ? ApplyCodeChanges() | Respond()`

`ProcessPrompt` builds a fully-structured Markdown prompt:

```
# Agent: <name>
## Role
<role prompt>
## Acceptance Criteria
<criteria prompt>
## Intent
<intent prompt>

## Prompt
<user's prompt>

[Project Folder: ...]
[Folder References] ...
[File References] ...
```

Subclasses receive this complete prompt in `Respond()` — no duplication needed.

| Actor | Behaviour |
|---|---|
| `WallyActor` | Forwards structured prompt to `gh copilot explain`. |
| `CopilotActor` | Forwards structured prompt to `gh copilot suggest`. Never applies changes. |

### `WallyHelper`

Static utilities:

| Method | Description |
|---|---|
| `GetDefaultParentFolder()` | Returns the exe directory. |
| `CreateDefaultWorkspace(path)` | Scaffolds workspace + copies default agent tree. |
| `LoadAgentDefinitions(folder, config)` | Reads all agent subfolders, returns `AgentDefinition` list. |
| `SaveAgentDefinition(folder, config, agent)` | Writes one agent's prompt files back to disk. |
| `ResolveConfig()` | Loads `wally-config.json` from the default workspace, or returns defaults. |
| `CopyDirectory(src, dest)` | Recursive directory copy (overwrite). |