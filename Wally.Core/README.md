# Wally.Core

Domain library for Wally — the AI Actor Environment. This is the core engine that powers both the CLI and GUI. It contains all business logic, workspace management, actor orchestration, and command dispatch. No UI, no CLI — embed in any .NET 8 application.

**Target:** .NET 8 | **Type:** Class Library | **Dependencies:** `CommandLineParser 2.9.1`

---

## Preferences vs. Config: Separation of Concerns

- **WallyPreferences** (`wally-prefs.json`): User-profile-level, global to all workspaces. Stores recent workspaces, last loaded workspace, and auto-load preference. Used for session and history, not workspace structure.
- **WallyConfig** (`wally-config.json`): Workspace-specific, lives inside each `.wally/` folder. Stores folder names, available/selected models, wrappers, loops, runbooks, and runtime options. Used for configuring the structure and behavior of a specific workspace.

<b>There is no redundancy:</b> Preferences are for user session/history (cross-workspace), config is for workspace structure/options (per workspace).

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Key Types](#key-types)
  - [WallyEnvironment](#wallyenvironment)
  - [Actor](#actor)
  - [LLMWrapper](#llmwrapper)
  - [WallyLoop](#wallyloop)
  - [WallyLoopDefinition](#wallyloopdefinition)
  - [WallyRunbook](#wallyrunbook)
  - [WallyWorkspace](#wallyworkspace)
  - [WallyConfig](#wallyconfig)
  - [WallyPreferences](#wallypreferences)
  - [WallyCommands](#wallycommands)
  - [SessionLogger](#sessionlogger)
- [RBA Framework](#rba-framework)
- [Workspace Layout](#workspace-layout)
- [Embedding Wally.Core in Your App](#embedding-wallycore-in-your-app)
- [Default Workspace Template](#default-workspace-template)
- [File Structure](#file-structure)

---

## Architecture Overview

```
???????????????????????????????????????????????????????????
?                    Your Host App                        ?
?              (Console, WinForms, API, etc.)             ?
???????????????????????????????????????????????????????????
                       ?
              ???????????????????
              ? WallyCommands   ?  ? Shared command dispatcher
              ? (static methods)?    used by CLI, GUI, and runbooks
              ???????????????????
                       ?
              ???????????????????
              ? WallyEnvironment?  ? Runtime orchestration layer
              ?                 ?    (workspace + actors + wrappers)
              ???????????????????
                 ?         ?
     ?????????????  ????????????????
     ? Actor     ?  ? LLMWrapper   ?
     ? (RBA prompt?  ? (JSON-driven ?
     ?  pipeline)?  ?  CLI recipe) ?
     ?????????????  ????????????????
                 ?
     ???????????????????????????????
     ? WallyWorkspace              ?
     ? ??? Actors (loaded from disk)?
     ? ??? Loops (JSON definitions)?
     ? ??? Wrappers (JSON defs)    ?
     ? ??? Runbooks (.wrb files)   ?
     ? ??? Config (wally-config)   ?
     ???????????????????????????????
```

| Concern | Owner |
|---|---|
| RBA personality & prompt enrichment | `Actor` |
| CLI recipe & process spawning | `LLMWrapper` (JSON-driven) |
| Orchestration (actor + wrapper + model) | `WallyEnvironment` |
| Loop iteration & stop conditions | `WallyLoop` / `WallyLoopDefinition` |
| Multi-step command workflows | `WallyRunbook` |
| Command dispatch (CLI, GUI, runbooks) | `WallyCommands` |
| Loading actors/loops/wrappers/runbooks from disk | `WallyHelper` |
| Workspace layout & config | `WallyWorkspace` / `WallyConfig` |
| Session logging | `SessionLogger` |

---

## Key Types

### WallyEnvironment

The runtime orchestration layer. Manages workspace lifecycle, actor execution, wrapper resolution, and session logging. This is the main entry point for any host application.

```csharp
var env = new WallyEnvironment();

// Scaffold a workspace (creates .wally/ if needed, then loads)
env.SetupLocal(@"C:\repos\MyApp");

// Single actor run
var responses = env.RunActor("Review the auth module", "Engineer");

// Direct mode (no actor — prompt sent as-is)
string response = env.ExecutePrompt("What does this codebase do?");

// Run all actors
var allResponses = env.RunActors("Explain this module");

// Execute with model/wrapper override
string result = env.ExecuteActor(actor, "Explain this", modelOverride: "claude-sonnet-4");

// Resolve the active LLM wrapper
LLMWrapper wrapper = env.ResolveWrapper();
LLMWrapper specific = env.ResolveWrapper("AutoCopilot");
```

**Key properties:**

| Property | Description |
|---|---|
| `Workspace` | The loaded `WallyWorkspace` (null when not loaded) |
| `HasWorkspace` | Whether a workspace is loaded |
| `WorkspaceFolder` | Path to the `.wally/` folder |
| `WorkSource` | Path to the codebase root (parent of `.wally/`) |
| `Actors` | Loaded actor list (pass-through to workspace) |
| `Loops` | Loaded loop definitions (pass-through) |
| `Runbooks` | Loaded runbook definitions (pass-through) |
| `Logger` | `SessionLogger` for this environment lifetime |

**Key methods:**

| Method | Description |
|---|---|
| `SetupLocal(path)` | Scaffold + load workspace |
| `LoadWorkspace(path)` | Load an existing workspace |
| `SaveWorkspace()` | Save config and actors to disk |
| `CloseWorkspace()` | Close and unbind the workspace |
| `ReloadActors()` | Re-read actor folders from disk |
| `ExecuteActor(actor, prompt)` | Run actor pipeline ? LLM wrapper |
| `ExecutePrompt(prompt)` | Direct mode — no actor enrichment |
| `RunActor(prompt, actorName)` | Find actor by name and execute |
| `RunActors(prompt)` | Execute all actors |
| `ResolveWrapper(name?)` | Get the active or named LLM wrapper |
| `GetActor(name)` | Find an actor by name (case-insensitive) |
| `GetLoop(name)` | Find a loop by name |
| `GetRunbook(name)` | Find a runbook by name |

---

### Actor

An actor is a personality defined by RBA prompts (Role, AcceptanceCriteria, Intent). Actors own prompt enrichment — they know nothing about LLM wrappers or execution.

```csharp
var actor = new Actor(
    "SecurityAuditor",
    folderPath,
    new Role("SecurityAuditor", "You are a security auditor..."),
    new AcceptanceCriteria("SecurityAuditor", "Find all vulnerabilities..."),
    new Intent("SecurityAuditor", "Produce a security report..."),
    workspace
);

// Generate the full RBA prompt
string prompt = actor.GeneratePrompt("Review the authentication module");

// Process prompt (enriches with RBA context + documentation listings)
string enriched = actor.ProcessPrompt("Review the authentication module");
```

**Prompt generation pipeline:**

1. `Setup()` — Called once before processing (hook for actor-specific initialization)
2. `ProcessPrompt(userPrompt)` — Enriches the prompt with:
   - Actor name and RBA identity (Role, Acceptance Criteria, Intent)
   - Documentation context (files from actor `Docs/` and workspace `Docs/`)
   - The user's prompt

**Documentation context:** Files in `.wally/Docs/` and `.wally/Actors/<Name>/Docs/` are automatically listed in the enriched prompt so the LLM knows they exist and can reference them when relevant.

---

### LLMWrapper

A data-driven CLI wrapper loaded entirely from a JSON definition. Each `.json` file in `Wrappers/` defines one wrapper. No C# subclass needed — the JSON *is* the wrapper.

```json
{
  "Name": "Copilot",
  "Description": "Read-only — runs gh copilot -p",
  "Executable": "gh",
  "ArgumentTemplate": "copilot {model} {sourcePath} --yolo -s -p {prompt}",
  "ModelArgFormat": "--model {model}",
  "SourcePathArgFormat": "--add-dir {sourcePath}",
  "UseSourcePathAsWorkingDirectory": true,
  "CanMakeChanges": false
}
```

**Placeholders in `ArgumentTemplate`:**

| Placeholder | Resolved To |
|---|---|
| `{prompt}` | The fully enriched prompt text |
| `{model}` | Model identifier (omitted when null) |
| `{sourcePath}` | Codebase root path (omitted when null) |

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Logical name (e.g. `"Copilot"`) |
| `Description` | `string` | Human-readable description |
| `Executable` | `string` | CLI executable (e.g. `"gh"`) |
| `ArgumentTemplate` | `string` | Template with placeholders |
| `ModelArgFormat` | `string` | Format for model argument segment |
| `SourcePathArgFormat` | `string` | Format for source path segment |
| `UseSourcePathAsWorkingDirectory` | `bool` | Set working directory to source path |
| `CanMakeChanges` | `bool` | Whether this wrapper can edit files |

To add a new LLM backend, drop a `.json` file in `Wrappers/` — zero code changes.

---

### WallyLoop

Iterative execution loop. Each LLM call is stateless — the loop carries context forward by embedding the previous response in the next prompt.

```csharp
var loop = new WallyLoop(
    action:         prompt => env.ExecuteActor(actor, prompt),
    startPrompt:    "Refactor error handling",
    continuePrompt: prev => $"Continue from:\n{prev}\nRespond with [LOOP COMPLETED] when done.",
    maxIterations:  5
);
loop.Run();

// Results
loop.Results         // List<string> — all iteration results
loop.ExecutionCount  // int — how many iterations ran
loop.StopReason      // LoopStopReason — Completed, Error, or MaxIterations
loop.LastResult      // string? — the most recent iteration result
```

**Stop keywords detected in the response:**
- `[LOOP COMPLETED]` — task finished successfully
- `[LOOP ERROR]` — actor detected an error

**`LoopStopReason` enum:** `MaxIterations`, `Completed`, `Error`

---

### WallyLoopDefinition

A serializable loop definition loaded from a JSON file in `Loops/`. Defines the actor, prompts, stop keywords, and iteration limit as data — no code needed.

```json
{
  "Name": "CodeReview",
  "Description": "Iterative code review with the Engineer actor",
  "ActorName": "Engineer",
  "StartPrompt": "{userPrompt}\n\nPerform a thorough code review...",
  "ContinuePromptTemplate": "Previous pass:\n---\n{previousResult}\n---\n...",
  "CompletedKeyword": "[LOOP COMPLETED]",
  "ErrorKeyword": "[LOOP ERROR]",
  "MaxIterations": 5
}
```

**Placeholders in prompts:** `{userPrompt}`, `{previousResult}`, `{completedKeyword}`, `{errorKeyword}`

**Factory methods:**

```csharp
var loop = WallyLoopDefinition.LoadFromFile("path/to/CodeReview.json");
var allLoops = WallyLoopDefinition.LoadFromFolder("path/to/Loops/");
loop.SaveToFile("path/to/MyLoop.json");
```

---

### WallyRunbook

A runbook loaded from a `.wrb` (Wally Runbook) file. Runbooks are plain-text files with one Wally command per line.

```
# First comment line becomes the description.
setup --verify
run "{userPrompt}" -a Stakeholder
run "{userPrompt}" -a Engineer -l CodeReview
```

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Derived from filename (e.g. `hello-world`) |
| `Description` | `string` | First comment line |
| `FilePath` | `string` | Absolute path to the `.wrb` file |
| `Commands` | `List<string>` | Parsed non-comment, non-blank lines |

**Placeholders** resolved at runtime: `{userPrompt}`, `{workSourcePath}`, `{workspaceFolder}`

**Nesting:** Runbooks can call other runbooks (max depth: 10). Execution stops on first error.

---

### WallyWorkspace

Owns the workspace layout on disk. Loads actors, loops, wrappers, and runbooks from the filesystem.

```
<WorkSource>/                   e.g. C:\repos\MyApp
    .wally/                     WorkspaceFolder
        wally-config.json
        Docs/                   Workspace-level documentation
        Templates/              Document templates
        Actors/
            <ActorName>/
                actor.json      name, rolePrompt, criteriaPrompt, intentPrompt
                Docs/           Actor-private documentation
        Loops/                  Loop definitions (JSON)
        Wrappers/               LLM wrapper definitions (JSON)
        Runbooks/               Runbook files (.wrb)
        Logs/                   Session logs
```

**Key properties:**

| Property | Description |
|---|---|
| `WorkSource` | Root of the user's codebase (parent of `.wally/`) |
| `WorkspaceFolder` | The `.wally/` folder path |
| `SourcePath` | Same as `WorkSource` — context scope for the LLM |
| `Actors` | Loaded actor list |
| `Loops` | Loaded loop definitions |
| `LlmWrappers` | Loaded wrapper definitions |
| `Runbook