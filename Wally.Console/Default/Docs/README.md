# Workspace Documentation

Place shared documentation files here (`.md`, `.txt`, `.rst`, `.adoc`).

These documents are injected into **every** actor's prompt as shared reference
context. Use them for:

- World-building rules and constraints
- Style guides and tone references
- Domain glossaries
- Project-wide specifications

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

Actor-level docs are only injected into that actor's prompts.
