# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a workspace next to any codebase, loads agents defined by individual folders under `.wally/Agents/`, enriches every prompt with your registered files and folders, and forwards the result to `gh copilot`. Works as a standalone CLI, an interactive REPL, or embedded in your own .NET app via `Wally.Core`.

---

## How it works

```
<ParentFolder>/
    Project/              ? your codebase (configurable name)
    .wally/               ? Wally's workspace
        wally-config.json
        Agents/
            Developer/    ? one folder = one agent
                role.txt
                criteria.txt
                intent.txt
            Tester/
                role.txt
                criteria.txt
                intent.txt
```

- **Parent folder** — the root you point Wally at.
- **Project folder** — sibling subfolder Wally treats as the codebase.
- **Workspace folder** — sibling subfolder holding config and all agent folders.
- **Agent folders** — each subfolder under `Agents/` defines one independent agent. The folder name is the agent name. Add a folder to create a new agent; delete a folder to remove one.
- **Prompt files** — each agent folder contains `role.txt`, `criteria.txt`, and `intent.txt`. The first line of any file may be a metadata header (`# Tier: task`). Edit the files directly — no JSON required.
- **References** — explicit opt-in: register files and folders with `add-file` / `add-folder`. Only those paths are appended to every prompt.

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
# 2. Self-assemble the workspace next to the exe (copies default agents)
wally setup

# 3. Register the code you want Copilot to reason about
wally add-folder .\Project\src
wally add-file   .\Project\src\Program.cs

# 4. Run a prompt against all agents
wally run "Implement input validation for the login form"

# 5. Run against one specific agent by name
wally run "Implement input validation" Developer
```

### Option B — point at an existing project path

```sh
# setup accepts --path / -p to target any folder directly
wally setup --path C:\repos\MyApp

wally add-folder C:\repos\MyApp\Project\src
wally run "Refactor the repository layer to use the Unit of Work pattern"
```

### Option C — iterative loop

`run-iterative` feeds each response back as the next prompt. Omit `-a` to loop all
agents together; supply `-a <name>` to drive a single agent.

```sh
wally setup --path C:\repos\MyApp
wally add-folder C:\repos\MyApp\Project\src

# Loop all agents (combined responses feed back each iteration)
wally run-iterative "Improve error handling across all services"

# Loop one agent, capped at 4 iterations
wally run-iterative "Add async/await throughout the data layer" -a Developer -m 4
```

### Option D — interactive REPL

Run `wally` with no arguments to enter interactive mode. The environment persists across commands in the session.

```
wally> setup --path C:\repos\MyApp
wally> add-folder .\Project\src
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
| `setup [-p <path>]` | Scaffold `.wally/` + `Project/` next to the exe, or at `<path>` when supplied. |
| `create <path>` | Scaffold a new workspace at `<path>` and load it. |
| `load <path>` | Load an existing workspace from `<path>` (parent folder). |
| `save <path>` | Persist the current config and all agent prompt files to `<path>`. |
| `info` | Print paths, loaded agents, reference counts, and settings. |

### Agents

| Command | Description |
|---|---|
| `list` | List all agents (with prompts), folder references, and file references. |
| `reload-agents` | Re-read agent folders from disk and rebuild actors without a full reload. |

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
| `run "<prompt>" [agent]` | Run all agents (or one by name) on the prompt. |
| `run-iterative "<prompt>" [-m N]` | Loop all agents, feeding combined responses back each iteration. |
| `run-iterative "<prompt>" -a <agent> [-m N]` | Loop a single named agent, feeding its response back each iteration. |

---

## How the iterative loop works

The loop runs directly inside `WallyEnvironment`. On each iteration the previous response is
passed back through `Actor.ProcessPrompt` so the agent's full RBA context (Role,
AcceptanceCriteria, Intent, file/folder references) is re-applied before the next `Act` call.
The loop stops early when the actor returns an empty response.

```
Initial prompt
  ? WallyEnvironment.RunActorIterative / RunActorsIterative
  ?? Iteration 1 ???????????????????????????????????????????
  ?  actor.Act(prompt)  ?  response?                       ?
  ??????????????????????????????????????????????????????????
  ?? Iteration 2 ???????????????????????????????????????????
  ?  actor.Act( ProcessPrompt(response?) )  ?  response?  ?
  ??????????????????????????????????????????????????????????
  … up to MaxIterations or until the actor returns empty
  ?
Final response returned
```

---

## Workspace config (`wally-config.json`)

```jsonc
{
  "WorkspaceFolderName": ".wally",   // name of the workspace subfolder
  "ProjectFolderName":   "Project",  // name of the project subfolder
  "AgentsFolderName":    "Agents",   // name of the agents subfolder inside .wally
  "MaxIterations": 10                // default cap for run-iterative
}
```

---

## Agent folders

```
.wally/Agents/<AgentName>/
    role.txt      — Role prompt
    criteria.txt  — Acceptance criteria prompt
    intent.txt    — Intent prompt
```

Each file is plain text. An optional first line in the form `# Tier: <value>` sets the tier metadata (e.g. `task`, `story`, `epoch`):

```
# Tier: task
Act as an expert software developer, writing clean and efficient code.
```

Add a new subfolder to create a new agent. Each agent is fully independent — no shared state.

---

## How prompts reach Copilot

```
User prompt
  ? Actor.ProcessPrompt()
# Agent: Developer
## Role
Act as an expert software developer…
## Acceptance Criteria
Code must compile without errors…
## Intent
Implement the requested feature…

## Prompt
<user's prompt>

[Project Folder: C:\repos\MyApp\Project]
[Folder References]
  C:\repos\MyApp\Project\src
[File References]
  C:\repos\MyApp\Project\src\Program.cs
  ?
gh copilot suggest "<full structured prompt>"   ? run-iterative (CopilotActor)
gh copilot explain "<full structured prompt>"   ? run / run-iterative (WallyActor)
```

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain model — `WallyWorkspace`, `WallyEnvironment`, `Actor`, `WallyConfig`, RBA types. |
| `Wally.Console` | CLI entry point — verb-based command dispatch, interactive REPL. |
| `Wally.Default` | Shipped defaults — `wally-config.json`, default `Agents/` tree. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## License

MIT
