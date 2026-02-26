# Wally.Core

Domain library — no CLI, no UI. Contains everything needed to host a Wally workspace in any .NET 8 application.

## Key types

### `WallyWorkspace`
Owns a three-folder layout on disk:

```
<ParentFolder>/
    <ProjectFolderName>/    ? codebase  (default: "Project")
    <WorkspaceFolderName>/  ? config    (default: ".wally")
        wally-config.json
```

Call `WallyWorkspace.Load(parentFolder)` or `LoadFrom(path)` (accepts any of the three folders).
Manages `FolderReferences` and `FileReferences` — explicit opt-in paths sent to actors.

### `WallyEnvironment`
Thin runtime host over a `WallyWorkspace`. Exposes workspace lifecycle (`LoadWorkspace`, `CreateWorkspace`, `SetupLocal`), reference management, and actor execution (`RunActors`, `RunActor`, `RunActorsIterative`).

```csharp
var env = new WallyEnvironment();
env.SetupLocal();                                    // scaffolds if needed
env.LoadDefaultActors("default-agents.json");
env.AddFolderReference(@".\Project\src");
var responses = env.RunActors("Explain this module");
```

### `WallyConfig`
Loaded from / saved to `wally-config.json`. Defines:
- `WorkspaceFolderName` — workspace subfolder name (default `.wally`)
- `ProjectFolderName` — project subfolder name (default `Project`)
- `MaxIterations` — cap for iterative runs
- `Roles`, `AcceptanceCriterias`, `Intents` — RBA definitions

### `Actor` (abstract)
Pipeline: `Setup() ? ProcessPrompt() ? ShouldMakeChanges() ? ApplyCodeChanges() | Respond()`

`ProcessPrompt` appends workspace context (project folder, folder refs, file refs) to every prompt before dispatch. Concrete implementations receive a `WallyWorkspace?` at construction.

| Actor | Behaviour |
|---|---|
| `WallyActor` | Forwards enriched prompt to `gh copilot explain`. |
| `CopilotActor` | Forwards enriched prompt to `gh copilot suggest`. Never applies changes. |

### `WallyHelper`
Static utilities: `GetDefaultParentFolder()` (exe directory), `CreateDefaultWorkspace(parentFolder)`, `ResolveConfig()`.

### RBA types (`Wally.Core.RBA`)
`Role`, `AcceptanceCriteria`, `Intent` — each has `Name`, `Prompt`, `Tier`.
Actors are the cartesian product of all three lists.