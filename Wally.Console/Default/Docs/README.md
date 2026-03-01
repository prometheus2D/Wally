# Workspace Documentation

Place shared documentation files here (`.md`, `.txt`, `.rst`, `.adoc`).

These files are inside the WorkSource tree, so `gh copilot` can already read
them natively via `--add-dir`. Nothing is injected into prompts.

To direct Copilot to a specific file, reference it by path in your prompt:

> Refer to `.wally/Docs/style-guide.md` for tone and formatting rules.

Use this folder for:

- Style guides and tone references
- Domain glossaries
- Project-wide specifications
- World-building rules and constraints

## Per-Actor Documentation

Each actor can also have its own private `Docs/` folder:

```
.wally/
    Docs/                      ? you are here (shared across all actors)
        style-guide.md
    Actors/
        Narrator/
            actor.json
            Docs/              ? private to Narrator
                chapter-outline.md
                character-guide.md
        Editor/
            actor.json
            Docs/              ? private to Editor
                tone-guide.md
```

All files are accessible to Copilot via `--add-dir` — just reference them
by path in your prompt when needed.
