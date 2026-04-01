# Proposal Document Template

> Reference: Proposals introduce new ideas, features, or approaches.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Senior engineers + AI agents; domain expertise assumed |
| **Scope** | Future plans, phases, concepts |
| **Out of Scope** | Current architecture details; implementation steps; test cases |
| **Maintenance** | Update as proposal evolves; archive when implemented |
| **Density** | Max info/line; no filler. Inline signatures only (≤1 line) |

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| RequirementsTemplate | Follows | Proposals may reference a requirements document |
| TaskTrackerTemplate | Precedes | Approved proposals are decomposed into task trackers via `ProposalToTasks` |
| ArchitectureTemplate | Informs | Proposals that change system design should reference current architecture |

---

## Required Sections

### Header
```markdown
# [Feature/System] Proposal

**Status**: [Draft | Under Review | Approved | Rejected | Implemented]
**Author**: [Name]
**Created**: [Date]
**Last Updated**: [Date]

## Problem Statement
[Concise description of issue/opportunity]

## Resolution
[High-level solution]
```

### Phases
Numbered, with effort estimates.

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|

### Impact
| System/File | Change | Risk Level |
|-------------|--------|------------|

### Benefits
Bullet list with measurable outcomes.

### Risks
Bullet list with mitigation strategies.

### Open Questions
- List unresolved questions blocking approval, estimation, or scope confidence.
- Use `None` or mark questions as `Resolved` / `Deferred` when the section is clear.

### Acceptance Criteria
```markdown
### Must Have (Required for Approval)
- [ ] Criterion with specific, verifiable condition

### Should Have (Preferred for Quality)
- [ ] Quality enhancement criterion
```

### Related Documents
| Document | Relationship | Notes |
|----------|--------------|-------|

---

## Optional Sections

### Concepts
Inline: `Term: definition`

### Related Proposals
| Proposal | Relationship | Notes |
|----------|--------------|-------|

Relationship types: `Parent`, `Child – Phase N`, `Depends on`, `Sibling`, `Supersedes`

### Detailed Concepts / Examples / Tech Debt
Include when: additional depth is needed.

---

## File Naming

`[FeatureName]Proposal.md` — PascalCase, suffix `Proposal`.

Examples: `AsyncExecutionProposal.md`, `AutonomyLoopProposal.md`