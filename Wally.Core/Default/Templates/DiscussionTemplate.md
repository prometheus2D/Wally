# Discussion Document Template

> Reference: Discussion documents capture open questions, design debates, and exploratory thinking.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, architects, AI agents, product owners |
| **Scope** | Open questions, competing options, design tensions, unknowns |
| **Out of Scope** | Finalized decisions, implementation steps |
| **Maintenance** | Update as questions are resolved; archive when complete or spawned into a proposal |

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ProposalTemplate | Spawns | Resolved discussions become proposals |
| ArchitectureTemplate | Informs | Discussions may expose architectural gaps |

---

## Required Sections

### Header
```markdown
# [Topic] – Discussion

**Status**: [Open | Resolved | Deferred | Archived]
**Facilitator**: [Name]
**Created**: [Date]
**Target Resolution**: [Date or milestone]
```

### Context
2–4 sentences framing the discussion.

### Open Questions
Numbered subsections per question:
- **Question**: specific enough for a concrete answer
- **Options**: table with Pros / Cons
- **Current lean**: option or "No lean yet"
- **Status**: `Open | Resolved – [Answer]`
- **Owner**: `@handle`

### Decisions Log
| Question | Decision | Rationale | Date | Owner |
|----------|----------|-----------|------|-------|

### Related Documents
| Document | Relationship | Notes |
|----------|--------------|-------|

---

## Optional Sections

### Hypotheses
Include when: empirical unknowns exist.

### Statements & Positions
Include when: participants want positions on the record.

---

## File Naming

`[TopicName]Discussion.md` — PascalCase, suffix `Discussion`.

Examples: `RunbookSyntaxDiscussion.md`, `LoopOpenSectionDiscussion.md`
