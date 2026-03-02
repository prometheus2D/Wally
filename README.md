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
wally run Engineer "Add input validation to the login form"

# Run with a specific model
wally run BusinessAnalyst "Write requirements for the search feature" -m claude-sonnet-4

# Run an actor in an iterative loop
wally run-loop Engineer "Build a REST API for user management" -n 5
```

That's it. Wally wraps your prompt with the actor's Role, AcceptanceCriteria, and Intent, then passes the structured prompt to `gh copilot`.

---

## Default Actors

The shipped workspace template includes three actors. These are starting points —
add, remove, or customise actors by editing the `Actors/` folder:

| Actor | Perspective | Produces |
|---|---|---|
| **Stakeholder** | Business — defines needs, priorities, success criteria | Business context, priorities, feedback |
| **BusinessAnalyst** | Bridge — translates needs into requirements, manages project | Requirements, Execution Plans |
| **Engineer** | Technical — designs, builds, tests, documents | Proposals, Implementation Plans, Architecture docs, Bug Reports, Test Plans |

Each actor generates documentation from its own perspective using the document templates in `Templates/`.

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
wally> run Engineer "Explain the main entry point"
wally> run Stakeholder "Define what the dashboard must achieve"
wally> run BusinessAnalyst "Write requirements for the search feature"
wally> run-loop Engineer "Implement the search API"
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
    Docs/                          Shared documentation (all actors)
        README.md
    Templates/                     Document templates used by actors
        RequirementsTemplate.md
        ProposalTemplate.md
        ImplementationPlanTemplate.md
        ExecutionPlanTemplate.md
        ArchitectureTemplate.md
        BugTemplate.md
        TestPlanTemplate.md
    Actors/
      Stakeholder/
        actor.json                 Actor definition (RBA prompts)
        Docs/                      Actor-specific documentation
      BusinessAnalyst/
        actor.json
        Docs/
      Engineer/
        actor.json
        Docs/
    Logs/                          Session logs (auto-created on first run)
      <timestamp_guid>/            One folder per session
        <timestamp>.txt            Rotated log files
```

---

## Document Templates

Templates in `Templates/` define document structures. Actors reference them by
name in their prompts — edit or add templates to fit your workflow:

| Template | Purpose |
|---|---|
| `RequirementsTemplate.md` | Define what the system must do (current state, future state, acceptance criteria) |
| `ExecutionPlanTemplate.md` | Coordinate delivery across implementation plans |
| `ProposalTemplate.md` | Introduce new ideas with phases, impact, and risks |
| `ImplementationPlanTemplate.md` | Break proposals into concrete, executable steps |
| `ArchitectureTemplate.md` | Capture system design decisions and patterns |
| `BugTemplate.md` | Track defects with symptoms, investigation, and resolution |
| `TestPlanTemplate.md` | Define how requirements will be verified |

---

## Actors — `actor.json`

Each actor lives in its own subfolder under `.wally/Actors/` with an `actor.json`:

```json
{
  "name": "Engineer",
  "rolePrompt": "Act as a senior software engineer responsible for all technical work...",
  "criteriaPrompt": "Output must be technically precise, trace back to a requirement...",
  "intentPrompt": "Design and build the system to meet requirements...",
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
# Actor: Engineer
## Role
Act as a senior software engineer...
## Acceptance Criteria
Output must be technically precise...
## Intent
Design and build the system to meet requirements...

## Prompt
<your prompt here>
```

This enriched prompt is passed to `gh copilot -p` along with `--add-dir` pointing to your WorkSource directory.

### Creating custom actors

1. Create a folder under `.wally/Actors/`:
   ```
   .wally/Actors/SecurityReviewer/
   ```

2. Add an `actor.json`:
   ```json
   {
     "name": "SecurityReviewer",
     "rolePrompt": "Act as a security engineer focused on threat modeling and vulnerability analysis.",
     "criteriaPrompt": "Identify all security vulnerabilities, rank by severity, and provide remediation steps.",
     "intentPrompt": "Review the system for security risks and produce actionable findings.",
     "docsFolderName": "Docs"
   }
   ```

3. Optionally add a `Docs/` subfolder with reference material.

4. Run it:
   ```sh
   wally reload-actors
   wally run SecurityReviewer "Review the authentication module"
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
wally run Engineer "Refactor the API. Refer to .wally/Templates/ArchitectureTemplate.md for structure."
```

| Tier | Location | Scope |
|---|---|---|
| **Workspace-level** | `.wally/Docs/` | Shared across all actors |
| **Actor-level** | `.wally/Actors/<Name>/Docs/` | Private to that actor |
| **Templates** | `.wally/Templates/` | Document structure definitions |

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
wally run Engineer "Explain this module" -m claude-sonnet-4
wally run Engineer "Explain this module" -m default   # uses config DefaultModel
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
