# Wally.Core

Domain library for Wally. No CLI, no UI — embed in any .NET 8 application.

## Key Types

### `Actor`

An actor is a personality defined by RBA prompts (Role, AcceptanceCriteria, Intent). Actors own prompt enrichment — they know nothing about LLM wrappers or execution.

**Prompt generation** — `GeneratePrompt(userPrompt)` wraps user input inside the actor's RBA context:

```csharp
string prompt = actor.GeneratePrompt("Write requirements for the search feature");
// Output:
// # Actor: BusinessAnalyst
// ## Role
// Act as a Business Analyst and Project Manager...
// ## Acceptance Criteria
// Output must trace every requirement back to a stakeholder need...
// ## Intent
// Translate stakeholder needs into clear requirements...
//
// ## Prompt
// Write requirements for the search feature
```

**Pipeline** — `ProcessPrompt(prompt)` enriches the raw prompt with RBA context and documentation file listings. `Setup()` runs any pre-processing. Neither method touches the LLM — execution is handled by `WallyEnvironment`.

Documentation files in `.wally/Docs/` and `.wally/Actors/<Name>/Docs/` are listed in the enriched prompt so the LLM knows they exist. The files themselves are accessible to the LLM wrapper via its own mechanisms (e.g. `--add-dir` for Copilot).

### `LlmWrapper`

A data-driven CLI wrapper loaded entirely from a JSON definition. Each `.json` file in the workspace's `Wrappers/` folder defines one wrapper: the executable to run, the argument template, display metadata, and behavioural flags. No C# subclass is needed — the JSON *is* the wrapper.

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

Placeholders in `ArgumentTemplate`: `{prompt}`, `{model}`, `{sourcePath}`. When a value is null/empty, its placeholder (and the corresponding format segment) is omitted from the command line.

To add a new LLM backend, drop a `.json` file in `Wrappers/` — zero code changes.

### `WallyLoop`

Iterative execution loop. Each LLM call is stateless — the loop carries context forward explicitly by embedding the previous response in the next prompt.

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
    action:         prompt => env.ExecuteActor(actor, prompt),
    startPrompt:    "Refactor error handling",
    continuePrompt: prev => $"Continue from:\n{prev}\nRespond with [LOOP COMPLETED] when done.",
    maxIterations:  5
);
loop.Run();
// loop.Results         — all iteration results
// loop.ExecutionCount  — how many iterations ran
// loop.StopReason      — Completed, Error, or MaxIterations
```

### `WallyLoopDefinition`

A serializable definition for a `WallyLoop`, loaded from a JSON file in `Loops/`. Defines the actor, prompts, stop keywords, and iteration limit as data — no code needed.

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

Placeholders in prompts: `{userPrompt}`, `{previousResult}`, `{completedKeyword}`, `{errorKeyword}`.

### `WallyWorkspace`

Owns the workspace layout on disk:

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
        Logs/                   Session logs
```

- **WorkSource** — the root of the user's codebase (parent of `.wally/`). Controls the LLM wrapper's working directory and context scope.
- **WorkspaceFolder** — the `.wally/` folder holding config, actors, loops, wrappers, and docs.

### `WallyEnvironment`

Runtime host over a `WallyWorkspace`. Manages workspace lifecycle, actor execution, wrapper resolution, and session logging. This is the orchestration layer that connects actors (RBA) with wrappers (LLM execution).

```csharp
var env = new WallyEnvironment();
env.SetupLocal(@"C:\repos\MyApp");   // scaffolds .wally/ if needed, then loads

// Single actor run
var responses = env.RunActor("Review the auth module", "Engineer");

// Run all actors
var allResponses = env.RunActors("Explain this module");

// Execute with model override
string response = env.ExecuteActor(actor, "Explain this", modelOverride: "claude-sonnet-4");

// Resolve the active LLM wrapper
LlmWrapper wrapper = env.ResolveWrapper();
```

### `WallyConfig`

Loaded from `wally-config.json`:

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `"Actors"` | Actor directory name inside workspace. |
| `LogsFolderName` | `"Logs"` | Session log directory name. |
| `DocsFolderName` | `"Docs"` | Workspace-level documentation directory name. |
| `TemplatesFolderName` | `"Templates"` | Document templates directory name. |
| `LoopsFolderName` | `"Loops"` | Loop definition directory name. |
| `WrappersFolderName` | `"Wrappers"` | LLM wrapper definition directory name. |
| `LogRotationMinutes` | `2` | Minutes per log file. `0` = single file. |
| `DefaultModel` | `"gpt-4.1"` | Model identifier passed to the LLM wrapper. |
| `DefaultModels` | `[...]` | Curated list of available model identifiers. |
| `DefaultWrappers` | `["Copilot", "AutoCopilot"]` | Curated list of available wrapper names. |
| `DefaultLoops` | `["SingleRun", ...]` | Curated list of available loop names. |
| `MaxIterations` | `10` | Default iteration cap for `WallyLoop`. |
| `DefaultWrapper` | `"Copilot"` | Name of the LLM wrapper to use (matches `Wrappers/*.json`). |

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

## Architecture

| Concern | Owner |
|---|---|
| RBA personality & prompt enrichment | `Actor` |
| CLI recipe & process spawning | `LlmWrapper` (JSON-driven) |
| Orchestration (actor + wrapper + model) | `WallyEnvironment` |
| Loop iteration & stop conditions | `WallyLoop` / `WallyLoopDefinition` |
| Loading actors/loops/wrappers from disk | `WallyHelper` |
| Workspace layout & config | `WallyWorkspace` / `WallyConfig` |