# Wally.Console

CLI entry point for Wally. See the [root README](../README.md) for full setup and command reference.

## Build

```sh
dotnet publish Wally.Console -c Release -r win-x64 --self-contained
```

Output: `Wally.Console\bin\Release\net8.0\win-x64\publish\wally.exe`

## Run

```sh
# One-shot
wally setup
wally load-actors .wally\default-agents.json
wally add-folder .\Project\src
wally run "Implement input validation"

# Interactive REPL (environment persists across commands)
wally
```

## Commands

```
setup                       Scaffold workspace next to exe and load it.
create <path>               Scaffold workspace at <path> and load it.
load <path>                 Load workspace from <path> (parent folder).
save <path>                 Save current config to <path>.
info                        Print workspace paths and loaded counts.
load-actors <path>          Load RBA actor definitions from JSON.
list                        List actors, folder refs, and file refs.
add-folder <path>           Register a folder for Copilot context.
add-file <path>             Register a file for Copilot context.
remove-folder <path>        Deregister a folder.
remove-file <path>          Deregister a file.
clear-refs                  Clear all registered folders and files.
run "<prompt>" [actor]      Run actors on prompt (all, or one by name).
run-iterative "<prompt>"    Run iteratively; -m N to cap iterations.
help                        Print this reference.