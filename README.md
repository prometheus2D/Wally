# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a workspace next to any codebase, loads actors defined by individual folders under `.wally/Actors/`, enriches every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards the result to `gh copilot`. Works as a standalone CLI, an interactive REPL, or embedded in your own .NET app via `Wally.Core`.

---

## ? 2-Minute Quick Start

### Prerequisites

Make sure these are installed before you begin:

```sh
# .NET 8 SDK — https://dotnet.microsoft.com/download
dotnet --version          # should print 8.x or later

# GitHub CLI — https://cli.github.com
gh --version

# Copilot extension
gh extension install github/gh-copilot
gh copilot --version      # verify it's installed
gh auth login             # make sure you're authenticated
```

### Build & Run

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build

# Run from the project you want Copilot to see:
cd C:\repos\MyApp
dotnet run --project C:\repos\Wally\Wally.Console -- setup
dotnet run --project C:\repos\Wally\Wally.Console -- run "Explain the architecture of this project"
```

Or publish a standalone exe and drop it anywhere:

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
# Binary: Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe
```

### Three commands to go from zero to prompt

```sh
# 1. Scaffold the .wally workspace (creates .wally/ with default actors)
wally setup

# 2. Point SourcePath at your codebase (so Copilot sees those files)
wally setup -s C:\repos\MyApp

# 3. Send a prompt — every actor responds with Copilot's answer
wally run "Explain the error handling strategy in this project"
```

That's it. Wally enriches your prompt with each actor's Role, AcceptanceCriteria, and Intent, then pipes it to `gh copilot explain` with the working directory set to your `SourcePath`.

### Target a single actor

```sh
wally run "Add input validation to the login form" Developer
```

### Interactive REPL

```sh
wally
wally> setup -s C:\repos\MyApp
wally> run "What does the Program.cs entry point do?"
wally> run "Suggest improvements to the error handling" Developer
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
  ? piped to gh copilot explain (working directory = SourcePath)
  ? response returned to the console
```

Each actor enriches the user's raw prompt with its own RBA context, then Wally pipes the full structured prompt to `gh copilot explain`. The `SourcePath` is set as the process working directory so Copilot CLI can see the target codebase's files and directories for context.

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
- **Actor folders** — each subfolder under `Actors/` defines one independent actor. The folder name is the actor name. Add a folder to create a new actor; delete a folder to remove one.
- **actor.json** — each actor folder contains an `actor.json` with `name`, `rolePrompt`, `criteriaPrompt`, and `intentPrompt`. Edit the JSON to change prompts.
- **SourcePath** — the directory whose files provide context to Copilot. Set in `wally-config.json` or via `setup -s <path>`. Defaults to the workspace's parent folder.

---

## Workspace config (`wally-config.json`)

```json
{
  "ActorsFolderName": "Actors",
  "SourcePath": "C:\\repos\\MyApp",
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `Actors` | Subfolder inside the workspace that holds actor directories |
| `SourcePath` | `null` | Directory whose files give context to `gh copilot`. Defaults to workspace parent when null. |
| `MaxIterations` | `10` | Default cap for `run-iterative` |

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
| `info` | Print paths (including SourcePath), loaded actors, and settings. |

### Actors

| Command | Description |
|---|---|
| `list` | List all actors and their prompts. |
| `reload-actors` | Re-read actor folders from disk and rebuild actors without a full reload. |

### Running

| Command | Description |
|---|---|
| `run "<prompt>" [actor]` | Run all actors (or one by name) on the prompt. |
| `run-iterative "<prompt>" [-m N]` | Loop all actors, feeding combined responses back each iteration. |
| `run-iterative "<prompt>" -a <actor> [-m N]` | Loop a single named actor, feeding its response back each iteration. |

---

## How the iterative loop works

The loop runs directly inside `WallyEnvironment`. On each iteration the previous response is
passed back through `Actor.ProcessPrompt` so the actor's full RBA context (Role,
AcceptanceCriteria, Intent) is re-applied before the next `Act` call.
The loop stops early when the actor returns an empty response.

```sh
# Loop all actors (combined responses feed back each iteration)
wally run-iterative "Improve error handling across all services"

# Loop one actor, capped at 4 iterations
wally run-iterative "Add async/await throughout the data layer" -a Developer -m 4
```

---

## SourcePath — controlling Copilot's file context

By default, `gh copilot` uses the **current working directory** for file context.
Wally's `SourcePath` config lets you explicitly control which directory Copilot sees,
regardless of where `wally.exe` is launched from.

**Set it during setup:**
```sh
wally setup -s C:\repos\MyApp
```

**Or edit `wally-config.json` directly:**
```json
{
  "SourcePath": "C:\\repos\\MyApp"
}
```

**Verify with:**
```sh
wally info
# Source path:  C:\repos\MyApp
```

When `SourcePath` is null or empty, the workspace's parent folder is used (e.g. if your
workspace is `C:\repos\MyApp\.wally`, the source path defaults to `C:\repos\MyApp`).

---

## Default workspace template

The `Default/` folder ships alongside the executable (from `Wally.Console/Default/`) and serves
as the canonical workspace template. When scaffolding a new workspace its entire contents are
copied recursively into the target folder without overwriting existing files.

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
| Empty responses | Check `wally info` — verify SourcePath points to a real directory with code files. |
| Prompt too long / truncated | Wally writes prompts to a temp file and pipes them in, so length should not be an issue. If Copilot itself truncates, shorten your prompt or actor RBA text. |

---

## License

MIT
