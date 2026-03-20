# Bug Report Template

> Reference: Bug reports track defects with symptoms, investigation, and resolution.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, QA |
| **Scope** | A single defect: symptoms, investigation, attempted fixes, resolution |
| **Out of Scope** | Feature requests, architectural proposals, general technical debt |
| **Maintenance** | Update after every fix attempt, new evidence, or status change |

---

## Objectives

- Preserve all investigation context so any engineer can pick up the bug without prior knowledge.
- Track hypotheses and attempted fixes to avoid repeating failed approaches.
- Provide a resolution record that prevents the same bug from being reintroduced.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| RequirementsTemplate | Informs | Bug may reference a broken requirement |
| TestPlanTemplate | Informs | Bug triggers new or updated test cases |
| ArchitectureTemplate | Informs | Bug investigation may reveal architectural issues |
| ProposalTemplate | Spawns | Systemic bugs may spawn a proposal for a structural fix |

---

## Required Sections

### Header
```markdown
# [Bug Title]

**Status**: [Open | In Progress | Blocked | Resolved | Won't Fix]
**Priority**: [Critical | High | Medium | Low]
**Affected Systems**: [List of affected components]
**First Observed**: [Date or version]
**Last Updated**: [Date]

*Template: [../Templates/BugTemplate.md](../Templates/BugTemplate.md)*
```

### Summary
2–3 sentences: what breaks and what the expected behaviour is.

### Environment
Table of relevant environment details.

| Property | Value |
|----------|-------|
| **Platform** | Server / Client / Both |
| **Occurrence** | Always / Intermittent / Specific Conditions |
| **Impact** | [Description of user impact] |

### Symptoms

**What happens**: bullet list of observable manifestations.
**What should happen**: bullet list of expected behaviour.

### Reproduction Steps
Numbered minimum steps to reproduce. End with: **Observe**: [bug manifestation].

### Investigation

#### Data Flow Analysis
Trace the path data takes; annotate where the issue likely occurs.

#### Hypotheses
Numbered list. Each entry: description, evidence for, evidence against, status (`Untested | Testing | Confirmed | Ruled Out`).

#### Code Locations
| File | Role in Bug | Lines of Interest |
|------|-------------|-------------------|

### Attempted Fixes
Chronological. Each entry: approach, files changed, result (`Success | Partial | Failed`), notes.

### Resolution
Include when resolved. Fields: Root Cause, Solution, Files Changed, Prevention, Lessons Learned.

---

## Optional Sections

### Potential Solutions
Include when: fix is not yet chosen. Each option: complexity, risk, pros, cons, status.

### Related Issues
Include when: related bugs, docs, or external issues exist.
Format: `| Document | Relationship |` table using vocabulary from TemplateTemplate.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for reproduction steps; bullets for symptoms |
| Status tags | **Bold**: [Value] |
| Logs | Code block; truncate to relevant lines only |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Feature requests in bug documents | Open a proposal |
| Hypotheses without evidence | Record evidence for and against |
| Fix attempts without results | Always record outcome |
| Exposing sensitive content in resolution notes | Summarise; redact data |
| Leaving Resolution empty after fix | Complete all Resolution fields |

---

## File Naming

`[BugID]-[ShortDescription].md` — use issue numbers when available; otherwise descriptive name.

Examples: `BUG-123-TileOrganismSync.md`, `NetworkDesync-ChunkLoading.md`
