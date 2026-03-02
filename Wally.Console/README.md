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
# Point Wally at your codebase root — .wally/ is created inside it
wally setup C:\repos\MyApp

# Run actors
wally run Engineer "Review the authentication module and document the architecture"
wally run BusinessAnalyst "Write requirements for the search feature"
wally run Stakeholder "Define success criteria for the dashboard"

# Run an actor in a loop
wally run-loop Engineer "Refactor error handling across the project" -n 5

# Override the model for a single run
wally run Engineer "Explain the data layer" -m claude-sonnet-4
```

## Run Modes

**One-shot** — single command, then exit:

```sh
wally setup C:\repos\MyApp
wally run Engineer "Describe the main entry point"
```

**Interactive REPL** — run `wally` with no arguments:

```
wally
wally> setup C:\repos\MyApp
wally> run Engineer "Explain error handling"
wally> run BusinessAnalyst "Write requirements for the login feature"
wally> run Stakeholder "Define success criteria for the dashboard"
wally> info
wally> exit
```

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
```

Add new default actors by creating subfolders under `Default/Actors/` with an `actor.json`.

For full command reference, configuration, and architecture details, see the [root README](../README.md).