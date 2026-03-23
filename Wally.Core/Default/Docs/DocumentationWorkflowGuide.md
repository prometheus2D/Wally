# Documentation Workflow Guide

> Usage patterns and best practices for the Documentation Reflection system.

---

## Overview

The Documentation Reflection loop automates the tedious work of keeping project documentation current. It scans proposals, implementation plans, and architecture docs, detects staleness, and emits corrections — all using the existing agent loop and action infrastructure.

---

## Quick Start

### Run a Documentation Audit

```bash
wally run "Audit all project documentation" -l DocumentationReflection
```

This starts the `DocumentationReflection` agent loop which:
1. Scans `Projects/Proposals/` for status mismatches
2. Scans `Projects/ImplementationPlans/` for stale phase tracking
3. Checks `Docs/` for outdated architecture docs
4. Emits `write_document` actions to fix inconsistencies
5. Stops when it finds nothing left to change (`DOCUMENTATION_COMPLETE`)

### Run with a Specific Focus

```bash
wally run "Focus on mailbox-related documentation only" -l DocumentationReflection
```

The user prompt is appended to the loop's start prompt, so you can direct the actor's attention.

---

## What the Loop Does

### Iteration 1: Inventory & Audit
- Browses `Projects/Proposals/` and `Projects/ImplementationPlans/`
- Reads each document to check its declared `Status` field
- Identifies documents where the status doesn't match reality
- Makes corrections via `write_document` actions

### Iteration 2+: Reconciliation
- Reviews cross-document references (proposal ? implementation plan)
- Verifies that parent proposal status matches child phase completion
- Updates architecture docs with current implementation status
- Checks for completed proposals that should be archived

### Final Iteration: Convergence
- Reviews all changes made in previous iterations
- Confirms no further updates are needed
- Emits `DOCUMENTATION_COMPLETE` to stop the loop

---

## Loop Configuration

| Parameter | Value | Purpose |
|-----------|-------|---------|
| `actorName` | `BusinessAnalyst` | BA owns project status and documentation health |
| `maxIterations` | `5` | Hard cap prevents runaway loops |
| `stopKeyword` | `DOCUMENTATION_COMPLETE` | Actor signals convergence |
| `feedbackMode` | `AppendResponse` | Each iteration sees full history of previous changes |

---

## Cross-Actor Coordination

During a reflection cycle, the BusinessAnalyst may need technical accuracy from the Engineer:

```
Iteration 1 (BA): Scans docs, finds unclear implementation status
  ? Emits send_message to Engineer asking for clarification

Between iterations: Run mailbox cycle
  wally process-mailboxes    # Engineer reads and responds
  wally route-outbox         # Response delivered to BA's Inbox

Iteration 2 (BA): Reads Engineer's response, updates docs accordingly
```

For automated coordination, use a runbook:

```wrb
# Full documentation cycle with coordination
run "Audit project documentation" -l DocumentationReflection
process-mailboxes
route-outbox
run "Continue documentation audit with Engineer feedback" -l DocumentationReflection
```

---

## Best Practices

### Do

- **Run after major implementation milestones** — when phases complete, proposals get implemented
- **Review the output** — the loop updates documents but a human should verify the changes
- **Use focused prompts** — direct the audit to specific areas when you know what changed
- **Chain with mailbox** — let BA and Engineer coordinate for technical accuracy

### Don't

- **Don't run on every commit** — the loop is for periodic documentation hygiene, not CI
- **Don't expect auto-archival** — the loop identifies documents ready to archive but doesn't move files (file moves are destructive and should be human-confirmed)
- **Don't skip the human review** — LLM-generated status assessments should be sanity-checked

---

## Document Lifecycle

The reflection loop enforces this lifecycle:

```
Draft ? Under Review ? Approved ? In Progress ? Implemented ? Archived
```

| Status | Location | Meaning |
|--------|----------|---------|
| Draft | `Projects/Proposals/` | Being written or refined |
| Under Review | `Projects/Proposals/` | Complete, awaiting stakeholder review |
| Approved | `Projects/Proposals/` | Ready for implementation planning |
| In Progress | `Projects/Proposals/` or `Projects/ImplementationPlans/` | Implementation started |
| Implemented | `Projects/Proposals/` | Implementation complete, ready to archive |
| Archived | `Projects/Archive/CompletedProposals/` | Historical record |

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Loop hits MaxIterations without converging | Too many documents to audit in 5 iterations | Run again with a focused prompt, or increase `maxIterations` in the loop JSON |
| Actor doesn't detect stale documents | Actor can't read file contents without `read_context` | Ensure the workspace has `read_context` in the actor's abilities |
| `write_document` action fails | Path validation or permission issue | Check the actor's `pathPattern` allows the target path |
| Loop makes incorrect status assessments | LLM misinterprets implementation state | Review output, correct manually, re-run with clarifying prompt |

---

## Related Documentation

| Document | Relationship |
|----------|-------------|
| [DocumentationWorkflowProposal](../Projects/Proposals/DocumentationWorkflowProposal.md) | Source proposal |
| [AutonomousBotImplementationPlan](../Projects/ImplementationPlans/AutonomousBotImplementationPlan.md) | Parent implementation plan (Phase 4) |
| [MailboxSystemArchitecture](./MailboxSystemArchitecture.md) | Mailbox system used for cross-actor coordination |
| [ProposalTemplate](../Templates/ProposalTemplate.md) | Template for proposals audited by this loop |
| [ImplementationPlanTemplate](../Templates/ImplementationPlanTemplate.md) | Template for implementation plans generated by this loop |
