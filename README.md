<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/platform-Windows-0078D6?style=flat&logo=windows" alt="Windows" />
  <img src="https://img.shields.io/badge/license-MIT-green?style=flat" alt="MIT License" />
</p>

# ?? Wally — AI Actor Environment

**Wally** is a .NET 8 desktop application and CLI that manages AI-powered **actors** — persona-driven LLM assistants that produce structured, high-quality output using the **RBA (Role, Acceptance Criteria, Intent)** prompting framework.

Instead of sending raw prompts to an LLM, Wally wraps every prompt in a rich persona context: *who* the AI is (Role), *what success looks like* (Acceptance Criteria), and *what it should accomplish* (Intent). The result is dramatically better, more consistent output — whether you're generating code reviews, architecture documents, requirements, or execution plans.

---

## ? Key Features

| Feature | Description |
|---|---|
| **RBA Actors** | Persona-driven prompting with Role, Acceptance Criteria, and Intent. Three default actors ship out of the box: Engineer, BusinessAnalyst, and Stakeholder. |
| **Direct Mode** | Send prompts without an actor for general questions — no persona enrichment. |
| **JSON-Driven LLM Wrappers** | Swap LLM backends by dropping a `.json` file in `Wrappers/`. Ships with `Copilot` (read-only) and `AutoCopilot` (agentic, can edit files). Zero code changes to add new backends. |
| **Iterative Loops** | Run an actor in a loop — each iteration sees the previous response and decides whether to continue. Named loop definitions (JSON) or inline `--loop` mode. |
| **Runbooks** | `.wrb` plain-text files with one Wally command per line. Chain setup, multiple actors, loops, and other commands into repeatable workflows. |
| **Document Templates** | Architecture, Requirements, Execution Plan, Proposal, Bug Report, Test Plan, and Implementation Plan templates ship with the workspace. |
| **Documentation Context** | Files in `Docs/` folders are listed in the enriched prompt so the LLM knows they exist and can reference them. |
| **WinForms GUI** | Dark-themed desktop app with file explorer, tabbed entity editors, AI chat panel, and integrated command terminal. |
| **CLI + Interactive REPL** | Full-featured command-line interface with one-shot and interactive modes. |
| **Full CRUD for Everything** | Create, edit, delete, and list actors, loops, wrappers, and runbooks from the CLI or GUI. |
| **Session Logging** | Structured JSON logs with configurable rotation under `.wally/Logs/`. |

---

## ?? Projects

| Project | Type | Description |
|---|---|---|
| [`Wally.Core`](Wally.Core/README.md) | Class Library (.NET 8) | Domain library — actors, loops, wrappers, runbooks, workspace management, and command logic. Embeddable in any .NET 8 host. |
| [`Wally.Console`](Wally.Console/README.md) | Console App (.NET 8) | CLI entry point — one-shot commands and interactive REPL. |
| [`Wally.Forms`](Wally.Forms/README.md) | WinForms App (.NET 8) | Desktop GUI — dark-themed IDE-like interface with file explorer, tabbed editors, AI chat, and command terminal. |

> ?? **New here?** See the [Quick Start Guide](QUICK-START.md) to get running in under 5 minutes.

---

## ?? Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [GitHub CLI](https://cli.github.com/) with Copilot extension (`gh extension install github/gh-copilot`) — *or configure a different LLM wrapper*
- Windows (WinForms GUI) — the CLI works on any .NET 8-supported platform

### Build

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

### Run the CLI

```sh
# Set up a workspace at your codebase root
dotnet run --project Wally.Console -- setup C:\repos\MyApp
cd C:\repos\MyApp

# Run a direct prompt (no actor)
.\wally run "What does this codebase do?"

# Run with an actor
.\wally run "Review the auth module for security issues" -a Engineer

# Run with a loop
.\wally run "Review the data access layer" -a Engineer -l CodeReview

# Run a runbook
.\wally runbook full-analysis "Explain the architecture"

# Interactive mode
.\wally
wally> run "Write requirements for search" -a BusinessAnalyst
wally> list
wally> info
wally> exit
```

### Run the GUI

```sh
dotnet run --project Wally.Forms
```

Or publish and run:

```sh
dotnet publish Wally.Forms -c Release
```

---

## ??? Workspace Structure

When you run `wally setup`, a `.wally/` folder is created inside your codebase root:

```
MyApp/                          ? WorkSource (your codebase root)
  .wally/                       ? Workspace folder
    wally-config.json           ? Configuration
    Actors/                     ? One folder per actor
      Engineer/
        actor.json              ? RBA prompts (Role, Criteria, Intent)
        Docs/                   ? Actor-private documentation
      BusinessAnalyst/
        actor.json
        Docs/
      Stakeholder/
        actor.json
        Docs/
    Loops/                      ? Loop definitions (JSON)
      SingleRun.json
      CodeReview.json
      Refactor.json
      RequirementsDeepDive.json
    Wrappers/                   ? LLM wrapper definitions (JSON)
      Copilot.json              ? Read-only (default)
      AutoCopilot.json          ? Agentic, can edit files
    Runbooks/                   ? Command sequence files (.wrb)
      hello-world.wrb
      full-analysis.wrb
    Docs/                       ? Workspace-level documentation
    Templates/                  ? Document templates
      ArchitectureTemplate.md
      RequirementsTemplate.md
      ProposalTemplate.md
      ImplementationPlanTemplate.md
      ExecutionPlanTemplate.md
      BugTemplate.md
      TestPlanTemplate.md
    Logs/                       ? Session logs (JSON, auto-rotated)
```

Everything is file-based and editable. Add new actors, loops, wrappers, or runbooks by creating files — zero code changes.

---

## ?? RBA Prompting Framework

Every actor is defined by three prompt components:

| Component | Purpose | Example |
|---|---|---|
| **Role** | *Who* the AI is | "You are a senior software engineer specializing in code review…" |
| **Acceptance Criteria** | *What success looks like* | "Output must include severity ratings, specific line references…" |
| **Intent** | *What the actor aims to accomplish* | "Produce a thorough code review that identifies bugs, security issues…" |

When you run a prompt through an actor, Wally wraps your input in this RBA context:

```
# Actor: Engineer
## Role
You are a senior software engineer...
## Acceptance Criteria
Output must include severity ratings...
## Intent
Produce a thorough code review...
## Documentation Context
- .wally/Docs/ArchitectureOverview.md (workspace docs)
- .wally/Actors/Engineer/Docs/CodingStandards.md (actor docs)
## Prompt
Review the authentication module for security issues
```

This structured context produces dramatically better, more consistent results compared to raw prompts.

---

## ?? Command Reference

### Workspace

| Command | Description |
|---|---|
| `setup [<path>]` | Scaffold a workspace at `<path>` (creates `.wally/`). |
| `setup --verify` | Check workspace structure without making changes. |
| `load <path>` | Load an existing `.wally/` workspace. |
| `save <path>` | Save config and actors to disk. |
| `cleanup [<path>]` | Delete `.wally/` so setup can run fresh. |
| `info` | Show workspace paths, actors, wrappers, and session info. |

### Running

| Command | Description |
|---|---|
| `run "<prompt>" [options]` | Run a prompt through the AI. |
| &nbsp;&nbsp;`-a, --actor <name>` | Use an actor (adds RBA context). Omit for direct mode. |
| &nbsp;&nbsp;`-m, --model <model>` | Override the AI model. |
| &nbsp;&nbsp;`-w, --wrapper <name>` | Override the LLM wrapper. |
| &nbsp;&nbsp;`--loop` | Run in iterative loop mode. |
| &nbsp;&nbsp;`-l, --loop-name <name>` | Use a named loop definition. |
| &nbsp;&nbsp;`-n, --max-iterations <n>` | Maximum loop iterations. |
| `runbook <name> ["<prompt>"]` | Execute a runbook (`.wrb` command sequence). |

### Actors (CRUD)

| Command | Description |
|---|---|
| `list` | List all actors and their RBA prompts. |
| `add-actor <name> [-r] [-c] [-i]` | Create a new actor. |
| `edit-actor <name> [-r] [-c] [-i]` | Edit an actor's prompts. |
| `delete-actor <name>` | Delete an actor. |
| `reload-actors` | Re-read actor folders from disk. |

### Loops (CRUD)

| Command | Description |
|---|---|
| `list-loops` | List all loop definitions. |
| `add-loop <name> [-d] [-a] [-n] [-s]` | Create a loop. |
| `edit-loop <name> [-d] [-a] [-n] [-s]` | Edit a loop. |
| `delete-loop <name>` | Delete a loop. |

### Wrappers (CRUD)

| Command | Description |
|---|---|
| `list-wrappers` | List all LLM wrapper definitions. |
| `add-wrapper <name> [-d] [-e] [-t] [--can-make-changes]` | Create a wrapper. |
| `edit-wrapper <name> [-d] [-e] [-t] [--can-make-changes]` | Edit a wrapper. |
| `delete-wrapper <name>` | Delete a wrapper. |

### Runbooks (CRUD)

| Command | Description |
|---|---|
| `list-runbooks` | List all runbook definitions. |
| `add-runbook <name> [-d]` | Create a runbook scaffold. |
| `edit-runbook <name> [-d]` | Edit a runbook's description. |
| `delete-runbook <name>` | Delete a runbook. |

### Help

| Command | Description |
|---|---|
| `commands` / `help` | Full command reference. |
| `tutorial` | Step-by-step getting started guide. |

---

## ?? Default Actors

| Actor | Role | Focus Areas |
|---|---|---|
| **Engineer** | Senior software engineer | Code reviews, architecture docs, proposals, bug reports, test plans |
| **BusinessAnalyst** | Business analyst & project manager | Requirements, execution plans, project status |
| **Stakeholder** | Business stakeholder | Business needs, priorities, success criteria |

---

## ?? LLM Wrappers

Wrappers are JSON definitions that tell Wally how to call an LLM CLI tool:

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

**Shipped wrappers:**

| Wrapper | Description |
|---|---|
| `Copilot` | Read-only — returns a text response (default) |
| `AutoCopilot` | Agentic — can make code/file changes on disk |

**Add a custom wrapper** — drop a `.json` file in `.wally/Wrappers/`:

```sh
wally add-wrapper OllamaChat -d "Local Ollama" -e ollama -t "run {model} {prompt}"
```

---

## ?? Loops

Loops run an actor iteratively. Each iteration sees the previous response and decides whether to continue. The actor signals completion with `[LOOP COMPLETED]` or error with `[LOOP ERROR]`.

```sh
# Named loop
wally run "Review the data access layer" -a Engineer -l CodeReview

# Inline loop
wally run "Refactor error handling" -a Engineer --loop -n 5
```

---

## ?? Runbooks

Runbooks are `.wrb` files with one Wally command per line — repeatable multi-step workflows:

```
# Full analysis — run all default actors on a prompt
run "{userPrompt}" -a Stakeholder
run "{userPrompt}" -a BusinessAnalyst
run "{userPrompt}" -a Engineer
```

**Placeholders:** `{userPrompt}`, `{workSourcePath}`, `{workspaceFolder}`

**Nesting:** Runbooks can call other runbooks (capped at 10 levels).

```sh
wally runbook full-analysis "Explain the architecture"
```

---

## ??? GUI Overview

The WinForms application (`Wally.Forms`) provides a dark-themed IDE-like interface:

```
???????????????????????????????????????????????????????????????
? Menu Bar  ?  ToolStrip                                      ?
???????????????????????????????????????????????????????????????
?           ?  Welcome ? Actor ? Loop ? …  ?                  ?
?  File     ????????????????????????????????   AI Chat        ?
?  Explorer ?                              ?   Panel          ?
?  (left)   ?  Tabbed Editors              ?   (right)        ?
?           ?  • Actor editors             ?   • Actor select ?
?           ?  • Loop editors              ?   • Model select ?
?           ?  • Wrapper editors           ?   • Loop select  ?
?           ?  • Runbook editors           ?   • Wrapper sel  ?
?           ?  • Config editor             ?                  ?
?           ?  • Log viewer                ?                  ?
???????????????????????????????????????????????????????????????
?                    Command Terminal (bottom)                  ?
?                    Interactive REPL — same commands as CLI    ?
???????????????????????????????????????????????????????????????
```

**Keyboard shortcuts:**

| Shortcut | Action |
|---|---|
| `Ctrl+O` | Open workspace |
| `Ctrl+Shift+N` | Setup new workspace |
| `Ctrl+S` | Save workspace |
| `` Ctrl+` `` | Focus command terminal |
| `Ctrl+1` | Focus file explorer |
| `Ctrl+2` | Focus chat panel |
| `Ctrl+3` | Focus terminal |
| `Ctrl+W` | Close active editor tab |
| `F5` | Refresh file explorer |

---

## ?? Repository Structure

```
Wally.sln
??? Wally.Core/             ? Domain library (actors, loops, wrappers, workspace)
?   ??? Actors/             ? Actor class (RBA prompt pipeline)
?   ??? RBA/                ? Role, AcceptanceCriteria, Intent types
?   ??? LLMWrappers/        ? LLMWrapper (JSON-driven CLI wrapper)
?   ??? Logging/            ? SessionLogger, LogEntry
?   ??? Default/            ? Shipped workspace template
?   ?   ??? Actors/         ? Default actor definitions
?   ?   ??? Loops/          ? Default loop definitions
?   ?   ??? Wrappers/       ? Default wrapper definitions
?   ?   ??? Runbooks/       ? Default runbook files
?   ?   ??? Docs/           ? Default documentation
?   ?   ??? Templates/      ? Document templates
?   ??? WallyEnvironment.cs ? Runtime orchestration layer
?   ??? WallyWorkspace.cs   ? Workspace layout & loading
?   ??? WallyConfig.cs      ? Configuration model
?   ??? WallyCommands.cs    ? All command implementations + shared dispatcher
?   ??? WallyLoop.cs        ? Iterative execution loop
?   ??? WallyLoopDefinition.cs ? Serializable loop definition
?   ??? WallyRunbook.cs     ? Runbook model & parser
?   ??? WallyHelper.cs      ? Loading, scaffolding, utilities
??? Wally.Console/          ? CLI entry point
?   ??? Program.cs          ? One-shot + interactive REPL
?   ??? Options/            ? CommandLineParser verb definitions
??? Wally.Forms/            ? WinForms GUI
?   ??? Controls/           ? UI panels (FileExplorer, Chat, Command, Welcome)
?   ?   ??? Editors/        ? Entity editors (Actor, Loop, Wrapper, Runbook, Config, Logs)
?   ??? Theme/              ? WallyTheme dark theme tokens & renderer
?   ??? Wally.Forms.cs      ? Main form orchestration
```

---

## ?? License

This project is open source. See the repository for license details.
