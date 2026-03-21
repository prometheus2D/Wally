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
| **Todo Tracking** | Every requirements document must track completion of all requirements and reviews |
| **Acceptance Criteria** | Every requirement must have measurable, testable acceptance criteria |
| **Related Documents** | Must reference parent requirements, child domain requirements, and related specification documents |

---

## Objectives

- Define the agreed scope of a feature or system change in testable, unambiguous terms.
- Provide traceability from business need through to acceptance criteria.
- Serve as the input contract for proposals, implementation plans, and test plans.
- Enable systematic tracking of requirements completion and stakeholder approval.
- Support requirements decomposition for complex systems with multiple domains or components.

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

### Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Gather business requirements | High | ? Complete | BA Team | 2024-01-10 | Initial stakeholder interviews done |
| Define functional requirements | High | ?? In Progress | @analyst | 2024-01-15 | 60% complete |
| Define non-functional requirements | Medium | ?? Blocked | @architect | 2024-01-18 | Waiting on performance targets |
| Stakeholder review | High | ?? Paused | @pm | 2024-01-20 | Scheduled after requirements complete |
| Final approval | High | ?? Paused | @stakeholder | 2024-01-25 | Waiting on review completion |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Paused`

### Acceptance Criteria

#### Must Have (Required for Completion)
- [ ] All functional requirements have specific, testable acceptance criteria
- [ ] All non-functional requirements include measurable targets
- [ ] Business context clearly defines success metrics
- [ ] Dependencies identified with ownership and risk assessment
- [ ] All requirements reviewed by stakeholders and approved
- [ ] Document status updated to "Approved"

#### Should Have (Preferred for Quality)
- [ ] Requirements traced to business objectives
- [ ] Non-functional requirements include performance baselines
- [ ] Edge cases and error conditions documented
- [ ] Requirements prioritized using MoSCoW method

#### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Document reviewed by required stakeholders (BA, Architect, PM)
- [ ] Status updated to "Approved"
- [ ] All todo items resolved or transferred to implementation phase
- [ ] Requirements linked to corresponding test cases

### Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [SystemRequirements](./SystemRequirements.md) | Parent | High-level system requirements |
| [AuthenticationRequirements](./AuthenticationRequirements.md) | Child — Domain | Authentication-specific requirements |
| [UIRequirements](./UIRequirements.md) | Child — Domain | User interface requirements |
| [APIRequirements](./APIRequirements.md) | Child — Domain | API and integration requirements |
| [SecurityRequirements](./SecurityRequirements.md) | Sibling | Cross-cutting security requirements |
| [PerformanceRequirements](./PerformanceRequirements.md) | Sibling | System-wide performance requirements |

### References

| Document | Relationship |
|----------|-------------|

Use relationship vocabulary from TemplateTemplate: `Precedes / Follows / Implements / Verifies / Spawns / Supersedes / Informs`.

---

## Acceptance Criteria Definition

### Completion Checklist
- All functional and non-functional requirements documented with testable acceptance criteria
- Business context provides clear justification and success metrics
- Stakeholder review and approval obtained
- Dependencies and constraints clearly identified
- Document status reflects current approval state

### Quality Gates
- Every requirement must have at least one measurable acceptance criterion
- All high-priority requirements must be approved by designated stakeholders
- Success metrics must be quantifiable and time-bound
- Dependencies must include risk mitigation strategies

### Sign-off Requirements
- Business stakeholder approval for business context and priorities
- Architecture team approval for non-functional requirements
- Product manager approval for scope and timeline impact
- Quality assurance review of testability

---

## Todo Tracker Specification

### Task Categories
- **Analysis**: Requirements gathering, stakeholder interviews, research
- **Documentation**: Writing requirements, acceptance criteria, constraints
- **Review**: Stakeholder review, peer review, technical validation
- **Approval**: Final sign-offs, status updates, handoff preparation

### Priority Levels
- **High**: Critical path items, stakeholder dependencies, deadline-driven tasks
- **Medium**: Important but flexible timeline, quality improvements
- **Low**: Nice-to-have items, future considerations, documentation polish

### Status Values
- **?? Blocked**: Cannot proceed due to external dependency or blocker
- **?? In Progress**: Actively being worked on
- **? Complete**: Finished and verified
- **?? Paused**: Temporarily stopped, waiting for conditions to resume

### Assignment Rules
- Every task must have a clear owner (@username format)
- Tasks should have realistic due dates aligned with project milestones
- Blocked tasks must include notes explaining the blocker and mitigation plan

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
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | Emoji prefixes: ?? Blocked, ?? In Progress, ? Complete |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Implementation details ("use SQL Server") | Behavioural outcome only |
| Requirements without acceptance criteria | Every row must have a testable condition |
| Bundled requirements (multiple things in one ID) | One ID = one testable thing |
| Vague language ("the system should be fast") | Measurable target ("response ? 200 ms") |
| Mixing functional and non-functional in one table | Separate FR and NFR tables |
| Todos without owners or due dates | Every todo must have clear ownership and timeline |
| Acceptance criteria that aren't measurable | Every criterion must be verifiable and specific |

---

## File Naming

`[FeatureName]Requirements.md` — PascalCase, suffix `Requirements`.

Examples: `AsyncExecutionRequirements.md`, `MailboxProtocolRequirements.md`
