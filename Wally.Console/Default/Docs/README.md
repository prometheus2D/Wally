# Workspace Documentation

Shared documentation that applies across all actors. Place files here that
any actor might need to reference (`.md`, `.txt`, `.rst`, `.adoc`).

All files are inside the WorkSource tree and accessible to `gh copilot`
via `--add-dir`. Nothing is injected into prompts — reference files by
path when needed:

> Refer to `.wally/Docs/some-file.md` for context.

## Workspace Layout

```
.wally/
    Docs/                      ? you are here (shared across all actors)
    Templates/                 ? document templates used by actors
    Actors/
        <ActorName>/
            actor.json
            Docs/              ? specific to that actor
```

## Templates

Document templates in `Templates/` define the structure for documents that
actors produce. See each template file for its purpose and format.

Run `wally list` to see which actors are loaded.
