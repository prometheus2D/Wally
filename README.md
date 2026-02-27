# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a `.wally/` workspace inside any codebase, loads actors defined by JSON files, wraps every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards it to `gh copilot`.

---

## Quick Start

### Prerequisites

| Tool | Install |
|---|---|
| .NET 8 SDK | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| GitHub CLI | [cli.github.com](https://cli.github.com) |
| Copilot extension | `gh extension install github/gh-copilot` |
| Auth | `gh auth login` (Copilot access required) |

### Build & Run

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

### First Setup

```sh
# Point Wally at your codebase root. .wally/ is created inside it.
wally setup C:\repos\MyApp

# Run a single actor
wally run Developer "Add input validation to the login form"

# Run an actor in a loop (iterates until "LOOP COMPLETED" or max iterations)
wally run-loop Developer "Refactor error handling across all services"
```

That's it. Wally wraps your prompt with the actor's Role, AcceptanceCriteria, and Intent, then passes the structured prompt to `gh copilot`.

---

## Commands

### Workspace

| Command | Description |
|---|---|
| `setup [<path>]` | Scaffold `.wally/` inside `<path>` (your codebase root). Created if it doesn't exist. Defaults to the exe directory. |
| `create <path>` | Scaffold a new `.wally/` workspace inside `<path>` and load it. |
| `load <path>` | Load an existing `.wally/` workspace folder. |
| `save <path>` | Persist config and all actor files to disk. |
| `info` | Print workspace paths, model config, loaded actors. |

### Actors

| Command | Description |
|---|---|
| `list` | List all actors and their prompts. |
| `reload-actors` | Re-read actor folders from disk. |

### Running

| Command | Description |
|---|---|
| `run <actor> "<prompt>" [-m <model>]` | Run a single actor. `-m` overrides the model for this run. |
| `run-loop <actor> "<prompt>" [-m <model>] [-n <max>]` | Run an actor in an iterative loop. Ends when the response contains `LOOP COMPLETED` or `-n` max iterations is reached. |
| `help` | Print command reference. |

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

## How It Works

### Single Run (`run`)

```
User prompt
  ? Actor.ProcessPrompt()
      Wraps prompt with Role, AcceptanceCriteria, Intent
  ? gh copilot --model <model> -p "<structured prompt>"
      WorkingDirectory = your codebase root
  ? Response printed to console
```

### Loop Run (`run-loop`)

```
User prompt
  ? Actor.GeneratePrompt(userPrompt)     ? start prompt (RBA + user input)
  ? WallyLoop iterates:
      Iteration 1: actor.Act(startPrompt)
      Iteration 2+: actor.Act(continuePrompt)
      Each result checked for "LOOP COMPLETED" stop word
  ? Loop ends on stop word detection or max iterations
```

The actor generates both the start and continue prompts using `GeneratePrompt()`, which wraps the user's raw input inside the actor's full RBA context. The `WallyLoop` class handles iteration, stop-word detection, and result collection.

---

## Workspace Layout

```
<YourCodebase>/                    e.g. C:\repos\MyApp
    .wally/                        Workspace folder
        wally-config.json          Config (model, iterations, etc.)
        Actors/
            Developer/
                actor.json         Role, AcceptanceCriteria, Intent
            Tester/
                actor.json
        Logs/                      Session logs (auto-created)
```

### actor.json

```json
{
  "name": "Developer",
  "rolePrompt": "Act as an expert software developer, writing clean and efficient code.",
  "criteriaPrompt": "Code must compile without errors, follow best practices, and pass unit tests.",
  "intentPrompt": "Implement the requested feature with proper error handling."
}
```

| Field | RBA Component | Purpose |
|---|---|---|
| `rolePrompt` | Role | The persona the AI adopts |
| `criteriaPrompt` | AcceptanceCriteria | Success criteria the output must meet |
| `intentPrompt` | Intent | The goal the actor pursues |

Add a new actor by creating a subfolder under `.wally/Actors/` with an `actor.json`.

### wally-config.json

```json
{
  "ActorsFolderName": "Actors",
  "DefaultModel": "gpt-4.1",
  "Models": ["gpt-4.1", "claude-sonnet-4", "gpt-5.2"],
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `DefaultModel` | `"gpt-4.1"` | Model passed to `gh copilot --model`. Null = Copilot default. |
| `Models` | `[...]` | Reference list of available model identifiers. |
| `MaxIterations` | `10` | Default iteration cap for `run-loop`. |

---

## Model Selection

Set `DefaultModel` in `wally-config.json`, or override per run:

```sh
wally run Developer "Explain this module" -m claude-sonnet-4
wally run Developer "Explain this module" -m default   # uses config DefaultModel
```

Run `gh copilot -- --help` to see available `--model` choices for your account.

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain library — `Actor`, `WallyLoop`, `WallyWorkspace`, `WallyEnvironment`, `WallyConfig`, RBA types. |
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

---

## License

MIT
