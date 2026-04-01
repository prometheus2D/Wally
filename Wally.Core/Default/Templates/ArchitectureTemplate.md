# Architecture Document Template

> Reference: Architecture documents capture current system design decisions and patterns.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Senior engineers; assumes domain expertise |
| **Scope** | Current design decisions and patterns |
| **Out of Scope** | Future plans, tutorials, implementation steps |
| **Maintenance** | Update on architectural change only |
| **Density** | Max info/line; inline signatures only (≤1 line); target <150 lines |

---

## Required Sections

### Header
```markdown
# [System] Architecture
*Template: [../Templates/ArchitectureTemplate.md](../Templates/ArchitectureTemplate.md)*
```

### Core Principle
One paragraph + benefits table. Fundamental invariant driving all decisions.

### Authority/Ownership Model
Table of responsibilities: who owns what state, who mutates, who reads.

### Data Flow
`A → B → C` inline or Mermaid for complex flows.

### Protocol/Interface
Tables for message types, method signatures, or API contracts.

### Patterns
Named patterns with anti-patterns. `**Anti-pattern**: X. **Pattern**: Y.`

### Diagnostics
Table of observable properties for debugging/monitoring.

### Design Principles
Numbered list, one line each. Maximum 7 items. Decisions, not goals.

### Acceptance Criteria
Must Have / Should Have checkboxes per `TemplateTemplate.md`.

### Related Documents
Table with relationship type.

---

## Optional Sections

### Documenting Decisions
Include when: a significant non-obvious tradeoff was made.
Format: **Problem** / **Decision** / **Benefits** table.

---

## File Naming

`[SystemName]Architecture.md` — PascalCase, suffix `Architecture`.

Examples: `NetworkingArchitecture.md`, `ChunkingArchitecture.md`
