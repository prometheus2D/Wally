# Ability Template

> Reference: Abilities define reusable Wally capabilities that loops or actors can reference to apply repeatable workflow logic without copying prompt text.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers and AI agents authoring reusable Wally workflow capabilities |
| **Scope** | Reusable loop guidance, inputs, outputs, safety constraints, prompt guidance, and integration notes for one named ability |
| **Out of Scope** | Full proposal text, legacy plan documents, actor personalities, or step-by-step runtime logs |
| **Maintenance** | Update when the reusable workflow behavior, inputs, outputs, or safety constraints change |
| **Todo Tracking** | Every ability document must track remaining definition or validation work |
| **Acceptance Criteria** | Every ability document must define how a caller knows the ability is correctly specified |
| **Related Documents** | Every ability document must link to loops, proposals, or actors that consume it |

---

## Objectives

- Define one reusable Wally ability in a way that loops and actors can reference consistently.
- Separate reusable workflow knowledge from any one step's custom prompt text.
- Make ability behavior inspectable so a reviewer can understand what guidance is being injected and why.
- Document inputs, outputs, and constraints before runtime support is implemented.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Follows | Abilities are often created or refined from approved proposals |
| TaskTrackerTemplate | Informs | Task trackers and execution loops may map runtime work to the defined ability contract |
| InvestigationLoopProposal | Informs | Investigation loops may reference abilities through `abilityRefs` |
| ExecutableLoopStepsProposal | Informs | Step definitions may consume abilities when building prompts |

---

## Required Sections

### Header
Contains the ability name, status, owner, and dates.
Format constraint: `header block`

### Purpose
Explains what reusable capability this ability provides and when it should be referenced.
Format constraint: `prose`

### Inputs
Lists the documents, placeholders, arguments, or other context the ability expects.
Format constraint: `table`

### Prompt Guidance
Defines the reusable guidance block that steps may inject through `abilityRefs`.
Format constraint: `prose`

### Output Contract
Defines what the consuming step should expect back from work performed under this ability.
Format constraint: `bullet list`

### Constraints
Defines safety rules, non-goals, and boundaries for the ability.
Format constraint: `bullet list`

### Integration Notes
Explains how loops or actors should apply this ability and what kinds of steps should use it.
Format constraint: `bullet list`

### Related Documents
Links to parent proposals, consuming loops, sibling abilities, or related execution artifacts.
Format constraint: `table`

### Open Questions
Tracks unresolved design or usage questions for this ability.
Format constraint: `bullet list`

### Todo Tracker
Tracks remaining work to validate or refine the ability definition.
Format constraint: `table`

### Acceptance Criteria
Defines the conditions under which the ability is ready to use.
Format constraint: `bullet list`

---

## Optional Sections

### Examples
Include when: callers need concrete examples of prompt blocks, inputs, or outputs.
Provides example usage for loops or actors consuming the ability.

### Handler Notes
Include when: the ability is expected to align with a `code` handler or command implementation.
Documents runtime expectations without embedding implementation details.

### Failure Modes
Include when: misuse of the ability could produce misleading or unsafe results.
Documents common failure cases and how callers should respond.

---

## Acceptance Criteria Definition

### Completion Checklist

- The ability has one clear reusable purpose.
- Inputs and expected outputs are explicit.
- Prompt Guidance is reusable across more than one step or loop.
- Constraints are clear enough to prevent accidental misuse.
- Related consumer documents are linked.

### Quality Gates

- A reviewer can explain when to use the ability and when not to use it.
- A loop author can reference the ability without guessing what it injects.
- The ability does not duplicate a step-specific prompt that should stay local to one step.

### Sign-off Requirements

- Approval from the engineer or agent defining the consuming loop.
- Approval from the owner of any proposal the ability materially affects.

---

## Todo Tracker Specification

### Task Categories

- Definition
- Validation
- Consumer alignment
- Safety review

### Priority Levels

- `High`
- `Medium`
- `Low`

### Status Values

- `?? Blocked`
- `?? In Progress`
- `? Complete`
- `?? Paused`
- `?? Not Started`

### Assignment Rules

- Every task must have one owner.
- Validation tasks must identify the consuming loop, actor, or proposal.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered flow; bullets for unordered rules |
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | `?? Blocked`, `?? In Progress`, `? Complete`, `?? Paused`, `?? Not Started` |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|--------|-----------|
| Ability documents that copy one step's entire prompt | Extract only the reusable guidance shared by multiple callers |
| Hiding safety constraints in unrelated proposals | Put the ability's boundaries directly in `Constraints` |
| Abilities that require hidden runtime assumptions | State required inputs, paths, and expected outputs explicitly |
| Treating actor abilities and loop ability refs as the same thing | Document actor authorization separately from loop `abilityRefs` |

---

## File Naming

Use `PascalCaseAbility.md` for ability documents.

Examples: `MailboxRoutingAbility.md`, `InvestigationAssessmentAbility.md`