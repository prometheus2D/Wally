# Executable Loop Steps Proposal

**Status**: Draft
**Author**: System Architecture Team
**Created**: 2026-03-28
**Last Updated**: 2026-03-28

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

---

## Problem Statement

`WallyStepDefinition` currently models only AI-prompt steps: an actor name plus a prompt template. That is sufficient for fixed AI pipelines, but it is not sufficient for real workflows that must also execute system behavior.

Current gap:

- a loop step cannot directly run built-in runtime code
- mailbox movement currently implies a separate router command or service
- investigation workflows need non-AI steps such as moving messages, updating state, running commands, and invoking shared runtime helpers
- putting these behaviors into prompt text is fragile and opaque

Wally needs loop steps that can execute code-backed runtime handlers while remaining declarative in loop definitions.

These executable steps are intended to support all three default loops: investigation, proposal-to-tasks, and task execution.

---

## Resolution

Introduce executable loop steps: typed loop steps that dispatch to built-in runtime handlers instead of only constructing an AI prompt.

This does **not** mean arbitrary inline C# or free-form code blobs in JSON. It means a step definition can declare a supported execution kind, and the loop runtime executes a known handler.

Prompt construction remains template-driven. Each named step definition owns its own custom prompt text, any declared document inputs, and any referenced workflow abilities. The loop routes between step names; it does not hide prompt-building rules in runtime code.

Step customization and ability reuse must both be first-class. A step may be authored entirely with step-local configuration, or it may keep that step-local configuration and additionally reference one or more reusable abilities through `abilityRefs`. The presence of an ability reference must never take ownership of the step away from the step definition itself.

For the first loop implementations, prompt execution should be actor-agnostic. No actor role, criteria, or intent prompt should be injected unless a later loop explicitly opts into actor-backed execution.

Recommended v1 step kinds:

| Step Kind | Purpose |
|-----------|---------|
| `prompt` | Execute a step-owned prompt in direct mode with no actor injection |
| `shell` | Run an OS process in WorkSource |
| `command` | Run a Wally command through the existing dispatcher |
| `code` | Run a built-in runtime handler selected by name |
| `user_input` | Pause the loop and request input from the user through console or UI |

The guiding rule is: use typed handlers for reusable runtime behavior, not arbitrary script injection.

Steps must support both customization and ability references without forcing a single authoring style:

- custom-only step: omit `abilityRefs`; the step owns its prompt text, routing, and execution settings directly
- ability-augmented step: include `abilityRefs`; the step still owns its kind, routing, document inputs, and any step-specific prompt text
- mixed prompt step: use `abilityRefs` plus a step-specific `promptTemplate` when reusable ability guidance needs local instructions or constraints

Abilities augment a step; they do not replace the step's kind, document inputs, keyword routes, handler settings, or other step-local configuration.

Recommended v1 code handlers:

| Handler | Purpose |
|---------|---------|
| `route_messages` | Move mailbox items from `Outbox/` to their next valid location |
| `record_user_response` | Persist a user answer into canonical docs and loop state |
| `sync_investigation_state` | Normalize or reconcile investigation docs when needed |

---

## Keyword-Driven Step Selection

**This is a fundamental design requirement.** Each step must be able to produce a keyword in its output that determines which step the loop should execute next. This applies to all step kinds:

- **`prompt` steps**: The LLM response contains a keyword (e.g. `NEED_USER_INPUT`, `INVESTIGATE_MORE`, `DRAFT_READY`) that selects the next step.
- **`user_input` steps**: After the user's answer is persisted, a keyword or default routing determines what runs next.
- **`code` steps**: The handler returns a result that may include a keyword for routing.
- **`shell` and `command` steps**: The exit code or output determines the next step.

The loop definition JSON declares which keywords map to which next steps. This makes any JSON loop definition a dynamic, branching workflow. The keyword-driven model is the standard mechanism for step-to-step routing across all Wally loops � it is not specific to any single loop profile.

A WallyStep is therefore one of three things:
1. A prompt to an LLM (with keyword output determining the next step)
2. A request for user input (the loop pauses and resumes after the answer is persisted)
3. A programmatic operation (a built-in handler that runs code)

---

## Why A Separate Router Service Is Not Required

The system does not need an always-on mailbox router service to work.

What it needs is deterministic mailbox movement at the right time. The cleanest place for that is a workflow-owned executable step.

Example:

- investigation loop writes a question to `Outbox/`
- the loop later runs `route_messages`
- the handler moves messages to the next location and records results
- the investigation continues using the updated folder state and docs

This keeps ownership clear:

- workflow decides **when** routing happens
- runtime handler decides **how** routing happens
- documentation records **what** happened

---

## Proposed Step Model

The current `WallyStepDefinition` should be extended so that a step can declare its execution mode, own its prompt template, and participate in keyword-driven routing.

Suggested fields:

| Field | Meaning |
|------|---------|
| `Name` | Step identifier |
| `Description` | Human-readable purpose |
| `Kind` | `prompt`, `shell`, `command`, `code`, `user_input` |
| `ActorName` | Optional actor for future actor-backed steps; omitted in v1 loop examples |
| `PromptTemplate` | Prompt template for `prompt` steps |
| `AbilityRefs` | Optional ordered list of reusable Wally abilities that augment the step's own configuration |
| `DocumentInputs` | Named docs or state inputs the prompt template expects |
| `CommandTemplate` | Template for `shell` or `command` steps |
| `HandlerName` | Built-in handler name for `code` steps |
| `Arguments` | Named arguments passed to the handler |
| `ContinueOnFailure` | Whether the workflow may continue after failure |
| `WritesToDocs` | Expected document outputs |
| `KeywordRoutes` | Map of output keywords to next step names (for keyword-driven routing) |
| `DefaultNextStep` | Step to execute when no keyword matches (fallback routing) |

Execution routing:

- `prompt` -> resolve referenced abilities, build `{AbilityBlocks}`, resolve the step's template inputs from docs, expand `PromptTemplate`, execute direct prompt path, scan response for keywords
- `shell` -> shared shell runner; exit code / output scanned for keywords
- `command` -> existing `DispatchCommand`; output scanned for keywords
- `code` -> built-in handler registry; handler returns result with optional keyword
- `user_input` -> pause loop, persist interaction request, resume after answer persisted

Prompt-step validation rules:

- `promptTemplate` remains step-owned and customizable even when `abilityRefs` is present
- `abilityRefs` is optional; omitting it must remain a valid first-class authoring path
- if `abilityRefs` is present, the runtime resolves those abilities and injects them into the step prompt without replacing step-local text
- if a prompt step references `abilityRefs` and omits `{AbilityBlocks}`, the runtime should prepend the resolved ability guidance so the reference is not silently ignored

Ownership model:

- step definition owns: custom text, prompt template, ability refs, document inputs, execution kind, and expected outputs
- loop definition owns: which step names participate in the workflow and how keyword routing moves between them
- docs own: mailbox state, memory state, findings, questions, ability documents, and other persisted inputs consumed by the step template

Recommended v1 storage model:

- keep named step definitions inside the loop JSON `Steps` collection
- require each step to have a stable `Name`
- `KeywordRoutes` and `DefaultNextStep` refer to step names in that same loop definition
- do not introduce a separate global step registry in v1

Canonical JSON conventions for v1:

- use `camelCase` property names in proposal docs and shipped examples
- loading may remain case-insensitive for backward compatibility
- step `name` values are stable identifiers and route targets, not display-only labels
- the field table below describes the logical model using C#-style names; JSON examples show the canonical wire format using `camelCase`

### JSON Shape

Recommended common JSON shape for all named steps:

```json
{
	"name": "assessState",
	"description": "Read canonical docs and decide what should happen next.",
	"kind": "prompt",
	"abilityRefs": ["investigation-assessment"],
	"promptTemplate": "...{AbilityBlocks}...",
	"documentInputs": [
		{ "key": "InvestigationBrief", "path": "Actors/Investigator/Docs/InvestigationBrief.md", "required": true }
	],
	"commandTemplate": null,
	"handlerName": null,
	"arguments": {},
	"continueOnFailure": false,
	"writesToDocs": ["Actors/Investigator/Docs/InvestigationLog.md"],
	"keywordRoutes": {
		"NEED_USER_INPUT": "requestUserInput",
		"GATHER_CONTEXT": "gatherContext"
	},
	"defaultNextStep": "gatherContext"
}
```

Recommended `documentInputs` shape:

```json
{
	"key": "OpenQuestions",
	"path": "Actors/Investigator/Docs/OpenQuestions.md",
	"required": false
}
```

Rules:

- `key` is the placeholder name used by the prompt template, for example `{OpenQuestions}`
- `path` is workspace-relative
- `required` determines whether missing input should fail the step or inject empty content
- if a step does not declare a document input, the runtime should not inject it implicitly

Recommended `abilityRefs` behavior:

- `abilityRefs` contains stable ability names, for example `investigation-assessment` or `mailbox-routing`
- referenced abilities are resolved in order
- the runtime combines the referenced ability guidance into `{AbilityBlocks}`
- the step still owns the final `promptTemplate` and all routing behavior
- `abilityRefs` augments step behavior; it does not replace step-local customization
- actor-owned abilities remain a separate authorization concept and should not be conflated with loop `abilityRefs`

### Kind-Specific Examples

Prompt step:

```json
{
	"name": "generateIdeas",
	"description": "Generate alternatives based on findings and open questions.",
	"kind": "prompt",
	"abilityRefs": ["alternative-generation"],
	"promptTemplate": "Use the current findings and open questions to generate candidate approaches.\n\nAbility Guidance:\n{AbilityBlocks}\n\nFindings:\n{Findings}\n\nOpen Questions:\n{OpenQuestions}",
	"documentInputs": [
		{ "key": "Findings", "path": "Actors/Investigator/Docs/Findings.md", "required": false },
		{ "key": "OpenQuestions", "path": "Actors/Investigator/Docs/OpenQuestions.md", "required": false }
	],
	"writesToDocs": ["Actors/Investigator/Docs/Ideas.md"],
	"keywordRoutes": {
		"ASK_USER": "requestUserInput",
		"DRAFT_PROPOSAL": "draftProposal"
	},
	"defaultNextStep": "draftProposal"
}
```

Custom-only prompt step:

```json
{
	"name": "draftProposal",
	"description": "Turn the current findings into a proposal draft.",
	"kind": "prompt",
	"promptTemplate": "Draft or revise the working proposal using the current brief, findings, ideas, and open questions. Keep the structure aligned with ProposalTemplate.md.\n\nBrief:\n{InvestigationBrief}\n\nFindings:\n{Findings}\n\nIdeas:\n{Ideas}\n\nOpen Questions:\n{OpenQuestions}",
	"documentInputs": [
		{ "key": "InvestigationBrief", "path": "Actors/Investigator/Docs/InvestigationBrief.md", "required": true },
		{ "key": "Findings", "path": "Actors/Investigator/Docs/Findings.md", "required": false },
		{ "key": "Ideas", "path": "Actors/Investigator/Docs/Ideas.md", "required": false },
		{ "key": "OpenQuestions", "path": "Actors/Investigator/Docs/OpenQuestions.md", "required": false }
	],
	"writesToDocs": ["Actors/Investigator/Memory/WorkingProposal.md"],
	"keywordRoutes": {
		"NEED_USER_INPUT": "requestUserInput",
		"DRAFT_UPDATED": "assessState"
	},
	"defaultNextStep": "assessState"
}
```

Code step:

```json
{
	"name": "routeMessages",
	"description": "Move outbound mailbox items to their next destination.",
	"kind": "code",
	"handlerName": "route_messages",
	"arguments": {
		"sourceFolder": "Actors/Investigator/Outbox",
		"logPath": "Actors/Investigator/Docs/InvestigationLog.md"
	},
	"writesToDocs": ["Actors/Investigator/Docs/InvestigationLog.md"],
	"keywordRoutes": {
		"ROUTED": "reviewInbox",
		"NO_MESSAGES": "assessState"
	},
	"defaultNextStep": "assessState"
}
```

User-input step:

```json
{
	"name": "requestUserInput",
	"description": "Persist a question batch and pause the loop.",
	"kind": "user_input",
	"promptTemplate": "Convert the current open questions into a user-facing question batch.\n\nOpen Questions:\n{OpenQuestions}",
	"documentInputs": [
		{ "key": "OpenQuestions", "path": "Actors/Investigator/Docs/OpenQuestions.md", "required": true }
	],
	"writesToDocs": [
		"Actors/Investigator/Docs/InteractionState.md",
		"Actors/Investigator/Docs/UserResponses.md"
	],
	"keywordRoutes": {
		"WAITING_FOR_USER": "recordUserInput"
	},
	"defaultNextStep": "recordUserInput"
}
```

Shell step:

```json
{
	"name": "gatherContext",
	"description": "Run a repository inspection command and summarize the result.",
	"kind": "shell",
	"commandTemplate": "rg --files {WorkSource}",
	"writesToDocs": ["Actors/Investigator/Docs/Findings.md"],
	"keywordRoutes": {
		"CONTEXT_CAPTURED": "assessState"
	},
	"defaultNextStep": "assessState"
}
```

Command step:

```json
{
	"name": "runMailboxCommand",
	"description": "Invoke a Wally command as part of the workflow.",
	"kind": "command",
	"commandTemplate": "list-loops",
	"writesToDocs": ["Actors/Investigator/Docs/Findings.md"],
	"keywordRoutes": {
		"COMMAND_COMPLETE": "assessState"
	},
	"defaultNextStep": "assessState"
}
```

---

## Mailbox Routing As A Code Step

`route_messages` should be the first concrete built-in code handler.

Responsibilities:

- enumerate source mailbox items, usually in `Outbox/`
- parse front matter to discover target and metadata
- validate the target location exists
- move or copy the message to the next destination
- update status or record routing outcome
- log what happened for later review

When an inbox message is consumed by any step, the relevant documentation must be updated to reflect the new information. Once the message is fully processed and its content is reflected in documentation, the message file is deleted. Documentation is the durable record � mailbox folders are transient delivery surfaces.

It should be callable from loops and runbooks through the same executable-step abstraction.

This gives the product a working mailbox flow without introducing a separate daemon, background worker, or always-on router.

---

## Safety Model

Executable loop steps must be constrained.

Rules:

- no arbitrary inline source code in loop definitions
- only known `HandlerName` values are allowed for `code` steps
- each handler validates its arguments before mutation
- mutating handlers must log their actions and outputs
- workflow docs remain the source of truth for why the step ran and what it changed
- if mailbox or memory behavior requires new runtime support, document the file contract and step responsibilities before implementing the handler

---

## Open Questions

No open questions remain. The key design decisions have been resolved:

- **Step kinds** include `prompt`, `shell`, `command`, `code`, and `user_input` � covering direct prompt execution, programmatic handlers, and user interaction.
- **Initial loop execution is actor-agnostic** � no actor role, criteria, or intent prompt is injected for the beginning loops.
- **Steps may reference reusable abilities** � `abilityRefs` points at documentation-first Wally abilities without taking prompt ownership away from the step.
- **Prompt construction is template-driven** � each named step definition owns its prompt template and declared document inputs.
- **Loops route by step name** � loop definitions reference named steps and keyword routes, while the step owns the custom text.
- **JSON loops are the composition model** � executable steps live inside JSON loop definitions, not a custom engine.
- **Keyword-driven step selection** is the fundamental routing mechanism � each step outputs a keyword that determines the next step.
- **Consumed inbox messages** update documentation and then get deleted.

---

## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [InvestigationLoopProposal](./InvestigationLoopProposal.md) | Primary consumer | InvestigationLoop needs executable steps for mailbox routing, user interaction, and durable state handling |
| [RunbookSyntaxProposal](./RunbookSyntaxProposal.md) | Builds on | Reuse the existing shell and command execution model |
| [UnifiedExecutionModelProposal](../Archive/CompletedProposals/UnifiedExecutionModelProposal.md) | Informs | Reuse data-driven validation patterns |

---

## Phases

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|
| 1 | Finalize typed step model, `abilityRefs`, and keyword-driven routing fields | 1-2 | None |
| 2 | Implement shared execution routing for `prompt`, `shell`, `command`, `code`, and `user_input` | 2-3 | Phase 1 |
| 3 | Implement keyword-driven step selection in the loop runtime | 1-2 | Phase 2 |
| 4 | Implement `route_messages` as the first built-in code handler | 1-2 | Phase 3 |
| 5 | Wire executable steps and reusable abilities into at least one loop and one runbook use case | 1-2 | Phase 4 |

---

## Concepts

- `Executable step`: A loop step backed by runtime execution rather than only an AI prompt.
- `Code handler`: A built-in named runtime operation invoked by a `code` step.
- `Typed step`: A step whose execution semantics are declared explicitly by kind.
- `User input step`: A step that pauses the loop, persists an interaction request, and resumes after the user's answer is persisted.
- `Keyword-driven step selection`: The mechanism where each step's output contains a keyword that determines which step runs next, making loops dynamic and branching.
- `Workflow-owned routing`: Mailbox movement performed by the workflow when needed, not by a separate service.
- `WallyStep`: One of three things � a direct prompt step (with keyword output), a user input request (pause/resume), or a programmatic operation (built-in handler).

---

## Impact

| System/File | Change | Risk Level |
|-------------|--------|------------|
| `Wally.Core/WallyStepDefinition.cs` | Add step kind, `abilityRefs`, keyword routing fields, and handler metadata | High |
| `Wally.Core/WallyLoopDefinition.cs` | Add keyword-driven step routing support to loop definitions | High |
| `Wally.Core/commands/WallyCommands.Run.cs` | Route step kinds to the correct executor; implement keyword-driven routing | High |
| `Wally.Core/commands/WallyCommands.Runbook.cs` | Extract shared shell and command execution helpers | Medium |
| `Wally.Core/Mailbox/` | Add mailbox-routing helper or handler registry | Medium |
| `Wally.Core/Default/Templates/AbilityTemplate.md` | Template for reusable Wally abilities referenced by `abilityRefs` | Low |
| `Wally.Core/Default/Projects/Proposals/ExecutableLoopStepsProposal.md` | Canonical design reference | Low |

---

## Benefits

- Removes pressure to misuse AI prompts for system behavior.
- Makes mailbox routing possible without a separate router subsystem.
- Keeps workflow execution declarative while allowing real runtime operations.
- Creates a reusable abstraction for future loops beyond investigation.
- Keyword-driven step selection makes any JSON loop definition a dynamic branching workflow.
- `user_input` as a first-class step kind gives loops a clean pause/resume model for interactive workflows.
- The same step model works in all Wally loops � not specific to any single loop profile.

---

## Risks

- `code` steps could become a dumping ground for unrelated runtime logic.
- Overloading `WallyStepDefinition` may blur the line between pipeline steps and dynamic workflow steps.
- Mutation-capable handlers raise the need for stronger validation and logging.
- Keyword-driven routing adds complexity to the step model.

Mitigations:

- allow only named built-in handlers
- keep the initial handler set small
- extend `WallyStepDefinition` incrementally � kind, keyword routes, and handler name are well-defined additions
- keep keyword routing simple: one keyword field per step, one route map, one default fallback

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Define the typed step schema for `prompt`, `shell`, `command`, `code`, and `user_input` | High | ?? Not Started | @developer | 2026-04-01 | Core model decision; includes `abilityRefs` and keyword routing fields |
| Define keyword-driven step selection model and routing fields | High | ?? Not Started | @developer | 2026-04-01 | Fundamental to all loops |
| Extract shared shell and command execution helpers for reuse by loop steps | High | ?? Not Started | @developer | 2026-04-02 | Avoid duplicate execution logic |
| Implement built-in handler registry for `code` steps | High | ?? Not Started | @developer | 2026-04-03 | Named handlers only |
| Implement `route_messages` handler with consume-then-delete semantics | High | ?? Not Started | @developer | 2026-04-04 | First concrete runtime step |
| Implement `user_input` step kind with pause/resume semantics | High | ?? Not Started | @developer | 2026-04-04 | Required for interactive loops |
| Validate executable steps inside InvestigationLoop | Medium | ?? Not Started | @developer | 2026-04-05 | Primary consumer |

**Legend**:
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ? Paused | ?? Not Started`

---

## Acceptance Criteria

### Must Have (Required for Approval)

- [ ] Step kinds can express non-AI execution without embedding arbitrary code.
- [ ] A built-in `code` handler can route messages from `Outbox/` to their next location, updating docs and deleting consumed messages.
- [ ] InvestigationLoop can depend on executable steps instead of a separate router service.
- [ ] Shared shell and command execution logic is reused, not duplicated.
- [ ] Keyword-driven step selection is implemented as the standard routing mechanism for JSON loops.
- [ ] `user_input` is a first-class step kind that pauses the loop and resumes after the user's answer is persisted.

### Should Have

- [ ] The same executable-step abstraction works in loops and runbooks.
- [ ] Handler logging is sufficient to debug runtime behavior.

### Success Metrics

- [ ] A workflow can send a message, route it, and consume it without any always-on mailbox process.
- [ ] A reviewer can explain the difference between `prompt`, `shell`, `command`, `code`, and `user_input` steps quickly and precisely.
- [ ] A JSON loop definition can express dynamic branching using keyword-driven step selection without any custom loop engine code.