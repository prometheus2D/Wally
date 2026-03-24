# Unified Execution Model — Proposal

**Status**: ? Implemented
**Author**: Will Wright, Chris Sawyer, Markus Persson (consultants)
**Created**: 2025-07-17
**Last Updated**: 2025-07-17
**Archived**: 2025-07-17

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`ActionDispatcher.HasRoleAction` hardcoded actor names (`"Engineer"`, `"BusinessAnalyst"`, `"RequirementsExtractor"`) instead of reading the `actions[]` array already declared in each actor's `actor.json`. The `actor.json` files were declarations the code ignored. Additionally, `WallyPipeline` in `WallyLoop.cs` was dead code never instantiated.

---

## Resolution

- Replace `HasRoleAction` hardcoding with data-driven check reading `actor.Actions[]` at runtime
- Create `ActionDefinition` and `ActionParameterDefinition` model classes
- Load and save `actions[]` in `WallyHelper`
- Add path-pattern (`GlobMatch`) and required-parameter validation before dispatch
- Delete dead `WallyPipeline` class

**Scope note**: Part C (unified execution routing) permanently deferred — the runbook ? loop composition model already provides the needed composability.

---

## Implementation Summary

All deliverables complete:

| File | Change |
|------|--------|
| `Wally.Core/Actors/ActionParameterDefinition.cs` | ? Created — pure data class |
| `Wally.Core/Actors/ActionDefinition.cs` | ? Created — pure data class |
| `Wally.Core/Actors/Actor.cs` | ? Added `List<ActionDefinition> Actions` property |
| `Wally.Core/WallyHelper.cs` | ? `LoadActorFromDirectory` deserialises `actions[]`; `SaveActor` persists it |
| `Wally.Core/ActionDispatcher.cs` | ? `HasRoleAction` deleted; data-driven auth + GlobMatch validation added |
| `Wally.Core/WallyLoop.cs` | ? Deleted — `WallyPipeline` was dead code |
| `Wally.Core/WallyLoopDefinition.cs` | ? Stale `WallyPipeline` doc comment removed |
