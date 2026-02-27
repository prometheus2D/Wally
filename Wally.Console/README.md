# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and actor authoring reference.

## Build

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

Output: `Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe`

## Run

```sh
# One-shot — scaffold next to the exe (default)
wally setup

# One-shot — scaffold at a specific path
wally setup --path C:\repos\MyApp\.wally

# Interactive REPL (environment persists across commands)
wally
```

## Commands

```
setup [-p <path>]             Scaffold .wally/ next to exe by default,
                              or at <path> when --path is supplied. Copies default actors.
create <path>                 Scaffold a new workspace at <path> and load it.
load <path>                   Load an existing workspace from <path>.
save <path>                   Persist config and all actor.json files to <path>.
info                          Print paths, loaded actors, reference counts, and settings.

list                          List all actors (with prompts), folder refs, and file refs.
reload-actors                 Re-read actor folders from disk and rebuild actors.

add-folder <path>             Register a folder for Copilot context.
add-file <path>               Register a file for Copilot context.
remove-folder <path>          Deregister a folder.
remove-file <path>            Deregister a file.
clear-refs                    Clear all registered folders and files.

run "<prompt>" [actor]        Run all actors, or one by name.
run-iterative "<prompt>"      Run all actors iteratively; -m N to cap iterations.
run-iterative "<prompt>" -a <actor>
                              Run one named actor iteratively; -m N to cap iterations.

help                          Print this reference.
```

## Iterative loop

Any actor can be run iteratively via `run-iterative -a <actor>`. On each iteration the
previous response is re-processed through the actor's full RBA context (Role,
AcceptanceCriteria, Intent, file/folder references) before the next `Act` call.
The loop stops early when the actor returns an empty response.

```sh
# Loop all actors (responses are combined and fed back each iteration)
wally run-iterative "Refactor the service layer to use async/await throughout"

# Loop a single actor up to 4 iterations
wally run-iterative "Add XML doc comments to all public methods" -a Developer -m 4

# Interactive — environment and workspace context persist across commands
wally
wally> setup --path C:\repos\MyApp\.wally
wally> add-folder C:\repos\MyApp\src
wally> run-iterative "Improve error handling" -a Developer -m 5
wally> exit
```

Each iteration is printed as it completes:

```
Running iterative loop on 'Developer' (max 5 iterations)...
--- Iteration 1 [Developer] ---
<response from actor>
--- Iteration 2 [Developer] ---
<response using previous output as context>
...
```

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

## Actor folders

Each actor lives in its own subfolder under `<workspace>/Actors/`:

```
.wally/
    wally-config.json
    Actors/
        Developer/
            actor.json
        Tester/
            actor.json
```

Add a subfolder with an `actor.json` to create a new actor. Edit the JSON to change its prompts.