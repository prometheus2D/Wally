# Template Template

> Reference: Meta-template. Every template in this folder must conform to this structure.

---

## Structure

Every template must contain these sections **in this order**:

1. **Title + Reference line** — `# [Type] Template` then one-sentence purpose.
2. **Document Constraints** — table: Audience, Scope, Out of Scope, Maintenance.
3. **Required Sections** — H3 per section with one-sentence description and format hint.
4. **Optional Sections** — list with include-when conditions.
5. **File Naming** — pattern, case style, two examples.

---

## Shared Formatting Rules

All templates and their instances use these conventions:

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only; no ASCII |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered; bullets for unordered |
| Tracking | `- [ ]` / `- [x]` in Acceptance Criteria section |

---

## Shared Anti-Patterns

| ❌ Avoid | ✅ Instead |
|----------|-----------|
| Multi-line code blocks | Inline signatures only |
| Separate Todo Tracker section | Use Acceptance Criteria checkboxes |
| Duplicating content from other docs | Cross-reference with links |
| Vague acceptance criteria | Specific, verifiable conditions |

---

## Instance Requirements

Every document created from any template must include:

- **Header** with Status, Author/Owner, Created, Last Updated
- **Acceptance Criteria** with Must Have / Should Have checkboxes
- **Related Documents** table with relationship type

These three sections are mandatory across all document types. Template-specific required sections are additive.

---

## Relationship Vocabulary

Use consistently in Related Documents tables:

| Term | Meaning |
|------|---------|
| `Precedes` | Authored before the related type |
| `Follows` | Authored after the related type |
| `Implements` | Describes how to build what the related type specifies |
| `Spawns` | Produces related documents as output |
| `Supersedes` | Replaces the related type |
| `Informs` | Provides context without strict ordering |

---

## File Naming

`[DocumentType]Template.md` — PascalCase.

Examples: `ProposalTemplate.md`, `TaskTrackerTemplate.md`
