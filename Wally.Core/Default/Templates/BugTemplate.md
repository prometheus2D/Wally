# Bug Report Template

> Reference: Bug reports track defects with symptoms, investigation, and resolution.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, QA |
| **Scope** | A single defect: symptoms, investigation, resolution |
| **Out of Scope** | Feature requests, proposals, general tech debt |
| **Maintenance** | Update after every fix attempt or new evidence |

---

## Required Sections

### Header
```markdown
# [Bug Title]

**Status**: [Open | In Progress | Blocked | Resolved | Won't Fix]
**Priority**: [Critical | High | Medium | Low]
**Affected Systems**: [list]
**First Observed**: [Date]
**Last Updated**: [Date]
```

### Summary
2–3 sentences: what breaks and expected behaviour.

### Reproduction Steps
Numbered minimum steps. End with: **Observe**: [manifestation].

### Investigation
- **Hypotheses**: numbered list with evidence and status (`Untested | Confirmed | Ruled Out`)
- **Code Locations**: `| File | Role in Bug | Lines |`

### Attempted Fixes
Chronological entries: approach, files changed, result (`Success | Partial | Failed`).

### Resolution
Include when resolved: Root Cause, Solution, Files Changed, Prevention.

### Acceptance Criteria
Must Have / Should Have checkboxes per `TemplateTemplate.md`.

---

## File Naming

`[BugID]-[ShortDescription].md` — use issue numbers when available.

Examples: `BUG-123-TileOrganismSync.md`, `NetworkDesync-ChunkLoading.md`
