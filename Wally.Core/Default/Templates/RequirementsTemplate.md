# Requirements Document Template

> Reference: Requirements documents define WHAT the system must do — not HOW.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Stakeholders, BAs, engineers |
| **Scope** | What the system must do; business context; acceptance criteria |
| **Out of Scope** | How to build it; implementation details; test procedures |
| **Maintenance** | Update on scope change; status must stay current |
| **Testability** | Every requirement has at least one acceptance criterion |

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Follows | Requirements may originate from a proposal |
| TaskTrackerTemplate | Precedes | Task trackers must satisfy approved requirements |
| TestPlanTemplate | Precedes | Test plans verify requirements |

---

## Required Sections

### Header
```markdown
# [Feature/Epic] Requirements

**Status**: [Draft | In Review | Approved | Superseded]
**Owner**: [Author]
**Created**: [Date]
**Last Updated**: [Date]
```

### Business Context
| Field | Value |
|-------|-------|
| **Business Need** | Why this exists |
| **Current State** | How things work today |
| **Future State** | How things should work |
| **Success Metrics** | Measurable outcomes |
| **Out of Scope** | What this does NOT cover |

### Functional Requirements
| ID | Requirement | Priority | Acceptance Criteria | Status |
|----|------------|----------|--------------------|---------| 

Priority: `Must / Should / Could`. Status: `Draft / Approved / Done`.

### Non-Functional Requirements
| ID | Category | Requirement | Acceptance Criteria | Status |
|----|----------|------------|--------------------|---------| 

Categories: Performance, Security, Scalability, Reliability, Usability, Maintainability.

### Dependencies
| Dependency | Owner | Impact if Blocked |
|-----------|-------|-------------------|

### Acceptance Criteria
Must Have / Should Have checkboxes per `TemplateTemplate.md`.

---

## File Naming

`[FeatureName]Requirements.md` — PascalCase, suffix `Requirements`.

Examples: `AsyncExecutionRequirements.md`, `MailboxProtocolRequirements.md`
