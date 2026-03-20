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

---

## Objectives

- Provide a single authoritative reference for how a system is currently designed.
- Capture non-obvious design decisions and the tradeoffs that drove them.
- Enable engineers and AI agents to reason about the system without reading source code.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Follows | Architecture docs are updated after proposals are implemented |
| ImplementationPlanTemplate | Follows | Implementation plans change the architecture; docs must reflect the result |
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

---

## Anti-Patterns

| ✗ Avoid | ✓ Instead |
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

---

## File Naming

`[SystemName]Architecture.md` — PascalCase, suffix `Architecture`.

Examples: `NetworkingArchitecture.md`, `ChunkingArchitecture.md`, `AIArchitecture.md`
