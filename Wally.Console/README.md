# Wally.Console

CLI entry point for Wally. See the [Wally.Core README](../Wally.Core/README.md) for full library reference.

## Build

```sh
dotnet build
# Or publish a self-contained exe:
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

## Quick Start

```sh
# Point Wally at your codebase root — .wally/ is created and the exe is copied there
wally setup C:\repos\MyApp
cd C:\repos\MyApp

# Run actors from your project root
.\wally run Engineer "Review the authentication module and document the architecture"
.\wally run BusinessAnalyst "Write requirements for the search feature"
.\wally run Stakeholder "Define success criteria for the dashboard"

# Run an actor in a loop
.\wally run Engineer "Refactor error handling across the project" --loop -n 5

# Run a named loop definition
.\wally run Engineer "Review the auth module" -l CodeReview

# Override the model for a single run
.\wally run Engineer "Explain the data layer" -m claude-sonnet-4

# Override the wrapper for agentic file changes
.\wally run Engineer "Fix the login bug" -w AutoCopilot

# List actors, loops, and workspace info
.\wally list
.\wally list-loops
.\wally info
```

## Run Modes

**One-shot** — single command, then exit:

```sh
.\wally run Engineer "Describe the main entry point"
```

**Interactive REPL** — run `wally` with no arguments:

```
.\wally
wally> run Engineer "Explain error handling"
wally> run Engineer "Review the auth module" --loop -l CodeReview
wally> list-loops
wally> info
wally> exit
```

## Commands

| Command | Description |
|---|---|
| `setup [<path>]` | Scaffold or load a workspace at `<path>` (your codebase root). |
| `setup --verify` | Check workspace structure without making changes. |
| `load <path>` | Load an existing `.wally/` workspace folder. |
| `info` | Show workspace paths, actors, wrappers, model config, and session info. |
| `list` | List all actors and their RBA prompts. |
| `list-loops` | List all loops and their settings. |
| `run <actor> "<prompt>" [options]` | Run an actor on a prompt. |
|   `-m, --model <model>` | Override the AI model. |
|   `-w, --wrapper <name>` | Override the LLM wrapper (e.g. AutoCopilot). |
|   `--loop` | Run in iterative loop mode. |
|   `-l, --loop-name <name>` | Use a named loop from Loops/. |
|   `-n, --max-iterations <n>` | Maximum iterations (implies --loop when > 1). |
| `save <path>` | Save config and actor files to disk. |
| `reload-actors` | Re-read actor folders from disk. |
| `cleanup [<path>]` | Delete the local `.wally/` folder so setup can run fresh. |
| `help` | Show the help message. |

## Default Workspace Template

The `Default/` folder ships alongside the exe and is copied into new workspaces
on `setup`:

```
Default/
    wally-config.json
    Docs/
        README.md
    Templates/
        RequirementsTemplate.md
        ExecutionPlanTemplate.md
        ProposalTemplate.md
        ImplementationPlanTemplate.md
        ArchitectureTemplate.md
        BugTemplate.md
        TestPlanTemplate.md
    Actors/
        Stakeholder/
            actor.json
            Docs/README.md
        BusinessAnalyst/
            actor.json
            Docs/README.md
        Engineer/
            actor.json
            Docs/README.md
    Loops/
        SingleRun.json
        CodeReview.json
        Refactor.json
        RequirementsDeepDive.json
    Wrappers/
        Copilot.json
        AutoCopilot.json
```

- **Actors** — add new actors by creating subfolders under `Actors/` with an `actor.json`.
- **Loops** — add new loop definitions by dropping `.json` files in `Loops/`.
- **Wrappers** — add new LLM backends by dropping `.json` files in `Wrappers/`. Each file defines the executable, argument template, and placeholders — zero code changes needed.