# Requirements Document Template

> Reference: Requirements documents define WHAT the system must do — not HOW.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Stakeholders, BAs, engineers |
| **Density** | One requirement per row; no bundling |
| **Testability** | Every requirement has at least one acceptance criterion |
| **Scope** | What the system must do, not how to build it |
| **Maintenance** | Update on scope change |

---

## Required Sections

### Header
```markdown
# [Feature/Epic] Requirements

**Status**: [Draft | In Review | Approved | Superseded]  
**Owner**: [Author]  
**Created**: [Date]  
**Last Updated**: [Date]

*Template: RequirementsTemplate.md*
```

### Business Context

| Field | Value |
|-------|-------|
| **Business Need** | [Why this exists — the business driver] |
| **Current State** | [How things work today] |
| **Future State** | [How things should work after delivery] |
| **Success Metrics** | [Measurable outcomes] |
| **Out of Scope** | [What this does NOT cover] |

### Functional Requirements

| ID | Requirement | Priority | Acceptance Criteria | Status |
|----|------------|----------|--------------------|---------| 
| FR-001 | [The system shall...] | [Must/Should/Could] | [Testable condition] | [Draft/Approved/Done] |
| FR-002 | [The system shall...] | [Must/Should/Could] | [Testable condition] | [Draft/Approved/Done] |

### Non-Functional Requirements

| ID | Category | Requirement | Acceptance Criteria | Status |
|----|----------|------------|--------------------|---------| 
| NFR-001 | Performance | [The system shall...] | [Measurable target] | [Status] |
| NFR-002 | Security | [The system shall...] | [Verifiable condition] | [Status] |

**Categories**: Performance, Security, Scalability, Reliability, Usability, Maintainability

### Dependencies

| Dependency | Owner | Impact if Blocked |
|-----------|-------|-------------------|
| [System/API/Team] | [Who] | [What happens] |

### Assumptions & Constraints

**Assumptions** (if wrong, requirements change):
- [Assumption]

**Constraints** (non-negotiable):
- [Constraint]

### References

| Document | Relationship |
|----------|-------------|
| [Document name] | [Originated from / Constrained by / Verified by / Implemented by] |

---

## Anti-Patterns

- ? Implementation details ("use SQL Server", "create a REST endpoint")
- ? Requirements without acceptance criteria
- ? Bundled requirements (one ID = one testable thing)
- ? Vague language ("the system should be fast")

---

## File Naming

`[FeatureName]Requirements.md` — PascalCase, suffix `Requirements`.
