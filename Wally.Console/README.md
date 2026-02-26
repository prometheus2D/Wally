# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and agent authoring reference.

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
wally setup --path C:\repos\MyApp

# Interactive REPL (environment persists across commands)
wally
```

## Commands

```
setup [-p <path>]             Scaffold .wally/ + Project/ next to exe by default,
                              or at <path> when --path is supplied. Copies default agents.
create <path>                 Scaffold a new workspace at <path> and load it.
load <path>                   Load an existing workspace from <path> (parent folder).
save <path>                   Persist config and all agent prompt files to <path>.
info                          Print paths, loaded agents, reference counts, and settings.

list                          List all agents (with prompts), folder refs, and file refs.
reload-agents                 Re-read agent folders from disk and rebuild actors.

add-folder <path>             Register a folder for Copilot context.
add-file <path>               Register a file for Copilot context.
remove-folder <path>          Deregister a folder.
remove-file <path>            Deregister a file.
clear-refs                    Clear all registered folders and files.

run "<prompt>" [agent]        Run all agents, or one by name.
run-iterative "<prompt>"      Run all agents iteratively; -m N to cap iterations.
run-iterative "<prompt>" -a <agent>
                              Run one named agent iteratively; -m N to cap iterations.

help                          Print this reference.
```

## Iterative loop

Any actor can be run iteratively via `run-iterative -a <agent>`. The loop is driven by
`ActorLoop` in `Wally.Core`, which is completely decoupled from the actor implementation:

```sh
# Loop all agents (responses are combined and fed back each iteration)
wally run-iterative "Refactor the service layer to use async/await throughout"

# Loop a single agent up to 4 iterations
wally run-iterative "Add XML doc comments to all public methods" -a Developer -m 4

# Interactive — environment and workspace context persist across commands
wally
wally> setup --path C:\repos\MyApp
wally> add-folder C:\repos\MyApp\Project\src
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

## Agent folders

Each agent lives in its own subfolder under `.wally/Agents/`:

```
.wally/
    Agents/
        Developer/
            role.txt       # Tier: task
            criteria.txt
            intent.txt
        Tester/
            role.txt       # Tier: task
            criteria.txt
            intent.txt
```

Add a subfolder to create a new agent. Edit the `.txt` files to change its prompts. No JSON required.Add a subfolder to create a new agent. Edit the `.txt` files to change its prompts. No JSON required.