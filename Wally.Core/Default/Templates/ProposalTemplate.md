# Proposal Document Template

> Reference: Proposals introduce new ideas, features, or approaches.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Senior engineers + AI agents; domain expertise assumed |
| **Scope** | Future plans, phases, concepts |
| **Out of Scope** | Current architecture details; implementation steps; test cases |
| **Maintenance** | Update as proposal evolves; archive when implemented |
| **Density** | Max info/line; no filler |
| **Code** | Inline signatures only (?1 line); source is implementation. No large code blocks |
| **Diagrams** | Mermaid only; no ASCII |

---

## Objectives

- Give engineers and AI agents a clear, actionable description of a problem and its proposed solution.
- Establish phase sequencing, effort estimates, and the concrete set of files affected before implementation begins.
- Serve as the canonical reference for scope decisions throughout implementation.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| RequirementsTemplate | Follows | Proposals may be preceded by or reference a requirements document |
| ImplementationPlanTemplate | Precedes | An approved proposal is the input to an implementation plan |
| ArchitectureTemplate | Informs | Proposals that change system design should reference the current architecture doc |
| ExecutionPlanTemplate | Precedes | Execution plans orchestrate the delivery of one or more approved proposals |

---

## Required Sections

### Header
```markdown
# [Feature/System] Proposal

*Template: [../Templates/ProposalTemplate.md](../Templates/ProposalTemplate.md)*

## Problem Statement
[Concise description of issue/opportunity]

## Resolution
[High-level solution]
```

### Phases
Numbered, with effort estimates.

### Concepts
Inline: `Term: definition`

### Impact
| System/File | Change |

### Benefits
Bullet list.

### Risks
Bullet list.

---

## Optional Sections

### Related Proposals

Use this section to link proposals that are part of the same workstream, depend on each other, or were split from a parent proposal. Always include the relationship type.

```markdown
## Related Proposals

| Proposal | Relationship | Notes |
|----------|--------------|-------|
| [ParentProposal](./ParentProposal.md) | Parent | Spawned from this parent |
| [PhaseXProposal](./PhaseXProposal.md) | Child — Phase N | Extracted phase |
| [OtherProposal](./OtherProposal.md) | Depends on | Must be implemented first |
| [AnotherProposal](./AnotherProposal.md) | Depended on by | This must be implemented first |
| [SiblingProposal](./SiblingProposal.md) | Sibling | Independent parallel workstream |
```

**Relationship types:**

| Type | Meaning |
|------|---------|
| `Parent` | This document was split out from the linked proposal |
| `Child — Phase N` | The linked proposal is a phase extracted from this one |
| `Depends on` | This proposal requires the linked proposal to be completed first |
| `Depended on by` | The linked proposal requires this one first |
| `Sibling` | Same parent; independent execution order |
| `Supersedes` | This proposal replaces the linked one |
| `Superseded by` | The linked proposal replaces this one |

### Detailed Concepts
Expanded explanations with inline signatures.

### Examples
Concrete usage with inline flows.

### Tech Debt
Related debt to address.

### Open Questions
Blockers requiring stakeholder input.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for phases; bullets for benefits/risks |
| Flows | `A ? B ? C` |
| Concepts | `Term: description` |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Multi-line code blocks | Inline signatures only |
| ASCII diagrams | Mermaid |
| Current architecture details | Link to Architecture docs |
| Vague plans | Actionable phases with effort estimates |
| Duplicate info | Cross-reference with Related Proposals |
| Repeating phase detail in parent | Summarise + link to child proposal |

---

## File Naming

`[FeatureName]Proposal.md` — PascalCase, suffix `Proposal`.

Examples: `AsyncExecutionProposal.md`, `AutonomyLoopProposal.md`

### Split Proposal Naming

When a single proposal is split into multiple child proposals, keep the parent as an index/overview and name children after their specific concern:

| Role | Example |
|------|---------|
| Parent (index) | `AutonomousBotGapsProposal.md` |
| Child (phase/workstream) | `AsyncExecutionProposal.md` |
| Child (phase/workstream) | `AutonomyLoopProposal.md` |
| Child (phase/workstream) | `MailboxProtocolProposal.md` |