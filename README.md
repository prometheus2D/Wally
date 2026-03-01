# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a `.wally/` workspace inside any codebase, loads actors defined by JSON files, wraps every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards it to `gh copilot`.

---

## Prerequisites

| Tool | Install |
|---|---|
| .NET 8 SDK | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| GitHub CLI | [cli.github.com](https://cli.github.com) |
| Copilot extension | `gh extension install github/gh-copilot` |
| Auth | `gh auth login` (Copilot access required) |

## Build & Run

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

## Quick Start

```sh
# Point Wally at your codebase root — .wally/ is created inside it
wally setup C:\repos\MyApp

# Run a single actor
wally run Developer "Add input validation to the login form"

# Run with a specific model
wally run Developer "Refactor the data layer" -m claude-sonnet-4

# Run an actor in an iterative loop
wally run-loop Developer "Build a REST API for user management" -n 5
```

That's it. Wally wraps your prompt with the actor's Role, AcceptanceCriteria, and Intent, then passes the structured prompt to `gh copilot`.

---

## Commands

### No workspace required

| Command | Description |
|---|---|
| `setup [<path>] [-w <path>]` | Scaffold or load a workspace. `<path>` is your codebase root. `.wally/` is created inside it. Defaults to the exe directory. |
| `create <path>` | Scaffold a new `.wally/` workspace inside `<path>` and load it. |
| `load <path>` | Load an existing `.wally/` workspace folder. |
| `info` | Print workspace paths, model config, loaded actors, and session info. |
| `help` | Print command reference. |

### Workspace required

| Command | Description |
|---|---|
| `save <path>` | Persist config and all actor files to disk. |
| `list` | List all actors and their RBA prompts. |
| `reload-actors` | Re-read actor folders from disk. |
| `run <actor> "<prompt>" [-m <model>]` | Run a single actor. `-m` overrides the model for this run. |
| `run-loop <actor> "<prompt>" [-m <model>] [-n <max>]` | Run an actor in an iterative loop. Ends on `[LOOP COMPLETED]`, `[LOOP ERROR]`, or `-n` max iterations. |

### Interactive REPL

Run `wally` with no arguments to enter interactive mode:

```sh
wally
wally> setup C:\repos\MyApp
wally> run Developer "Explain the main entry point"
wally> run-loop Tester "Find and document all edge cases"
wally> info
wally> exit
```

---

## Workspace Layout

Running `wally setup <path>` creates a `.wally/` folder inside your codebase:

```
<WorkSource>/                      Your codebase root (e.g. C:\repos\MyApp)
  .wally/                          Workspace folder
    wally-config.json              Configuration
    instructions.md                Quick-reference (this content, shipped with every workspace)
    Docs/                          Workspace-level documentation (shared by all actors)
      style-guide.md               .md, .txt, .rst, .adoc files
    Actors/
      Developer/
        actor.json                 Actor definition (RBA prompts)
        Docs/                      Actor-private documentation
          architecture.md
      Tester/
        actor.json
        Docs/
          test-plan.md
    Logs/                          Session logs (auto-created on first run)
      <timestamp_guid>/            One folder per session
        <timestamp>.txt            Rotated log files
```

---

## Actors — `actor.json`

Each actor lives in its own subfolder under `.wally/Actors/` with an `actor.json`:

```json
{
  "name": "Developer",
  "rolePrompt": "Act as an expert software developer, writing clean and efficient code.",
  "criteriaPrompt": "Code must compile without errors, follow best practices, and pass unit tests.",
  "intentPrompt": "Implement the requested feature with proper error handling.",
  "docsFolderName": "Docs"
}
```

| Field | RBA Component | Purpose |
|---|---|---|
| `rolePrompt` | Role | The persona the AI adopts |
| `criteriaPrompt` | AcceptanceCriteria | Success criteria the output must meet |
| `intentPrompt` | Intent | The goal the actor pursues |
| `docsFolderName` | — | Subfolder name for actor-private docs (default: `Docs`) |

### How prompts are built

When you run an actor, Wally wraps your prompt inside the actor's RBA context:

```
# Actor: Developer
## Role
Act as an expert software developer...
## Acceptance Criteria
Code must compile without errors...
## Intent
Implement the requested feature...

## Prompt
<your prompt here>
```

This enriched prompt is passed to `gh copilot -p` along with `--add-dir` pointing to your WorkSource directory.

### Creating custom actors

1. Create a folder under `.wally/Actors/`:
   ```
   .wally/Actors/Reviewer/
   ```

2. Add an `actor.json`:
   ```json
   {
     "name": "Reviewer",
     "rolePrompt": "Act as a senior code reviewer focused on security and performance.",
     "criteriaPrompt": "Identify all security vulnerabilities and performance bottlenecks.",
     "intentPrompt": "Review the code and provide actionable feedback.",
     "docsFolderName": "Docs"
   }
   ```

3. Optionally add a `Docs/` subfolder with reference material.

4. Run it:
   ```sh
   wally reload-actors
   wally run Reviewer "Review the authentication module"
   ```

---

## Configuration — `wally-config.json`

```json
{
  "ActorsFolderName": "Actors",
  "LogsFolderName": "Logs",
  "DocsFolderName": "Docs",
  "LogRotationMinutes": 2,
  "DefaultModel": "gpt-4.1",
  "Models": ["gpt-4.1", "claude-sonnet-4", "gpt-5.2"],
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `"Actors"` | Subfolder holding actor directories. |
| `LogsFolderName` | `"Logs"` | Subfolder for session logs. |
| `DocsFolderName` | `"Docs"` | Subfolder for workspace-level documentation. |
| `LogRotationMinutes` | `2` | Minutes per log file. `0` disables rotation (single `session.txt`). |
| `DefaultModel` | `"gpt-4.1"` | Model passed to `gh copilot --model`. Null = Copilot default. |
| `Models` | `[...]` | Reference list of available model identifiers. |
| `MaxIterations` | `10` | Default iteration cap for `run-loop`. |

---

## Documentation

Files in `.wally/Docs/` and `.wally/Actors/<Name>/Docs/` are **not** injected into prompts. They live on disk inside the WorkSource tree, and `gh copilot` can read them natively via `--add-dir`.

To point Copilot at a specific file, reference it by path in your prompt:

```sh
wally run Developer "Refactor the API. Refer to .wally/Docs/style-guide.md for conventions."
```

| Tier | Location | Scope |
|---|---|---|
| **Workspace-level** | `.wally/Docs/` | Shared across all actors |
| **Actor-level** | `.wally/Actors/<Name>/Docs/` | Private to that actor |

Supported formats: `.md`, `.txt`, `.text`, `.rst`, `.adoc`

---

## How It Works

### Single run (`run`)

```
User prompt
  ? Actor.ProcessPrompt()
      Wraps prompt with Role, AcceptanceCriteria, Intent
  ? gh copilot --model <model> --add-dir <WorkSource> -s -p "<structured prompt>"
      WorkingDirectory = your codebase root
  ? Response printed to console
```

### Iterative loop (`run-loop`)

```
User prompt
  ? Iteration 1: actor.Act(startPrompt)
  ? Iteration 2+: actor.Act(continuePrompt)
      continuePrompt embeds the previous response for context
      (each gh copilot call is stateless)
  ? Loop ends on [LOOP COMPLETED], [LOOP ERROR], or max iterations
```

The `run-loop` command runs an actor repeatedly, feeding each response back as context for the next iteration. Each `gh copilot -p` call is completely stateless — the loop carries context forward explicitly in the prompt.

The loop stops when:
- The actor responds with `[LOOP COMPLETED]` — task finished.
- The actor responds with `[LOOP ERROR]` — something went wrong.
- Max iterations is reached (default: 10, override with `-n`).

---

## Model Selection

Set `DefaultModel` in `wally-config.json`, or override per run with `-m`:

```sh
wally run Developer "Explain this module" -m claude-sonnet-4
wally run Developer "Explain this module" -m default   # uses config DefaultModel
```

Run `gh copilot -- --help` to see available `--model` choices for your account.

---

## Logging

Every command, prompt, and response is logged per session under `.wally/Logs/`. Each session gets a timestamped folder (e.g. `2025-07-13_143022_a1b2c3d4/`).

Log files rotate every `LogRotationMinutes` (default: 2 minutes). Set to `0` in config to write everything to a single `session.txt`.

What's logged:

| Category | When |
|---|---|
| `Command` | Every CLI command invocation |
| `Prompt` | Raw user prompt before RBA wrapping |
| `ProcessedPrompt` | Full enriched prompt sent to `gh copilot` |
| `Response` | Actor response text + elapsed time |
| `CliError` | Non-zero exit codes or stderr from `gh copilot` |
| `Info` | Session start, loop completion summaries |
| `Error` | Missing workspace, actor not found, etc. |

Before a workspace is loaded, entries buffer in memory and flush to disk on first `setup`/`load`/`create`.

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain library — `Actor`, `WallyLoop`, `WallyWorkspace`, `WallyEnvironment`, `WallyConfig`, RBA types, session logging. |
| `Wally.Console` | CLI entry point — verb-based commands, interactive REPL, ships default workspace template. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `'gh' is not recognized` | Install [GitHub CLI](https://cli.github.com) and add to PATH. |
| `gh copilot: command not found` | `gh extension install github/gh-copilot` |
| `HTTP 401` | `gh auth login` — ensure Copilot access. |
| Empty responses | `wally info` — verify WorkSource points to a directory with code. |
| Model not available | `gh copilot -- --help` — check `--model` choices, update config. |
| Actor not found | `wally list` to see loaded actors. `wally reload-actors` after adding new ones. |

---

## License

MIT
