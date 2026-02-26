# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a workspace next to any codebase, loads actors defined by Role × AcceptanceCriteria × Intent triples, enriches every prompt with your registered files and folders, and forwards the result to `gh copilot`. Works as a standalone CLI, an interactive REPL, or embedded in your own .NET app via `Wally.Core`.

---

## How it works

```
<ParentFolder>/
    Project/          ? your codebase (configurable name)
    .wally/           ? Wally's workspace
        wally-config.json
        default-agents.json
        Roles/
            Developer.txt       ? edit to change the role's prompt
            Tester.txt
        Criteria/
            CodeQuality.txt
            UserSatisfaction.txt
        Intents/
            ImplementFeature.txt
            FixBug.txt
```

- **Parent folder** — the root you point Wally at. Can be any directory.
- **Project folder** — sibling subfolder Wally treats as the codebase.
- **Workspace folder** — sibling subfolder holding config and RBA prompt files.
- **Prompt files** — each `.txt` file under `Roles/`, `Criteria/`, or `Intents/` defines one RBA item. The filename stem is the name; the file content is the prompt. Edit them directly — no JSON required.
- **Actors** — built from every `Role × AcceptanceCriteria × Intent` combination. Prompt files are re-read on every `load`.
- **References** — explicit opt-in: add files and folders with `add-file` / `add-folder`. Only those paths are sent to Copilot.

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
# 1. Copy wally.exe into your project root (or any parent folder)
# 2. Self-assemble the workspace next to the exe
wally setup

# 3. Load actors from the default agents file
wally load-actors .wally\default-agents.json

# 4. Register the code you want Copilot to reason about
wally add-folder .\Project\src
wally add-file   .\Project\src\Program.cs

# 5. Run a prompt
wally run "Implement input validation for the login form"
```

### Option B — point at an existing project path

```sh
# Create a workspace at an explicit parent folder
wally create C:\repos\MyApp

# Load it back later (parent folder, not the .wally subfolder)
wally load C:\repos\MyApp

# Load actors
wally load-actors C:\repos\MyApp\.wally\default-agents.json

# Register context
wally add-folder C:\repos\MyApp\Project\src

# Ask
wally run "Refactor the repository layer to use the Unit of Work pattern"
```

### Option C — interactive REPL

Run `wally` with no arguments to enter interactive mode. The environment persists across commands in the session.

```
wally> setup
wally> load-actors .wally\default-agents.json
wally> add-folder .\Project\src
wally> run "Add retry logic to the HTTP client"
wally> run-iterative "Improve test coverage" -m 3
wally> exit
```

---

## Setup from source

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
# Binary is at: Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe
```

---

## Command reference

### Workspace

| Command | Description |
|---|---|
| `setup` | Scaffold `.wally/` + `Project/` next to the exe, load the workspace. |
| `create <path>` | Scaffold a new workspace at `<path>` and load it. |
| `load <path>` | Load an existing workspace from `<path>` (parent folder). |
| `save <path>` | Persist the current config to `<path>`. |
| `info` | Print parent, project, and workspace paths plus loaded counts. |

### Actors

| Command | Description |
|---|---|
| `load-actors <path>` | Load RBA actor definitions from a JSON file (see `default-agents.json`). |
| `list` | List all loaded actors, folder references, and file references. |

### Context (what Copilot can see)

| Command | Description |
|---|---|
| `add-folder <path>` | Register a folder — appended to every prompt. |
| `add-file <path>` | Register a file — appended to every prompt. |
| `remove-folder <path>` | Deregister a folder. |
| `remove-file <path>` | Deregister a file. |
| `clear-refs` | Clear all registered folders and files. |

### Running

| Command | Description |
|---|---|
| `run "<prompt>" [actor]` | Run all actors (or one by name) on the prompt. |
| `run-iterative "<prompt>" [-m N]` | Run actors iteratively, feeding each response back as the next prompt. Stops after N iterations or when no response is returned. |

### Other

| Command | Description |
|---|---|
| `help` | Print command reference. |
| `--help` | CommandLine parser help. |
| `--version` | Print version. |

---

## Workspace config (`wally-config.json`)

```jsonc
{
  "WorkspaceFolderName": ".wally",   // name of the workspace subfolder
  "ProjectFolderName":   "Project",  // name of the project subfolder
  "MaxIterations": 10,               // default cap for run-iterative
  "Roles": [ ... ],
  "AcceptanceCriterias": [ ... ],
  "Intents": [ ... ]
}
```

Actors are the cartesian product of all Roles × AcceptanceCriterias × Intents. The `default-agents.json` file (2 Roles × 2 Criteria × 2 Intents = 8 actors) is the recommended starting point.

---

## Actor agents file (`default-agents.json`)

```json
{
  "Roles":              [{ "Name": "Developer", "Prompt": "...", "Tier": "task" }, ...],
  "AcceptanceCriterias":[{ "Name": "CodeQuality", "Prompt": "...", "Tier": "task" }, ...],
  "Intents":            [{ "Name": "ImplementFeature", "Prompt": "...", "Tier": "task" }, ...]
}
```

The `Tier` field is metadata only — it has no effect on execution.

---

## How prompts reach Copilot

```
User prompt
  ? Actor.ProcessPrompt()
Enriched prompt:
  <original prompt>
  [Project Folder: C:\repos\MyApp\Project]
  [Folder References]
    C:\repos\MyApp\Project\src
  [File References]
    C:\repos\MyApp\Project\src\Program.cs
  ?
Role: <role prompt>
Intent: <intent prompt>
Acceptance Criteria: <criteria prompt>
Prompt: <enriched prompt>
  ?
gh copilot explain "<full prompt>"
```

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain model — `WallyWorkspace`, `WallyEnvironment`, `Actor`, `WallyConfig`, RBA types. |
| `Wally.Console` | CLI entry point — verb-based command dispatch, interactive REPL. |
| `Wally.Default` | Shipped defaults — `wally-config.json`, `default-agents.json`. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## License

MIT
