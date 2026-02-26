# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and agent authoring reference.

## Build

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

Output: `Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe`

## Run

```sh
# One-shot
wally setup
wally add-folder .\Project\src
wally run "Implement input validation"

# Interactive REPL (environment persists across commands)
wally
```

## Commands

```
setup                         Scaffold .wally/ + Project/ next to exe, copy default agents, load.
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
run-iterative "<prompt>"      Run agents iteratively; -m N to cap iterations.

help                          Print this reference.
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

Add a subfolder to create a new agent. Edit the `.txt` files to change its prompts. No JSON required.