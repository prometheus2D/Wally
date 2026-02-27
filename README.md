# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a workspace next to any codebase, loads actors defined by individual folders under `.wally/Actors/`, enriches every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards the result to `gh copilot`. Works as a standalone CLI, an interactive REPL, or embedded in your own .NET app via `Wally.Core`.

> **Context:** Copilot CLI automatically uses the current working directory and its subdirectories for file context. Run `wally` from your project root so Copilot can see your code.

---

## How it works

```
.wally/                       ? Wally's workspace
    wally-config.json
    Actors/
        Developer/            ? one folder = one actor
            actor.json
        Tester/
            actor.json
```

- **Workspace folder** — the `.wally/` directory holding config and all actor folders.
- **Actor folders** — each subfolder under `Actors/` defines one independent actor. The folder name is the actor name. Add a folder to create a new actor; delete a folder to remove one.
- **actor.json** — each actor folder contains an `actor.json` with `name`, `rolePrompt`, `criteriaPrompt`, and `intentPrompt`. Edit the JSON to change prompts.
- **File context** — handled automatically by Copilot CLI based on your working directory. No manual file registration needed.

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET | 8+ |
| GitHub CLI | latest |
| GitHub Copilot CLI extension | `gh extension install github/gh-copilot` |

---

## Quick start

### Option A — drop into any project

```sh
# 1. Copy wally.exe into your project root
# 2. Self-assemble the workspace next to the exe (copies default actors)
wally setup

# 3. Run a prompt against all actors
wally run "Implement input validation for the login form"

# 4. Run against one specific actor by name
wally run "Implement input validation" Developer
```

### Option B — point at an existing workspace path

```sh
wally setup --path C:\repos\MyApp\.wally
wally run "Refactor the repository layer to use the Unit of Work pattern"
```

### Option C — iterative loop

`run-iterative` feeds each response back as the next prompt. Omit `-a` to loop all
actors together; supply `-a <name>` to drive a single actor.

```sh
wally setup

# Loop all actors (combined responses feed back each iteration)
wally run-iterative "Improve error handling across all services"

# Loop one actor, capped at 4 iterations
wally run-iterative "Add async/await throughout the data layer" -a Developer -m 4
```

### Option D — interactive REPL

Run `wally` with no arguments to enter interactive mode. The environment persists across commands in the session.

```
wally> setup --path C:\repos\MyApp\.wally
wally> run "Add retry logic to the HTTP client"
wally> run-iterative "Improve test coverage" -m 3
wally> run-iterative "Refactor to clean architecture" -a Developer -m 5
wally> list
wally> exit
```

---

## Setup from source

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
# Binary: Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe
```

---

## Command reference

### Workspace

| Command | Description |
|---|---|
| `setup [-p <path>]` | Scaffold `.wally/` next to the exe, or at `<path>` when supplied. |
| `create <path>` | Scaffold a new workspace at `<path>` and load it. |
| `load <path>` | Load an existing workspace from `<path>`. |
| `save <path>` | Persist the current config and all actor.json files to `<path>`. |
| `info` | Print paths, loaded actors, and settings. |

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

## Workspace config (`wally-config.json`)

```json
{
  "ActorsFolderName": "Actors",
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `ActorsFolderName` | `Actors` | Subfolder inside the workspace that holds actor directories |
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

## How prompts reach Copilot

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
  -->
    gh copilot -p "<full structured prompt>"
```

Copilot CLI automatically uses the current working directory for file context.

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain model — `WallyWorkspace`, `WallyEnvironment`, `Actor`, `WallyConfig`, RBA types. |
| `Wally.Console` | CLI entry point — verb-based command dispatch, interactive REPL, ships `Default/` template. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## License

MIT
