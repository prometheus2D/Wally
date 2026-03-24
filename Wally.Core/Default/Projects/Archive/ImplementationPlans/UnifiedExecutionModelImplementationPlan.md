# Unified Execution Model — Implementation Plan

**Status**: ? Implemented
**Owner**: Lead Engineer
**Started**: 2025-07-17
**Completed**: 2025-07-17
**Archived**: 2025-07-17

---

## Summary

Replaced `HasRoleAction` hardcoding with data-driven dispatch reading `actor.Actions[]` from `actor.json` at runtime. Deleted dead `WallyPipeline` class.

**Source proposal**: [UnifiedExecutionModelProposal](../CompletedProposals/UnifiedExecutionModelProposal.md)

---

## Deliverables

| Step | File | Status |
|------|------|--------|
| 1 | CREATE `Wally.Core/Actors/ActionParameterDefinition.cs` | ? Complete |
| 2 | CREATE `Wally.Core/Actors/ActionDefinition.cs` | ? Complete |
| 3 | ADD `Actions` property to `Actor.cs` | ? Complete |
| 4a | MODIFY `WallyHelper.LoadActorFromDirectory` — deserialise `actions[]` | ? Complete |
| 4b | MODIFY `WallyHelper.SaveActor` — persist `actions[]` | ? Complete |
| 5 | MODIFY `ActionDispatcher` — remove `HasRoleAction`, add data-driven check + GlobMatch | ? Complete |
| 6 | DELETE `WallyLoop.cs` | ? Complete |
| 7 | FIX `WallyLoopDefinition.cs` doc comment | ? Complete |
