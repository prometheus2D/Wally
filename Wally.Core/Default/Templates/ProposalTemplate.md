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
| **Todo Tracking** | Every proposal must track research, validation, and approval tasks |
| **Acceptance Criteria** | Every proposal must define clear approval criteria and success metrics |

---

## Objectives

- Give engineers and AI agents a clear, actionable description of a problem and its proposed solution.
- Establish phase sequencing, effort estimates, and the concrete set of files affected before implementation begins.
- Serve as the canonical reference for scope decisions throughout implementation.
- Enable systematic tracking of proposal validation and approval process.

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

**Status**: [Draft | Under Review | Approved | Rejected | Implemented]
**Author**: [Name]
**Created**: [Date]
**Last Updated**: [Date]

*Template: [../Templates/ProposalTemplate.md](../Templates/ProposalTemplate.md)*

## Problem Statement
[Concise description of issue/opportunity]

## Resolution
[High-level solution]
```

### Phases
Numbered, with effort estimates.

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|

### Concepts
Inline: `Term: definition`

### Impact
| System/File | Change | Risk Level |
|-------------|--------|------------|

### Benefits
Bullet list with measurable outcomes.

### Risks
Bullet list with mitigation strategies.

### Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Research existing solutions | High | ? Complete | @author | 2024-01-10 | Found 3 viable approaches |
| Create proof of concept | High | ?? In Progress | @engineer | 2024-01-15 | 50% complete |
| Stakeholder review | Medium | ?? Blocked | @pm | 2024-01-18 | Waiting on calendar availability |
| Technical feasibility analysis | High | ?? Paused | @architect | 2024-01-20 | Waiting on POC results |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Paused`

### Acceptance Criteria

#### Must Have (Required for Approval)
- [ ] Problem statement validated by stakeholders
- [ ] Solution approach technically feasible (POC completed)
- [ ] Effort estimates reviewed and approved by engineering lead
- [ ] Risk assessment completed with mitigation strategies
- [ ] Impact analysis covers all affected systems
- [ ] Approval obtained from technical and business stakeholders

#### Should Have (Preferred for Quality)
- [ ] Alternative solutions evaluated and compared
- [ ] Performance impact analysis completed
- [ ] Security implications reviewed
- [ ] Dependency conflicts identified and resolved

#### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Proposal reviewed by architecture team
- [ ] Status updated to "Approved" or "Rejected"
- [ ] All todo items resolved
- [ ] Next steps (implementation plan) initiated if approved

---

## Acceptance Criteria Definition

### Completion Checklist
- Problem statement clearly articulates business need
- Solution approach is technically sound and implementable
- Effort estimates are realistic and stakeholder-approved
- Risk assessment identifies major concerns with mitigation plans
- Stakeholder approval obtained from all required parties

### Quality Gates
- Solution must be technically feasible (validated through POC or research)
- Effort estimates must be within acceptable project constraints
- Risks must be identified with clear mitigation strategies
- Impact analysis must cover integration points and dependencies

### Sign-off Requirements
- Technical stakeholder approval (architect, engineering lead)
- Business stakeholder approval (product owner, project sponsor)
- Security review if proposal affects security boundaries
- Performance review if proposal affects critical paths

---

## Todo Tracker Specification

### Task Categories
- **Research**: Investigation, analysis, proof of concepts, feasibility studies
- **Validation**: Stakeholder interviews, requirements validation, technical validation
- **Documentation**: Writing, reviewing, updating proposal content
- **Approval**: Review cycles, sign-offs, stakeholder consensus building

### Priority Levels
- **High**: Critical path items, blocking decisions, stakeholder dependencies
- **Medium**: Important but not blocking, quality improvements, nice-to-haves
- **Low**: Documentation polish, minor research, future considerations

### Status Values
- **?? Blocked**: Cannot proceed due to dependency or external blocker
- **?? In Progress**: Actively being worked on
- **? Complete**: Finished and validated
- **?? Paused**: Temporarily stopped, waiting for conditions

### Assignment Rules
- Every task must have a clear owner (@username format)
- Research tasks should have realistic timelines for investigation
- Approval tasks must identify specific stakeholders required
- Blocked tasks must include explanation and path to resolution

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
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | Emoji prefixes: ?? Blocked, ?? In Progress, ? Complete |

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
| Proposals without validation tasks | Every proposal must track validation and approval |
| Acceptance criteria that aren't measurable | Every criterion must be specific and verifiable |

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