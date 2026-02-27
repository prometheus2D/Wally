# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a workspace next to any codebase, loads actors defined by individual folders under `.wally/Actors/`, enriches every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards the result to `gh copilot`. Works as a standalone CLI, an interactive REPL, or embedded in your own .NET app via `Wally.Core`.

---

## ? 2-Minute Quick Start

### 1. Prerequisites

```sh
# .NET 8 SDK — https://dotnet.microsoft.com/download
dotnet --version          # 8.x or later

# GitHub CLI — https://cli.github.com
gh --version

# Copilot CLI extension
gh extension install github/gh-copilot
gh copilot --version      # verify it's installed
gh auth login             # authenticate (Copilot access required)
```

### 2. Build

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

Or publish a self-contained exe:

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
# ? Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe
```

### 3. Setup & first prompt

```sh
# Scaffold the .wally workspace and point SourcePath at your codebase
wally setup -s C:\repos\MyApp

# Target a specific actor
wally run Developer "Add input validation to the login form"
```

That's it. Wally enriches your prompt with each actor's Role, AcceptanceCriteria, and Intent, then passes the full structured prompt to `gh copilot -p` with the working directory set to your `SourcePath`.

### Interactive REPL

```sh
wally                           # no arguments ? REPL mode
wally> setup -s C:\repos\MyApp
wally> run Developer "What does the Program.cs entry point do?"
wally> run Tester "Suggest improvements"
wally> exit
```

---

## How it works

```
User prompt
  ? Actor.ProcessPrompt()
    # Actor: Developer
    ## Role
    Act as an expert software developer…
    ## Acceptance Criteria
    Code must compile without errors…
    ## Intent
    Implement the requested feature…

    ## Prompt
    <user's prompt>
  ? gh copilot [--model <model>] -p "<full structured prompt>"
       (working directory = SourcePath)
  ? response returned to the console
```

Each actor enriches the user's raw prompt with its own RBA context. Wally then invokes
`gh copilot -p` directly (using `ProcessStartInfo.ArgumentList` — no shell, no
escaping issues). The `SourcePath` is set as the process working directory so Copilot CLI
sees the target codebase. If a `DefaultModel` is configured, `--model` is added automatically.

---

## Workspace layout

```
.wally/                        ? Wally's workspace
    wally-config.json
    Actors/
        Developer/             ? one folder = one actor
            actor.json
        Tester/
            actor.json
```

- **Workspace folder** — the `.wally/` directory holding config and all actor folders.
- **Actor folders** — each subfolder under `Actors/` defines one actor. The folder name is the actor name.
- **actor.json** — contains `name`, `rolePrompt`, `criteriaPrompt`, and `intentPrompt`.
- **SourcePath** — the directory whose files provide context to Copilot. Defaults to the workspace's parent folder.

---

## Workspace config (`wally-config.json`)

```json
{
  "ActorsFolderName": "Actors",
  "SourcePath": "C:\\repos\\MyApp",
  "DefaultModel": "gpt-4.1",
  "Models": [
    "claude-sonnet-4.6",
    "claude-sonnet-4.5",
    "claude-haiku-4.5",
    "claude-opus-4.6",
    "claude-opus-4.6-fast",
    "claude-opus-4.5",
    "claude-sonnet-4",
    "gemini-3-pro-preview",
    "gpt-5.3-codex",
    "gpt-5.2-codex",
    "gpt-5.2",
    "gpt-5.1-codex-max",
    "gpt-5.1-codex",
    "gpt-5.1",
    "gpt-5.1-codex-mini",
    "gpt-5-mini",
    "gpt-4.1"
  ],
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `"Actors"` | Subfolder inside the workspace that holds actor directories. |
| `SourcePath` | `null` | Directory whose files give context to `gh copilot`. Defaults to workspace parent when null. |
| `DefaultModel` | `"gpt-4.1"` | LLM model passed via `--model` to all actors. Null = Copilot default. |
| `Models` | `[…]` | List of available/allowed model identifiers for this workspace. |
| `MaxIterations` | `10` | Default cap for `run-iterative`. |

### Available models

Run `gh copilot -- --help` to see the `--model` choices available to your account. Current models:

| Model ID | Provider |
|---|---|
| `claude-sonnet-4.6` | Anthropic |
| `claude-sonnet-4.5` | Anthropic |
| `claude-haiku-4.5` | Anthropic |
| `claude-opus-4.6` | Anthropic |
| `claude-opus-4.6-fast` | Anthropic |
| `claude-opus-4.5` | Anthropic |
| `claude-sonnet-4` | Anthropic |
| `gemini-3-pro-preview` | Google |
| `gpt-5.3-codex` | OpenAI |
| `gpt-5.2-codex` | OpenAI |
| `gpt-5.2` | OpenAI |
| `gpt-5.1-codex-max` | OpenAI |
| `gpt-5.1-codex` | OpenAI |
| `gpt-5.1` | OpenAI |
| `gpt-5.1-codex-mini` | OpenAI |
| `gpt-5-mini` | OpenAI |
| `gpt-4.1` | OpenAI |

Set `DefaultModel` in `wally-config.json` to pick which one is used for all actors.

---

## Actor folders

```
.wally/Actors/<ActorName>/
    actor.json
```

Each `actor.json` contains:

```json
{
  "name": "Developer",
  "rolePrompt": "Act as an expert software developer, writing clean and efficient code.",
  "criteriaPrompt": "Code must compile without errors, follow best practices, and pass unit tests.",
  "intentPrompt": "Implement the requested feature with proper error handling."
}
```

| JSON key | RBA Component | Description |
|---|---|---|
| `rolePrompt` | Role | The persona the AI adopts |
| `criteriaPrompt` | AcceptanceCriteria | Success criteria the output must meet |
| `intentPrompt` | Intent | The goal or task the actor pursues |

Add a new subfolder with an `actor.json` to create a new actor. Each actor is fully independent — no shared state.

---

## Command reference

### Workspace

| Command | Description |
|---|---|
| `setup [-p <path>] [-s <source>]` | Scaffold `.wally/` next to the exe (or at `<path>`). `-s` sets the source directory for Copilot context. |
| `create <path>` | Scaffold a new workspace at `<path>` and load it. |
| `load <path>` | Load an existing workspace from `<path>`. |
| `save <path>` | Persist the current config and all actor.json files to `<path>`. |
| `info` | Print paths, model config, loaded actors, and settings. |

### Actors

| Command | Description |
|---|---|
| `list` | List all actors and their prompts. |
| `reload-actors` | Re-read actor folders from disk and rebuild actors without a full reload. |

### Running

| Command | Description |
|---|---|
| `run <actor> "<prompt>" [-m <model>]` | Run a specific actor by name on the prompt. `-m` overrides the model for this run. |
| `run-iterative "<prompt>" [--model <model>] [-m N]` | Loop all actors, feeding combined responses back each iteration. |
| `run-iterative "<prompt>" -a <actor> [--model <model>] [-m N]` | Loop a single named actor, feeding its response back each iteration. |

---

## SourcePath — controlling Copilot's file context

Wally's `SourcePath` config controls which directory `gh copilot` uses for file context.

```sh
# Set during setup
wally setup -s C:\repos\MyApp

# Or edit wally-config.json
{ "SourcePath": "C:\\repos\\MyApp" }

# Verify
wally info
```

When `SourcePath` is null, the workspace's parent folder is used (e.g. `.wally/` inside
`C:\repos\MyApp` ? source path = `C:\repos\MyApp`).

---

## Model selection

Control which LLM model Copilot uses by editing `wally-config.json`:

```json
{
  "DefaultModel": "gpt-4.1",
  "Models": ["gpt-4.1", "claude-sonnet-4", "gpt-5.2"]
}
```

- **`DefaultModel`** — the model passed to `--model` for every actor invocation. `"gpt-4.1"` by default (free tier). Null = Copilot picks.
- **`Models`** — a reference list of model identifiers available to this workspace.

When `DefaultModel` is set, Wally adds `--model <id>` to the `gh copilot` invocation.

Override per run with `-m`:

```sh
# Use a specific model for one run
wally run Developer "Explain this module" -m claude-sonnet-4

# Explicitly use the configured default (useful in scripts)
wally run Developer "Explain this module" -m default
```

---

## How the iterative loop works

The loop runs directly inside `WallyEnvironment`. On each iteration the previous response is
passed back through `Actor.ProcessPrompt` so the actor's full RBA context is re-applied.
The loop stops early when the actor returns an empty response.

```sh
wally run-iterative "Improve error handling across all services"
wally run-iterative "Add async/await throughout the data layer" -a Developer -m 4
```

---

## Default workspace template

The `Default/` folder ships alongside the executable (from `Wally.Console/Default/`) and serves
as the canonical workspace template. When scaffolding, its contents are copied recursively
into the target folder without overwriting existing files.

Config resolution follows a three-tier fallback:
1. **Workspace-local** (`<workspaceFolder>/wally-config.json`)
2. **Template** (`<exeDir>/Default/wally-config.json`)
3. **Hard-coded** (`new WallyConfig()`)

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain model — `WallyWorkspace`, `WallyEnvironment`, `Actor`, `WallyConfig`, RBA types. |
| `Wally.Console` | CLI entry point — verb-based command dispatch, interactive REPL, ships `Default/` template. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `'gh' is not recognized` | Install [GitHub CLI](https://cli.github.com) and add it to your PATH. |
| `gh copilot: command not found` | Run `gh extension install github/gh-copilot`. |
| `HTTP 401 / not authenticated` | Run `gh auth login` and ensure your account has Copilot access. |
| Empty responses | Check `wally info` — verify SourcePath points to a real directory with code. |
| Model not available | Run `gh copilot -- --help` and check `--model` choices, then update `DefaultModel` in config. |
| `Copilot exited with code 1` | Run `gh copilot -p "test"` manually to verify Copilot works outside Wally. |

---

## License

MIT
