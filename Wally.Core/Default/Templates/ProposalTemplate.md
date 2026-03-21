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
| Create proof of concept for async execution | High | ?? In Progress | @engineer | 2024-01-15 | Event loop implementation 50% complete |
| Set up message queue infrastructure | High | ? Complete | @devops | 2024-01-10 | Redis queues configured |
| Implement mailbox protocol handlers | Medium | ?? Blocked | @developer | 2024-01-18 | Waiting on message schema approval |
| Deploy Phase 1 to staging environment | Medium | ?? Not Started | @devops | 2024-01-22 | Scheduled after POC completion |
| Conduct performance testing | Low | ?? Paused | @qa | 2024-01-25 | Waiting on staging deployment |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Paused | ?? Not Started`

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
- **Development**: Coding, implementing features, building components per the proposal
- **Infrastructure**: Setting up environments, CI/CD, monitoring, deployment systems
- **Testing**: Writing tests, validation, performance testing, user acceptance testing
- **Integration**: Connecting systems, APIs, data flows, third-party integrations
- **Deployment**: Rolling out to environments, production deployment, rollback procedures

### Priority Levels
- **High**: Critical path work, blocking dependencies, production issues, stakeholder commitments
- **Medium**: Important features, planned enhancements, quality improvements
- **Low**: Nice-to-have features, technical debt, optimization, future preparation

### Status Values
- **?? Blocked**: Cannot proceed due to external dependency or blocker
- **?? In Progress**: Actively being developed or worked on
- **? Complete**: Finished, tested, and deployed/validated
- **?? Paused**: Temporarily stopped, waiting for conditions to resume
- **?? Not Started**: Planned but not yet begun

### Assignment Rules
- Every task must have a clear owner (@username format)
- Tasks should represent actual implementation work described in the proposal
- Blocked tasks must include specific blocker and escalation path
- Due dates should align with project phases and delivery milestones

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