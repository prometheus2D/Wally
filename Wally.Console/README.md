# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and usage reference.

## Build

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

Output: `Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe`

## Quick Start

```sh
# Scaffold workspace and point SourcePath at your codebase
wally setup -s C:\repos\MyApp

# Target a specific actor
wally run Developer "Add input validation"
```

## Run Modes

**One-shot** — run a single command and exit:
```sh
wally setup
wally run Developer "Describe the main entry point"
```

**Interactive REPL** — environment persists across commands:
```sh
wally
wally> setup -s C:\repos\MyApp
wally> run Developer "Explain error handling in this codebase"
wally> run Tester "Suggest improvements"
wally> info
wally> exit
```

## Commands

```
setup [-p <path>] [-s <source>]  Scaffold .wally/ next to exe by default,
                                 or at <path> when --path is supplied.
                                 -s sets the source directory for Copilot file context.
create <path>                    Scaffold a new workspace at <path> and load it.
load <path>                      Load an existing workspace from <path>.
save <path>                      Persist config and all actor.json files to <path>.
info                             Print paths, model config, loaded actors, settings.

list                             List all actors and their prompts.
reload-actors                    Re-read actor folders from disk and rebuild actors.

run <actor> "<prompt>"           Run a specific actor by name.
run-iterative "<prompt>"         Run all actors iteratively; -m N to cap iterations.
run-iterative "<prompt>" -a <actor> [-m N]
                                 Run one named actor iteratively.

help                             Print this reference.
```

## SourcePath

Controls which directory `gh copilot` uses for file context. Set during setup:

```sh
wally setup -s C:\repos\MyApp
```

Or edit `.wally/wally-config.json`:
```json
{
  "SourcePath": "C:\\repos\\MyApp"
}
```

When null, defaults to the workspace's parent folder.

## Model Selection

Control which LLM model Copilot uses by editing `.wally/wally-config.json`:

```json
{
  "DefaultModel": "gpt-4o",
  "Models": ["gpt-4o", "claude-3.5-sonnet", "o4-mini"]
}
```

- `DefaultModel` — the model passed to `--model` for every actor. Null = Copilot picks.
- `Models` — reference list of available/allowed model identifiers.

Run `gh copilot model list` to see available models. Use `wally info` to verify config.

## Default workspace template

The `Default/` folder in this project ships alongside the exe and is the canonical workspace
template. When scaffolding a new workspace its entire contents are copied (no-overwrite) into
the target workspace folder.

```
Default/
    wally-config.json
    Actors/
        Developer/
            actor.json          name, rolePrompt, criteriaPrompt, intentPrompt
        Tester/
            actor.json
```

To add a new default actor, create a subfolder under `Default/Actors/` with an `actor.json`.
The glob `<Content Include="Default\**\*">` in the `.csproj` ensures it is automatically
copied to the output directory on build.