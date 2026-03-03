# Workspace Documentation

Shared documentation that applies across all actors. Place files here that
any actor might need to reference (`.md`, `.txt`, `.rst`, `.adoc`).

All files are inside the WorkSource tree and accessible to the LLM provider
(e.g. `gh copilot` via `--add-dir`). Doc file names are listed in the
enriched prompt so the LLM knows they exist and can consult them when
relevant to the task.

> Refer to `.wally/Docs/some-file.md` for context.

## Workspace Layout

```
.wally/
    wally-config.json          ? workspace configuration
    Docs/                      ? you are here (shared across all actors)
    Templates/                 ? document templates used by actors
    Actors/
        <ActorName>/
            actor.json
            Docs/              ? specific to that actor
    Loops/                     ? loop definitions (JSON) for iterative runs
    Wrappers/                  ? LLM wrapper definitions (JSON)
    Logs/                      ? session logs (auto-created)
```

## Templates

Document templates in `Templates/` define the structure for documents that
actors produce. See each template file for its purpose and format.

## Loops

Loop definitions in `Loops/` define reusable iterative execution patterns.
Each `.json` file specifies the actor, prompts, stop keywords, and iteration
limits. Run `wally list-loops` to see available loop definitions.

## Wrappers

LLM wrapper definitions in `Wrappers/` define how Wally calls external LLM
tools. Each `.json` file specifies the executable, argument template, and
behavioural flags. To add a new LLM backend, drop a `.json` file here —
no code changes needed.

Run `wally list` to see which actors are loaded.
Run `wally info` to see the active wrapper and model configuration.
