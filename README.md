# Wally

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub CLI](https://img.shields.io/badge/Requires-GitHub%20CLI-181717?logo=github)](https://cli.github.com)

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a `.wally/` workspace inside any codebase, loads actors defined by JSON, enriches every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards it to `gh copilot`. Zero prompt engineering required—actors handle it for you.

```
???????????????????????????????????????????????????????????????????????????
?  Your Prompt  ?  Actor (RBA)  ?  Wrapper (LLM)  ?  Structured Output   ?
???????????????????????????????????????????????????????????????????????????
```

---

## ? Quick Start

### Prerequisites

```sh
# 1. Install GitHub CLI
winget install GitHub.cli          # Windows
brew install gh                    # macOS

# 2. Install Copilot extension
gh extension install github/gh-copilot

# 3. Authenticate (Copilot license required)
gh auth login
```

### Install & Run

```sh
# Clone and build
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build

# Publish self-contained (optional)
dotnet publish Wally.Console -c Release -r win-x64 --self-contained -o ./dist
```

### 3-Step Setup

```sh
# 1. Scaffold workspace in your project
wally setup C:\repos\MyApp
cd C:\repos\MyApp

# 2. Run an actor
.\wally run Engineer "Review the authentication module"

# 3. View results
.\wally info    # workspace state
.\wally list    # available actors
```

That's it. Every prompt is enriched with RBA context and logged under `.wally/Logs/`.

---

## ?? Table of Contents

- [Core Concepts](#core-concepts)
- [Commands Reference](#commands-reference)
- [Run Modes](#run-modes)
- [Actors](#actors)
- [Loops](#loops)
- [Wrappers](#wrappers)
- [Workspace Layout](#workspace-layout)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [Extending Wally](#extending-wally)
- [Troubleshooting](#troubleshooting)

---

## Core Concepts

### Role-Based Actors (RBA)

Every actor is defined by three prompt components:

| Component | Purpose | Example |
|-----------|---------|---------|
| **Role** | Persona the AI adopts | *"Act as a senior software engineer..."* |
| **AcceptanceCriteria** | Success criteria for output | *"Output must be technically precise..."* |
| **Intent** | Goal the actor pursues | *"Design and build the system..."* |

When you run `wally run Engineer "Fix the login bug"`, Wally wraps your prompt:

```
# Actor: Engineer
## Role
Act as a senior software engineer responsible for all technical work...

## Acceptance Criteria
Output must be technically precise, trace back to a requirement...

## Intent
Design and build the system to meet requirements...

## Documentation Context
- Templates/ArchitectureTemplate.md
- Actors/Engineer/Docs/README.md

## Prompt
Fix the login bug
```

### Data-Driven Everything

Actors, loops, and wrappers are **JSON files**—no code changes needed to extend:

```
.wally/
??? Actors/          # actor.json per subfolder
??? Loops/           # *.json loop definitions
??? Wrappers/        # *.json LLM wrapper definitions
```

---

## Commands Reference

### Global Commands (no workspace required)

| Command | Description |
|---------|-------------|
| `setup [<path>]` | Scaffold `.wally/` in `<path>` and copy exe there. Re-running repairs missing structure. |
| `setup --verify` | Validate workspace structure without modifications. |
| `load <path>` | Load an existing `.wally/` workspace. |
| `info` | Display workspace paths, actors, wrappers, model config. |
| `commands` | Print command reference. |
| `cleanup [<path>]` | Delete `.wally/` folder to allow fresh setup. |

### Workspace Commands

| Command | Description |
|---------|-------------|
| `list` | List all actors and their RBA prompts. |
| `list-loops` | List all loop definitions. |
| `reload-actors` | Re-read actors from disk. |
| `save <path>` | Persist config and actors to disk. |

### Execution Commands

```sh
# Single run
wally run <actor> "<prompt>" [-m <model>] [-w <wrapper>]

# Iterative loop (generic)
wally run <actor> "<prompt>" --loop [-n <max>]

# Named loop definition
wally run <actor> "<prompt>" -l <loop-name>
```

| Flag | Description |
|------|-------------|
| `-m, --model <model>` | Override AI model (e.g., `claude-sonnet-4`, `gpt-5.2`) |
| `-w, --wrapper <name>` | Override LLM wrapper (e.g., `AutoCopilot` for agentic mode) |
| `--loop` | Enable iterative loop mode |
| `-l, --loop-name <name>` | Use a named loop from `Loops/` |
| `-n, --max-iterations <n>` | Max iterations (implies `--loop` when > 1) |

---

## Run Modes

### One-Shot

Single command execution:

```sh
wally run Engineer "Explain the data access layer"
wally run BusinessAnalyst "Write requirements for user search"
wally run Stakeholder "Define success criteria for the dashboard"
```

### Interactive REPL

Run `wally` with no arguments:

```
.\wally
wally> run Engineer "Review error handling"
wally> run Engineer "Refactor the auth module" --loop -n 5
wally> run Engineer "Code review" -l CodeReview
wally> list-loops
wally> exit
```

### Agentic Mode (File Changes)

Use `AutoCopilot` wrapper for agentic execution with file modifications:

```sh
wally run Engineer "Fix the null reference bug in UserService.cs" -w AutoCopilot
```

---

## Actors

### Default Actors

| Actor | Perspective | Produces |
|-------|-------------|----------|
| **Stakeholder** | Business—defines needs, priorities | Business context, acceptance feedback |
| **BusinessAnalyst** | Bridge—translates needs to specs | Requirements docs, Execution Plans |
| **Engineer** | Technical—designs, builds, tests | Architecture docs, Bug Reports, code reviews |

### Actor Definition (`actor.json`)

```json
{
  "name": "Engineer",
  "rolePrompt": "Act as a senior software engineer responsible for all technical work...",
  "criteriaPrompt": "Output must be technically precise, trace back to a requirement...",
  "intentPrompt": "Design and build the system to meet requirements...",
  "docsFolderName": "Docs"
}
```

### Creating Custom Actors

```sh
# 1. Create folder
mkdir .wally/Actors/SecurityReviewer

# 2. Add actor.json
```

```json
{
  "name": "SecurityReviewer",
  "rolePrompt": "Act as a security engineer focused on threat modeling and vulnerability analysis.",
  "criteriaPrompt": "Identify all security vulnerabilities, rank by severity, provide remediation steps.",
  "intentPrompt": "Review the system for security risks and produce actionable findings.",
  "docsFolderName": "Docs"
}
```

```sh
# 3. Reload and run
wally reload-actors
wally run SecurityReviewer "Review the authentication module"
```

---

## Loops

Loops enable **iterative execution** where each LLM call builds on the previous response.

### Default Loops

| Loop | Description | Max Iterations |
|------|-------------|----------------|
| `SingleRun` | One prompt, one response (default) | 1 |
| `CodeReview` | Multi-pass code review, progressively deeper | 5 |
| `Refactor` | Iterative refactoring until clean | 8 |
| `RequirementsDeepDive` | Deep-dive requirements analysis | 5 |

### Usage

```sh
# Generic loop
wally run Engineer "Refactor error handling" --loop -n 5

# Named loop
wally run Engineer "Review the auth module" -l CodeReview
```

### Loop Definition (`Loops/*.json`)

```json
{
  "Name": "CodeReview",
  "Description": "Iterative code review with progressively deeper analysis",
  "ActorName": "Engineer",
  "StartPrompt": "{userPrompt}\n\nPerform a thorough code review...",
  "ContinuePromptTemplate": "Previous pass:\n---\n{previousResult}\n---\n\nPerform another pass...",
  "CompletedKeyword": "[LOOP COMPLETED]",
  "ErrorKeyword": "[LOOP ERROR]",
  "MaxIterations": 5
}
```

**Placeholders:** `{userPrompt}`, `{previousResult}`, `{completedKeyword}`, `{errorKeyword}`

**Stop conditions:**
- `[LOOP COMPLETED]` in response ? success
- `[LOOP ERROR]` in response ? error
- Max iterations reached

---

## Wrappers

Wrappers define **how to invoke the LLM**—entirely via JSON, no code changes.

### Default Wrappers

| Wrapper | Mode | Command | Can Edit Files |
|---------|------|---------|----------------|
| `Copilot` | Read-only | `gh copilot -p` | ? |
| `AutoCopilot` | Agentic | `gh copilot -i --yolo` | ? |

### Wrapper Definition (`Wrappers/*.json`)

```json
{
  "Name": "Copilot",
  "Description": "Read-only—runs gh copilot -p",
  "Executable": "gh",
  "ArgumentTemplate": "copilot {model} {sourcePath} --yolo -s -p {prompt}",
  "ModelArgFormat": "--model {model}",
  "SourcePathArgFormat": "--add-dir {sourcePath}",
  "UseSourcePathAsWorkingDirectory": true,
  "CanMakeChanges": false
}
```

**Placeholders:** `{prompt}`, `{model}`, `{sourcePath}`

### Adding Custom Wrappers

Drop a `.json` file in `.wally/Wrappers/`:

```json
{
  "Name": "Claude",
  "Description": "Direct Claude API via CLI",
  "Executable": "claude",
  "ArgumentTemplate": "chat --model {model} --prompt {prompt}",
  "ModelArgFormat": "--model {model}",
  "SourcePathArgFormat": "",
  "UseSourcePathAsWorkingDirectory": false,
  "CanMakeChanges": true
}
```

---

## Workspace Layout

```
<YourProject>/                        # WorkSource (your codebase root)
??? .wally/                           # WorkspaceFolder
    ??? wally-config.json             # Configuration
    ??? Docs/                         # Shared documentation
    ??? Templates/                    # Document templates
    ?   ??? RequirementsTemplate.md
    ?   ??? ExecutionPlanTemplate.md
    ?   ??? ProposalTemplate.md
    ?   ??? ImplementationPlanTemplate.md
    ?   ??? ArchitectureTemplate.md
    ?   ??? BugTemplate.md
    ?   ??? TestPlanTemplate.md
    ??? Actors/
    ?   ??? Stakeholder/
    ?   ?   ??? actor.json
    ?   ?   ??? Docs/
    ?   ??? BusinessAnalyst/
    ?   ?   ??? actor.json
    ?   ?   ??? Docs/
    ?   ??? Engineer/
    ?       ??? actor.json
    ?       ??? Docs/
    ??? Loops/
    ?   ??? SingleRun.json
    ?   ??? CodeReview.json
    ?   ??? Refactor.json
    ?   ??? RequirementsDeepDive.json
    ??? Wrappers/
    ?   ??? Copilot.json
    ?   ??? AutoCopilot.json
    ??? Logs/                         # Session logs (auto-created)
        ??? <session>/
            ??? <timestamp>.txt
```

Everything under `<YourProject>/` is accessible to the LLM via `--add-dir`.

---

## Configuration

### `wally-config.json`

```json
{
  "ActorsFolderName": "Actors",
  "LogsFolderName": "Logs",
  "DocsFolderName": "Docs",
  "TemplatesFolderName": "Templates",
  "LoopsFolderName": "Loops",
  "WrappersFolderName": "Wrappers",
  "LogRotationMinutes": 2,
  "DefaultModel": "gpt-4.1",
  "DefaultWrapper": "Copilot",
  "MaxIterations": 10,
  "DefaultModels": ["gpt-4.1", "gpt-5.2", "claude-sonnet-4", "..."],
  "DefaultWrappers": ["Copilot", "AutoCopilot"],
  "DefaultLoops": ["SingleRun", "CodeReview", "Refactor", "RequirementsDeepDive"]
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultModel` | `gpt-4.1` | Model passed to wrapper's `{model}` placeholder |
| `DefaultWrapper` | `Copilot` | Wrapper used when `-w` not specified |
| `MaxIterations` | `10` | Default cap for loop iterations |
| `LogRotationMinutes` | `2` | Minutes per log file (`0` = single file) |

### Runtime Overrides

```sh
wally run Engineer "Explain this" -m claude-sonnet-4 -w AutoCopilot
```

---

## Architecture

```
??????????????????????????????????????????????????????????????????????????????
?                              Wally.Console                                  ?
?                         (CLI / Interactive REPL)                           ?
??????????????????????????????????????????????????????????????????????????????
                                  ?
??????????????????????????????????????????????????????????????????????????????
?                            Wally.Core                                       ?
?  ???????????????????  ???????????????????  ???????????????????             ?
?  ? WallyEnvironment?  ?   WallyLoop     ?  ?  WallyWorkspace ?             ?
?  ? (orchestration) ?  ? (iteration)     ?  ? (disk layout)   ?             ?
?  ???????????????????  ???????????????????  ???????????????????             ?
?           ?                    ?                    ?                       ?
?  ???????????????????  ???????????????????  ???????????????????             ?
?  ?     Actor       ?  ? WallyLoopDef    ?  ?   WallyConfig   ?             ?
?  ? (RBA prompts)   ?  ? (JSON-driven)   ?  ? (settings)      ?             ?
?  ???????????????????  ???????????????????  ???????????????????             ?
?           ?                                                                 ?
?  ???????????????????                                                        ?
?  ?   LlmWrapper    ? ??????????????????????????????????????? gh copilot    ?
?  ? (JSON-driven)   ?                                                        ?
?  ???????????????????                                                        ?
??????????????????????????????????????????????????????????????????????????????
```

| Layer | Responsibility |
|-------|----------------|
| `Actor` | RBA personality, prompt enrichment |
| `LlmWrapper` | CLI recipe, process spawning (JSON-driven) |
| `WallyEnvironment` | Orchestration (actor + wrapper + model) |
| `WallyLoop` / `WallyLoopDefinition` | Iteration logic, stop conditions |
| `WallyWorkspace` / `WallyConfig` | Disk layout, configuration |
| `SessionLogger` | Structured logging with rotation |

### Projects

| Project | Purpose |
|---------|---------|
| `Wally.Core` | Domain library—Actor, Loop, Workspace, Config. Embeddable in any .NET 8 app. |
| `Wally.Console` | CLI entry point—verbs, REPL, ships default workspace template. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## Extending Wally

### Add an Actor

```sh
mkdir .wally/Actors/DBA
echo '{"name":"DBA","rolePrompt":"Act as a database administrator...","criteriaPrompt":"...","intentPrompt":"..."}' > .wally/Actors/DBA/actor.json
wally reload-actors
```

### Add a Loop

```sh
cat > .wally/Loops/BugHunt.json << 'EOF'
{
  "Name": "BugHunt",
  "Description": "Iterative bug hunting",
  "ActorName": "Engineer",
  "StartPrompt": "{userPrompt}\n\nSearch for bugs...",
  "ContinuePromptTemplate": "Previous findings:\n{previousResult}\n\nContinue searching...",
  "CompletedKeyword": "[LOOP COMPLETED]",
  "ErrorKeyword": "[LOOP ERROR]",
  "MaxIterations": 10
}
EOF
```

### Add a Wrapper

```sh
cat > .wally/Wrappers/Ollama.json << 'EOF'
{
  "Name": "Ollama",
  "Description": "Local Ollama model",
  "Executable": "ollama",
  "ArgumentTemplate": "run {model} {prompt}",
  "ModelArgFormat": "",
  "SourcePathArgFormat": "",
  "UseSourcePathAsWorkingDirectory": true,
  "CanMakeChanges": false
}
EOF
wally run Engineer "Explain this" -w Ollama -m codellama
```

---

## Troubleshooting

| Symptom | Solution |
|---------|----------|
| `'gh' is not recognized` | Install [GitHub CLI](https://cli.github.com) and add to PATH |
| `gh copilot: command not found` | `gh extension install github/gh-copilot` |
| `HTTP 401` | `gh auth login` (ensure Copilot license) |
| Empty responses | `wally info`—verify WorkSource points to a directory with code |
| Model not available | `gh copilot -- --help`—check `--model` choices |
| Actor not found | `wally list` / `wally reload-actors` |
| Missing workspace structure | `wally setup <path>` repairs missing folders |
| Loop never completes | Increase `-n` or edit loop's `MaxIterations` |

---

## License

MIT © [prometheus2D](https://github.com/prometheus2D)
