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

# Run with a specific model
wally run Developer "Refactor the data layer" -m claude-sonnet-4

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

## Default Workspace Template

The `Default/` folder ships alongside the exe and is copied into new workspaces on `setup` or `create`:

```
Default/
    wally-config.json
    instructions.md
    Docs/
        README.md
    Actors/
        Developer/
            actor.json
            Docs/README.md
        Tester/
            actor.json
            Docs/README.md
```

Add new default actors by creating subfolders under `Default/Actors/` with an `actor.json`.

For full command reference, configuration, and architecture details, see the [root README](../README.md).