# Wally Quickstart

## Setup

```bash
wally setup <path>          # Initialize workspace at <path>/.wally
wally setup --verify        # Verify existing workspace is intact
wally load <path>           # Load an existing workspace
```

## Run a Prompt

```bash
wally run "<prompt>"                        # Single-shot with defaults
wally run "<prompt>" -a Engineer            # Use a specific actor
wally run "<prompt>" -w Copilot             # Use a specific LLM wrapper
wally run "<prompt>" -l InvestigationLoop   # Use a specific loop
wally run "<prompt>" -a Engineer -l SingleRun --no-history
```

## Three-Loop Workflow

The default workflow for turning a question into working code:

```
1. InvestigationLoop   ? produces an approved Proposal
2. ProposalToTasks     ? decomposes Proposal into a Task Tracker
3. ExecuteTasksLoop    ? executes tasks one at a time from the tracker
```

### Step by step

```bash
# 1. Investigate and produce a proposal
wally run "How should we add caching to the API?" -l InvestigationLoop

# 2. Decompose the approved proposal into tasks
wally run "Projects/Proposals/CachingProposal.md" -a ProjectPlanner -l ProposalToTasks

# 3. Execute tasks from the tracker
wally run "Projects/Proposals/CachingProposalTasks.md" -a Engineer -l ExecuteTasksLoop
```

## Runbooks (Multi-Step Workflows)

```bash
wally runbook hello-world                       # Run a named runbook
wally runbook full-analysis "my question"        # Runbook with prompt
wally list-runbooks                              # See available runbooks
```

## Inspection

```bash
wally list                  # List actors
wally list-loops            # List loops
wally list-wrappers         # List LLM wrappers
wally list-runbooks         # List runbooks
wally info                  # Full workspace summary
wally commands              # Show all available commands
wally tutorial              # Interactive tutorial
```

## CRUD

```bash
# Actors
wally add-actor MyActor -r "role" -c "criteria" -i "intent"
wally edit-actor MyActor -r "new role"
wally delete-actor MyActor

# Loops
wally add-loop MyLoop -d "description" -a Engineer -s "{userPrompt}"
wally edit-loop MyLoop -d "updated"
wally delete-loop MyLoop

# Wrappers
wally add-wrapper MyWrapper -e gh -t "template" --can-make-changes
wally delete-wrapper MyWrapper

# Runbooks
wally add-runbook MyRunbook -d "description"
wally delete-runbook MyRunbook
```

## Mailbox

```bash
wally process-mailboxes     # Process all actor inboxes
wally route-outbox          # Deliver outbox messages to recipient inboxes
```

## Workspace Structure

```
<your-repo>/
    .wally/
        wally-config.json       # workspace settings
        Actors/                 # actor configs + mailbox folders
        Loops/                  # loop definitions (.json)
        Wrappers/               # LLM wrapper configs (.json)
        Runbooks/               # multi-step workflows (.wrb)
        Templates/              # document templates (.md)
        Projects/Proposals/     # proposals and task trackers
        Docs/                   # workspace-wide docs
        Logs/                   # session logs
```

## Key Concepts

| Concept | What It Is |
|---------|-----------|
| **Actor** | An AI persona with a role, criteria, allowed actions, and private docs |
| **Loop** | A JSON definition controlling iteration: single-shot, investigation, or task execution |
| **Wrapper** | An LLM backend config (Copilot, Claude, Codex, etc.) |
| **Runbook** | A `.wrb` file with one Wally command per line for multi-step workflows |
| **Proposal** | A design document produced by investigation, input to task decomposition |
| **Task Tracker** | The execution artifact: dependency-aware tasks with status and done-conditions |
