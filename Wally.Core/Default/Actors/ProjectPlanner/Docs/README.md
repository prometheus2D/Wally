# ProjectPlanner — Actor Reference

> Shared conventions are in `Actors/README.md`. This file covers ProjectPlanner-specific details only.

## Purpose

Reads a single approved proposal and produces one `<ProposalName>Tasks.md` tracker in the same directory.

## What It Does NOT Do

Write code, write new proposals, or invent scope not in the proposal.

## Usage

```
wally run "Projects/Proposals/MyFeatureProposal.md" -a ProjectPlanner -l ProposalToTasks
```

Output: `Projects/Proposals/MyFeatureProposalTasks.md`
