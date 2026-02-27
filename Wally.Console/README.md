# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full reference.

## Build

```sh
dotnet build
# Or publish a self-contained exe:
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

## Quick Start

```sh
# Point Wally at your codebase root
wally setup C:\repos\MyApp

# Run a single actor
wally run Developer "Add input validation"

# Run an actor in a loop
wally run-loop Developer "Refactor error handling" -n 5
```

## Run Modes

**One-shot** — single command, then exit:

```sh
wally run Developer "Describe the main entry point"
```

**Interactive REPL** — run `wally` with no arguments:

```sh
wally
wally> setup C:\repos\MyApp
wally> run Developer "Explain error handling"
wally> run-loop Tester "Find edge cases"
wally> info
wally> exit
```

## Commands

| Command | Description |
|---|---|
| `setup [<path>]` | Scaffold `.wally/` inside `<path>`. Defaults to exe directory. |
| `create <path>` | Scaffold and load a new workspace. |
| `load <path>` | Load an existing `.wally/` folder. |
| `save <path>` | Persist config and actor files. |
| `info` | Print workspace paths, model config, actors. |
| `list` | List all actors and their prompts. |
| `reload-actors` | Re-read actor folders from disk. |
| `run <actor> "<prompt>" [-m <model>]` | Run a single actor. |
| `run-loop <actor> "<prompt>" [-m <model>] [-n <max>]` | Run an actor in a loop until `LOOP COMPLETED` or max iterations. |
| `help` | Print command reference. |

## Model Selection

Set `DefaultModel` in `.wally/wally-config.json`, or override per run with `-m`:

```sh
wally run Developer "Explain this" -m claude-sonnet-4
```

## Default Workspace Template

The `Default/` folder ships alongside the exe and is copied into new workspaces on `setup` or `create`:

```
Default/
    wally-config.json
    Actors/
        Developer/actor.json
        Tester/actor.json
```

Add new default actors by creating subfolders under `Default/Actors/` with an `actor.json`.