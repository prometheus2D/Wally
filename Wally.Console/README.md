# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and usage reference.

## Build

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

Output: `Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe`

## Quick Start

```sh
# Point Wally at your codebase root (WorkSource).
# .wally/ is created inside it automatically.
wally setup C:\repos\MyApp

# Target a specific actor
wally run Developer "Add input validation"
```

## Setting up with a custom path

The path you pass to `setup` is the **WorkSource** — the root directory whose files Copilot
will see as context. The `.wally/` workspace folder is always created inside it.

```sh
# Existing directory
wally setup C:\repos\MyApp
#  ? C:\repos\MyApp\.wally\  (workspace created inside)

# New directory that doesn't exist yet — created automatically
wally setup C:\repos\NewProject
#  ? creates C:\repos\NewProject\
#  ? creates C:\repos\NewProject\.wally\
#  ? scaffolds default config and actors

# Subdirectory — useful for monorepos or service-specific workspaces
wally setup C:\repos\MyApp\services\api
#  ? C:\repos\MyApp\services\api\.wally\

# No path — defaults to the exe directory
wally setup
#  ? <exeDir>\.wally\
```

> **Important:** Always pass the WorkSource directory, not the `.wally/` folder.
> Wally appends `.wally/` automatically. If the path doesn't exist, Wally creates it.

## Run Modes

**One-shot** — run a single command and exit:
```sh
wally setup C:\repos\MyApp
wally run Developer "Describe the main entry point"
```

**Interactive REPL** — environment persists across commands:
```sh
wally
wally> setup C:\repos\MyApp
wally> run Developer "Explain error handling in this codebase"
wally> run Tester "Suggest improvements"
wally> info
wally> exit
```

## Commands

```
setup [<path>]                   Scaffold .wally/ inside <path> (your WorkSource / codebase root).
                                 <path> is created if it doesn't exist.
                                 Defaults to the exe directory when omitted.
create <path>                    Scaffold a new .wally/ workspace inside <path> and load it.
                                 <path> is created if it doesn't exist.
load <path>                      Load an existing workspace from <path> (.wally/ folder).
save <path>                      Persist config and all actor.json files to <path>.
info                             Print paths, model config, loaded actors, settings.

list                             List all actors and their prompts.
reload-actors                    Re-read actor folders from disk and rebuild actors.

run <actor> "<prompt>" [-m <model>]
                                 Run a specific actor by name. -m overrides the model.
run-iterative "<prompt>" [--model <model>]
                                 Run all actors iteratively; -m N to cap iterations.
run-iterative "<prompt>" -a <actor> [--model <model>] [-m N]
                                 Run one named actor iteratively.

help                             Print this reference.
```

## WorkSource

The **WorkSource** is your codebase root directory. The `.wally/` workspace folder is
always created inside it. When Wally invokes `gh copilot`, the WorkSource is used as
both the working directory and the `--add-dir` target, so Copilot CLI sees your codebase.

```sh
wally setup C:\repos\MyApp
wally info
```

The workspace folder is always `<WorkSource>/.wally/`.

## Model Selection

Control which LLM model Copilot uses by editing `.wally/wally-config.json`:

```json
{
  "DefaultModel": "gpt-4.1",
  "Models": ["gpt-4.1", "claude-sonnet-4", "gpt-5.2"]
}
```

- `DefaultModel` — the model passed to `--model` for every actor. `"gpt-4.1"` by default (free tier). Null = Copilot picks.
- `Models` — reference list of available/allowed model identifiers.

Override per run with `-m`:
```sh
wally run Developer "Explain this module" -m claude-sonnet-4
wally run Developer "Explain this module" -m default   # uses DefaultModel from config
```

Run `gh copilot -- --help` and check the `--model` choices to see available models. Use `wally info` to verify config.

## Default workspace template

The `Default/` folder in this project ships alongside the exe and is the canonical workspace
template. When scaffolding a new workspace its entire contents are copied (no-overwrite) into
the target `.wally/` workspace folder.

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