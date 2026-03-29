# Architecture Document Template

> Reference: Architecture documents capture current system design decisions and patterns.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Senior engineers; assumes domain expertise |
| **Scope** | Current design decisions and patterns |
| **Out of Scope** | Future plans, new ideas, tutorials, implementation steps |
| **Maintenance** | Update on architectural change only |
| **Density** | Maximum information per line; no filler |
| **Code** | Inline signatures only (≤1 line); source is implementation |
| **Diagrams** | Mermaid only; no ASCII art |
| **Length** | Target <150 lines; split if exceeds |
| **Todo Tracking** | Every architecture document must track the actual work needed to implement, validate, or update the architecture |
| **Acceptance Criteria** | Every architecture document must define clear completion criteria for the document itself |
| **Related Documents** | Must reference parent system architecture, child component architectures, and related design documents |

---

## Objectives

- Provide a single authoritative reference for how a system is currently designed.
- Capture non-obvious design decisions and the tradeoffs that drove them.
- Enable engineers and AI agents to reason about the system without reading source code.
- Support sub-architecture documentation for complex systems with multiple components.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Follows | Architecture docs are updated after approved proposal work is implemented |
| TaskTrackerTemplate | Informs | Task trackers identify the concrete implementation work that may require architecture updates |
| RequirementsTemplate | Informs | Requirements may reference architecture constraints |

---

## Required Sections

### Header
```markdown
# [System] Architecture

> *"[Relevant design quote]"* — [Attribution]

*Template: [../Templates/ArchitectureTemplate.md](../Templates/ArchitectureTemplate.md)*
```

### Core Principle
One paragraph + benefits table. State the fundamental invariant that drives all design decisions.

### Authority/Ownership Model
Table of responsibilities. Who owns what state? Who mutates? Who reads?

### Data Flow
How data moves through the system. Use inline notation `A → B → C` or Mermaid for complex flows.

### Protocol/Interface
Tables for message types, method signatures, or API contracts. Include IDs where applicable.

### Patterns
Named patterns with anti-patterns. Format: `**Anti-pattern**: X. **Pattern**: Y.`

### Diagnostics
Table of observable properties for debugging/monitoring.

### Design Principles
Numbered list, one line each. Maximum 7 items. Each principle captures a decision, not a goal.

### Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Implement caching layer as per new architecture | High | 🟡 In Progress | @backend-dev | 2024-01-20 | Redis integration 70% complete |
| Update authentication service to new protocol | High | 🔴 Blocked | @auth-team | 2024-01-18 | Waiting on security review approval |
| Migrate legacy endpoints to new API structure | Medium | ⏸️ Paused | @api-team | 2024-01-25 | Scheduled after Phase 1 completion |
| Add monitoring dashboards for new diagnostic points | Medium | 🟡 In Progress | @devops | 2024-01-22 | Grafana templates in development |
| Refactor data access layer per ownership model | Low | 📋 Not Started | @data-team | 2024-02-01 | Planning phase |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `🔴 Blocked | 🟡 In Progress | ✅ Complete | ⏸️ Paused | 📋 Not Started`

### Acceptance Criteria

#### Must Have (Required for Document Completion)
- [ ] All current design decisions documented with rationale
- [ ] Core principle clearly stated and validated across patterns
- [ ] Authority/ownership model covers all system components
- [ ] Data flow accurately represents current implementation
- [ ] Interface contracts match actual system interfaces
- [ ] Design principles validated against implementation

#### Should Have (Preferred for Quality)
- [ ] Non-obvious tradeoffs documented with alternatives considered
- [ ] Diagnostic approach enables effective system monitoring
- [ ] Patterns include concrete examples from implementation
- [ ] Architecture supports stated performance and scalability goals
- [ ] Design decisions traced to requirements or proposals

#### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Architecture reviewed by technical team and stakeholders
- [ ] Documentation validated against current implementation
- [ ] Status reflects current accuracy and completeness

### Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [SystemArchitecture](./SystemArchitecture.md) | Parent | Overall system design |
| [ComponentAArchitecture](./ComponentAArchitecture.md) | Child — Component | Detailed subsystem design |
| [ComponentBArchitecture](./ComponentBArchitecture.md) | Child — Component | Detailed subsystem design |
| [IntegrationArchitecture](./IntegrationArchitecture.md) | Sibling | Cross-system integration patterns |
| [ArchitectureProposal](./ArchitectureProposal.md) | Informs | Proposed future changes |

---

## Acceptance Criteria Definition

### Completion Checklist
- All architectural decisions documented with clear rationale
- System design accurately reflects current implementation
- Interface contracts validated against actual code
- Design patterns consistently applied across system
- Diagnostic and monitoring approach supports operational needs

### Quality Gates
- Every design decision must include tradeoff analysis
- All interface contracts must be verifiable in implementation
- Core principle must be demonstrably applied across components
- Data flow must be traceable through actual system execution
- Design principles must be concrete and measurable

### Sign-off Requirements
- Technical architect approval for design accuracy
- Engineering team validation of implementation alignment
- Operations team review of diagnostic and monitoring approach
- Stakeholder acceptance of architectural direction

---

## Todo Tracker Specification

### Task Categories
- **Implementation**: Building/coding the architecture (new services, refactoring, migrations)
- **Infrastructure**: Setting up systems, environments, monitoring, deployment
- **Integration**: Connecting components, APIs, data flows per architectural design
- **Validation**: Testing architectural assumptions, performance validation, compliance
- **Documentation**: Updating related docs, creating runbooks, knowledge transfer

### Priority Levels
- **High**: Critical path work, blocking other teams, production issues
- **Medium**: Important architectural improvements, planned enhancements
- **Low**: Technical debt, optimization, future preparation

### Status Values
- **🔴 Blocked**: Cannot proceed due to external dependency or blocker
- **🟡 In Progress**: Actively being worked on
- **✅ Complete**: Finished and deployed/validated
- **⏸️ Paused**: Temporarily stopped, waiting for conditions
- **📋 Not Started**: Planned but not yet begun

### Assignment Rules
- Every task must have a clear owner (@username or @team format)
- Tasks should represent actual implementation work, not documentation tasks
- Blocked tasks must include specific blocker and escalation path
- Due dates should align with project milestones and architectural rollout phases

---

## Optional Sections

### Documenting Decisions
Include when: a significant non-obvious tradeoff was made.

| Element | Format |
|---------|--------|
| Problem statement | **Problem**: [concise issue] |
| Decision | **Decision**: [what we chose] |
| Components | Table of types/roles if multiple |
| Benefits | Bullet list of concrete gains |

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered items; bullets for unordered |
| Inline flows | `A → B → C` |
| Patterns | **Bold label**: description |
| Emphasis | Bold for terms; italic for quotes only |
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | Emoji prefixes: 🔴 Blocked, 🟡 In Progress, ✅ Complete |
| Document relationships | Use standard vocabulary; link with markdown |

---

## Anti-Patterns

| ❌ Avoid | ✅ Instead |
|----------|-----------|
| Multi-line code blocks (>1 line) | Inline signatures only |
| ASCII box diagrams | Mermaid |
| Tutorial-style explanations | Decision + tradeoff |
| Implementation details that change frequently | Stable interfaces and invariants |
| Duplicate information from source code | Reference source; document intent |
| Version history | Use git |
| Justifying obvious choices | Document non-obvious tradeoffs only |
| Mermaid diagrams with >10 nodes | Split diagram or summarise |
| Future plans or proposals | Link to a Proposal document |
| Missing related document references | Must link to parent/child architecture docs |
| Documentation tasks in todo tracker | Todo tracker is for actual implementation work |
| Vague acceptance criteria | Every criterion must be specific and measurable |

---

## File Naming

`[SystemName]Architecture.md` — PascalCase, suffix `Architecture`.

Examples: `NetworkingArchitecture.md`, `ChunkingArchitecture.md`, `AIArchitecture.md`
