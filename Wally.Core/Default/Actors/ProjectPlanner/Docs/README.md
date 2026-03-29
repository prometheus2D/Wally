# ProjectPlanner Actor

The **ProjectPlanner** actor reads a single proposal document and produces two outputs:

1. **Task Breakdown** � every deliverable in the proposal decomposed into granular, independently executable tasks with priorities, effort estimates, and done-conditions.
2. **Todo Tracker** � a `<ProposalName>Tasks.md` file written next to the proposal, ready for a team or AI agent to pick up and start executing immediately.

---

## When to use

Run this actor (via the `ProposalToTasks` loop) immediately after a proposal is approved. The output tracker becomes the living work queue for that proposal's implementation.

## What it does NOT do

- Write code
- Write new proposals
- Write architecture, requirements, or plan-based intermediate documents
- Invent scope not present in the proposal

## Output convention

The tracker is always written to the same directory as the source proposal:

```
Projects/Proposals/MyFeatureProposal.md        ? input
Projects/Proposals/MyFeatureProposalTasks.md   ? output
```

## Loop

Use the `ProposalToTasks` loop to drive this actor. Pass the relative path to the proposal as the prompt:

```
run ProjectPlanner --loop ProposalToTasks "Projects/Proposals/MyFeatureProposal.md"
```
