# Wally

**Role-Based Actor (RBA) framework that wraps GitHub Copilot CLI into a structured, prompt-driven AI environment.**

Wally scaffolds a `.wally/` workspace inside any codebase, loads actors defined by JSON files, wraps every prompt with RBA context (Role, AcceptanceCriteria, Intent), and forwards it to `gh copilot`.

---

## Prerequisites

| Tool | Install |
|---|---|
| .NET 8 SDK | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| GitHub CLI | [cli.github.com](https://cli.github.com) |
| Copilot extension | `gh extension install github/gh-copilot` |
| Auth | `gh auth login` (Copilot access required) |

## Build

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

---

## Quick Start — 3 Steps

### 1. Set up a workspace

Point Wally at the root of your codebase. A `.wally/` folder is created inside it
with config, actors, templates, and docs:

```sh
wally setup C:\repos\MyApp
```

> If you omit the path, Wally uses the directory where the exe lives.
> If the directory doesn't exist, it's created automatically.
> Running `setup` again on an existing workspace repairs any missing structure.

### 2. Run an actor

Each actor wraps your prompt with its Role, AcceptanceCriteria, and Intent
before forwarding to `gh copilot`:

```sh
# Have the Engineer review code and produce documentation
wally run Engineer "Review the authentication module. Identify any bugs, document the architecture, and propose improvements."

# Have the BusinessAnalyst write requirements
wally run BusinessAnalyst "Write requirements for the user search feature"

# Have the Stakeholder define success criteria
wally run Stakeholder "Define what the dashboard must achieve from a business perspective"
```

### 3. Check what happened

```sh
wally info       # workspace paths, loaded actors, session log location
wally list       # actor names and their RBA prompts
```

That's it. Every prompt is logged under `.wally/Logs/` with full context.

---

## Interactive Mode

Run `wally` with no arguments to enter a REPL:

```
wally
wally> setup C:\repos\MyApp
wally> run Engineer "Explain the main entry point"
wally> run BusinessAnalyst "Write requirements for the login feature"
wally> run Stakeholder "Define success criteria for the dashboard"
wally> run-loop Engineer "Refactor error handling across the project" -n 5
wally> info
wally> exit
```

---

## Commands

### No workspace required

| Command | Description |
|---|---|
| `setup [<path>]` | Scaffold and load a workspace. `<path>` is your codebase root — `.wally/` is created inside it. Defaults to the exe directory. Re-running repairs missing structure. |
| `setup [<path>] --verify` | Check workspace structure without making changes. |
| `load <path>` | Load an existing `.wally/` workspace folder. |
| `info` | Print workspace paths, model config, loaded actors, and session info. |
| `commands` | Print command reference. |

### Workspace required

| Command | Description |
|---|---|
| `save <path>` | Persist config and all actor files to disk. |
| `list` | List all actors and their RBA prompts. |
| `reload-actors` | Re-read actor folders from disk. |
| `run <actor> "<prompt>" [-m <model>]` | Run a single actor. `-m` overrides the model. |
| `run-loop <actor> "<prompt>" [-m <model>] [-n <max>]` | Run an actor in an iterative loop. Ends on `[LOOP COMPLETED]`, `[LOOP ERROR]`, or max iterations. |

---

## Default Actors

The shipped workspace template includes three actors. Add, remove, or customise
actors by editing the `.wally/Actors/` folder:

| Actor | Perspective | Produces | Example |
|---|---|---|---|
| **Stakeholder** | Business — defines needs, priorities, success criteria | Business context, priorities, acceptance feedback | `run Stakeholder "Define what the payment system must achieve"` |
| **BusinessAnalyst** | Bridge — translates needs into requirements, manages scope | Requirements docs, Execution Plans | `run BusinessAnalyst "Write requirements for the search feature"` |
| **Engineer** | Technical — designs, builds, tests, documents | Proposals, Implementation Plans, Architecture docs, Bug Reports, Test Plans, code reviews | `run Engineer "Review the data access layer and document the architecture"` |

Each actor references document templates in `.wally/Templates/` when producing
structured output.

---

## Workspace Layout

```
<WorkSource>/                      Your codebase root (e.g. C:\repos\MyApp)
  .wally/                          Workspace folder (created by setup)
    wally-config.json              Configuration
    Docs/                          Shared documentation (all actors)
    Templates/                     Document templates
        RequirementsTemplate.md
        ExecutionPlanTemplate.md
        ProposalTemplate.md
        ImplementationPlanTemplate.md
        ArchitectureTemplate.md
        BugTemplate.md
        TestPlanTemplate.md
    Actors/
      Stakeholder/
        actor.json                 Actor definition (RBA prompts)
        Docs/                      Actor-specific documentation
      BusinessAnalyst/
        actor.json
        Docs/
      Engineer/
        actor.json
        Docs/
    Logs/                          Session logs (auto-created on first run)
      <timestamp_guid>/
        <timestamp>.txt
```

Everything under `<WorkSource>/` (including `.wally/`) is granted to
`gh copilot` via `--add-dir`, so Copilot can read your code, docs, and
templates natively.

---

## Document Templates

Templates define the expected structure for documents that actors produce.
Actors reference them by name in their `criteriaPrompt`:

| Template | Used by | Purpose |
|---|---|---|
| `RequirementsTemplate.md` | BusinessAnalyst | What the system must do — stakeholder needs, acceptance criteria |
| `ExecutionPlanTemplate.md` | BusinessAnalyst | Coordinate delivery across implementation plans |
| `ProposalTemplate.md` | Engineer | Introduce new ideas with phases, impact, and risks |
| `ImplementationPlanTemplate.md` | Engineer | Break proposals into concrete, executable steps |
| `ArchitectureTemplate.md` | Engineer | Capture system design decisions and patterns |
| `BugTemplate.md` | Engineer | Track defects with symptoms, investigation, and resolution |
| `TestPlanTemplate.md` | Engineer | Define how requirements will be verified |

Edit or add templates in `.wally/Templates/` to fit your workflow.

---

## Actors — `actor.json`

Each actor lives in its own subfolder under `.wally/Actors/` with an `actor.json`:

```json
{
  "name": "Engineer",
  "rolePrompt": "Act as a senior software engineer responsible for all technical work...",
  "criteriaPrompt": "Output must be technically precise, trace back to a requirement...",
  "intentPrompt": "Design and build the system to meet requirements...",
  "docsFolderName": "Docs"
}
```

| Field | RBA Component | Purpose |
|---|---|---|
| `rolePrompt` | Role | The persona the AI adopts |
| `criteriaPrompt` | AcceptanceCriteria | Success criteria the output must meet |
| `intentPrompt` | Intent | The goal the actor pursues |
| `docsFolderName` | — | Subfolder name for actor-private docs (default: `Docs`) |

### How prompts are built

When you run `wally run Engineer "Add input validation"`, Wally wraps your prompt:

```
# Actor: Engineer
## Role
Act as a senior software engineer...
## Acceptance Criteria
Output must be technically precise...
## Intent
Design and build the system to meet requirements...

## Documentation Context
(lists doc files the actor can reference)

## Prompt
Add input validation
```

This enriched prompt is passed to `gh copilot -p` with `--add-dir` pointing to
your codebase root, so Copilot has full file access.

### Creating custom actors

1. Create a folder: `.wally/Actors/SecurityReviewer/`
2. Add an `actor.json`:
   ```json
   {
     "name": "SecurityReviewer",
     "rolePrompt": "Act as a security engineer focused on threat modeling and vulnerability analysis.",
     "criteriaPrompt": "Identify all security vulnerabilities, rank by severity, provide remediation steps.",
     "intentPrompt": "Review the system for security risks and produce actionable findings.",
     "docsFolderName": "Docs"
   }
   ```
3. Run `wally reload-actors` then `wally run SecurityReviewer "Review the auth module"`

---

## Configuration — `wally-config.json`

```json
{
  "ActorsFolderName": "Actors",
  "LogsFolderName": "Logs",
  "DocsFolderName": "Docs",
  "TemplatesFolderName": "Templates",
  "LogRotationMinutes": 2,
  "DefaultModel": "gpt-4.1",
  "Models": ["gpt-4.1", "claude-sonnet-4", "gpt-5.2"],
  "MaxIterations": 10
}
```

| Property | Default | Description |
|---|---|---|
| `DefaultModel` | `"gpt-4.1"` | Model passed to `gh copilot --model`. Null uses Copilot's default. |
| `Models` | `[...]` | Reference list of available model identifiers. |
| `MaxIterations` | `10` | Default iteration cap for `run-loop`. |
| `ActorsFolderName` | `"Actors"` | Subfolder holding actor directories. |
| `DocsFolderName` | `"Docs"` | Workspace-level documentation directory. |
| `TemplatesFolderName` | `"Templates"` | Document templates directory. |
| `LogsFolderName` | `"Logs"` | Session log directory. |
| `LogRotationMinutes` | `2` | Minutes per log file. `0` disables rotation. |

Override the model per-run: `wally run Engineer "Explain this" -m claude-sonnet-4`

---

## How It Works

### Single run (`run`)

```
Your prompt
  ? Actor.ProcessPrompt() wraps it with Role + AcceptanceCriteria + Intent
  ? gh copilot --model <m> --add-dir <WorkSource> -s -p "<structured prompt>"
  ? Response printed to console
```

### Iterative loop (`run-loop`)

```
Your prompt
  ? Iteration 1: actor.Act(prompt)
  ? Iteration 2+: actor.Act(continuePrompt with previous response embedded)
  ? Stops on [LOOP COMPLETED], [LOOP ERROR], or max iterations
```

Each `gh copilot -p` call is stateless — the loop carries context forward
explicitly in the prompt.

---

## Logging

Every command, prompt, and response is logged per session under `.wally/Logs/`.
Log files rotate every `LogRotationMinutes` (default: 2 min).

Use `wally info` to see the current session log path.

---

## Projects

| Project | Purpose |
|---|---|
| `Wally.Core` | Domain library — Actor, WallyLoop, WallyWorkspace, WallyEnvironment, WallyConfig, RBA types, session logging. |
| `Wally.Console` | CLI entry point — verb-based commands, interactive REPL, ships default workspace template. |
| `Wally.Forms` | Windows Forms UI (in progress). |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `'gh' is not recognized` | Install [GitHub CLI](https://cli.github.com) and add to PATH. |
| `gh copilot: command not found` | `gh extension install github/gh-copilot` |
| `HTTP 401` | `gh auth login` — ensure Copilot access. |
| Empty responses | `wally info` — verify WorkSource points to a directory with code. |
| Model not available | `gh copilot -- --help` — check `--model` choices. |
| Actor not found | `wally list` to see loaded actors. `wally reload-actors` after adding new ones. |
| Missing workspace structure | `wally setup <path>` repairs any missing folders or files. |

---

## License

MIT
