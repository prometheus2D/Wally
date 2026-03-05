# Wally.Console — Quick Setup Guide

Get the Wally CLI running in 2 minutes.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — verify: `dotnet --version`
- [GitHub CLI](https://cli.github.com/) with Copilot extension — verify: `gh copilot --version`
  - Install Copilot: `gh extension install github/gh-copilot`
  - *Or configure a [custom LLM wrapper](#custom-wrapper) for a different backend*

---

## Build

```sh
cd Wally
dotnet build Wally.Console
```

Or publish a standalone exe:

```sh
# Self-contained (no .NET runtime needed on target machine)
dotnet publish Wally.Console -c Release -r win-x64 --self-contained

# Framework-dependent (smaller, requires .NET 8 runtime)
dotnet publish Wally.Console -c Release
```

---

## Setup & First Run

```sh
# 1. Create a workspace at your codebase root
dotnet run --project Wally.Console -- setup C:\repos\MyApp
cd C:\repos\MyApp

# 2. Run a direct prompt (no actor)
.\wally run "What does this codebase do?"

# 3. Run with an actor (adds RBA persona context)
.\wally run "Review the auth module" -a Engineer

# 4. Check what's loaded
.\wally info
.\wally list
```

---

## Interactive Mode

Run `wally` with no arguments:

```
> .\wally
wally> run "Explain error handling" -a Engineer
wally> run "Review the auth module" -a Engineer -l CodeReview
wally> list-loops
wally> runbook full-analysis "Explain the architecture"
wally> help
wally> exit
```

---

## Essential Commands

| Command | What It Does |
|---|---|
| `setup <path>` | Create a workspace at your codebase root |
| `run "<prompt>"` | Direct mode — prompt sent as-is |
| `run "<prompt>" -a <actor>` | Run with an actor (RBA enrichment) |
| `run "<prompt>" -a <actor> -l <loop>` | Named loop (iterative) |
| `run "<prompt>" -a <actor> --loop -n 5` | Inline loop with max iterations |
| `runbook <name> "<prompt>"` | Execute a multi-step runbook |
| `list` | List actors |
| `list-loops` | List loop definitions |
| `list-wrappers` | List LLM wrappers |
| `list-runbooks` | List runbook definitions |
| `info` | Workspace paths, config, session info |
| `tutorial` | Step-by-step guide |
| `help` | Full command reference |

---

## Default Actors

| Actor | Focus |
|---|---|
| **Engineer** | Code reviews, architecture docs, bug reports, test plans |
| **BusinessAnalyst** | Requirements, execution plans, project status |
| **Stakeholder** | Business needs, priorities, success criteria |

---

## Custom Wrapper

Don't have GitHub Copilot? Create a custom LLM wrapper:

```sh
.\wally add-wrapper MyLLM -d "My LLM backend" -e my-llm-cli -t "{model} {prompt}"
```

Or drop a `.json` file in `.wally/Wrappers/`:

```json
{
  "Name": "MyLLM",
  "Description": "My custom LLM backend",
  "Executable": "my-llm-cli",
  "ArgumentTemplate": "{model} {prompt}",
  "ModelArgFormat": "{model}",
  "SourcePathArgFormat": "",
  "UseSourcePathAsWorkingDirectory": false,
  "CanMakeChanges": false
}
```

---

## Next Steps

- Run `.\wally tutorial` for a comprehensive walkthrough
- See the full [Wally.Console README](README.md) for every command and option
- See the [root README](../README.md) for project-wide documentation
- See the [Wally.Core README](../Wally.Core/README.md) for the library API
