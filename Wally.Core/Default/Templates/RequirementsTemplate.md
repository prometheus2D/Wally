# Requirements Document Template

> Reference: Requirements documents define WHAT the system must do — not HOW.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Stakeholders, BAs, engineers |
| **Scope** | What the system must do; business context; acceptance criteria |
| **Out of Scope** | How to build it; implementation details; test procedures |
| **Maintenance** | Update on scope change; status field must stay current |
| **Density** | One requirement per row; no bundling |
| **Testability** | Every requirement has at least one acceptance criterion |

---

## Objectives

- Define the agreed scope of a feature or system change in testable, unambiguous terms.
- Provide traceability from business need through to acceptance criteria.
- Serve as the input contract for proposals, implementation plans, and test plans.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Follows | Requirements may originate from or follow a proposal |
| ImplementationPlanTemplate | Precedes | Implementation plans must satisfy requirements |
| TestPlanTemplate | Precedes | Test plans verify requirements; every FR/NFR needs test coverage |
| ArchitectureTemplate | Informs | Architecture constraints may limit or shape requirements |

---

## Required Sections

### Header
```markdown
# [Feature/Epic] Requirements

**Status**: [Draft | In Review | Approved | Superseded]
**Owner**: [Author]
**Created**: [Date]
**Last Updated**: [Date]

*Template: [../Templates/RequirementsTemplate.md](../Templates/RequirementsTemplate.md)*
```

### Business Context

| Field | Value |
|-------|-------|
| **Business Need** | Why this exists — the business driver |
| **Current State** | How things work today |
| **Future State** | How things should work after delivery |
| **Success Metrics** | Measurable outcomes |
| **Out of Scope** | What this does NOT cover |

### Functional Requirements

| ID | Requirement | Priority | Acceptance Criteria | Status |
|----|------------|----------|--------------------|---------| 

Priority values: `Must / Should / Could`. Status values: `Draft / Approved / Done`.

### Non-Functional Requirements

| ID | Category | Requirement | Acceptance Criteria | Status |
|----|----------|------------|--------------------|---------| 

Categories: Performance, Security, Scalability, Reliability, Usability, Maintainability.

### Dependencies

| Dependency | Owner | Impact if Blocked |
|-----------|-------|-------------------|

### Assumptions and Constraints

**Assumptions** (if wrong, requirements change): bullet list.
**Constraints** (non-negotiable): bullet list.

### References

| Document | Relationship |
|----------|-------------|

Use relationship vocabulary from TemplateTemplate: `Precedes / Follows / Implements / Verifies / Spawns / Supersedes / Informs`.

---

## Optional Sections

### Open Questions
Include when: assumptions are unresolved or stakeholder sign-off is pending.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered items; bullets for assumptions/constraints |
| Requirement statements | "The system shall…" — active, present tense |
| Priority | Must / Should / Could — never High/Med/Low |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Implementation details ("use SQL Server") | Behavioural outcome only |
| Requirements without acceptance criteria | Every row must have a testable condition |
| Bundled requirements (multiple things in one ID) | One ID = one testable thing |
| Vague language ("the system should be fast") | Measurable target ("response ? 200 ms") |
| Mixing functional and non-functional in one table | Separate FR and NFR tables |

---

## File Naming

`[FeatureName]Requirements.md` — PascalCase, suffix `Requirements`.

Examples: `AsyncExecutionRequirements.md`, `MailboxProtocolRequirements.md`
