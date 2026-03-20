# Template Template

> Reference: This is the meta-template. Every template document in this folder must conform to it.
> It defines the **structure** all templates share — not the content of any individual template's sections.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Template authors — engineers or AI agents creating or updating a template |
| **Scope** | The mandatory and optional sections every template must contain; section ordering; naming conventions |
| **Out of Scope** | The content of any individual template's sections; instance-level documents; project-specific conventions |
| **Maintenance** | Update only when a structural rule must change across all templates; changes here require all templates to be audited |

---

## Objectives

- Define the section structure every template must follow so all document types are consistent.
- Give template authors an unambiguous checklist: what to include, in what order, and to what standard.
- Establish a shared relationship vocabulary so cross-document links are unambiguous regardless of document type.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| All other templates | Precedes | Every template must conform to this document |
| ProposalTemplate | Informs | Proposals may spawn new document types that require a new template |

---

## Required Sections

Every template must contain the following sections, **in this order**.

### 1. Title and Reference Line

```markdown
# [DocumentType] Template

> Reference: [One sentence describing the purpose of this document type.]
```

- Title: `[DocumentType] Template` — matches the file name without the `.md` suffix.
- Reference line: one sentence; describes what *instances* of this template are used for.

### 2. Document Constraints

A table of the non-negotiable rules every *instance* of the template must obey.

Required rows (all templates):

| Constraint | Rule |
|------------|------|
| **Audience** | Who reads instances of this document |
| **Scope** | What is in scope for this document type |
| **Out of Scope** | What must not appear in instances of this document type |
| **Maintenance** | When instances must be updated |

Additional rows are allowed; the four required rows must not be removed.

### 3. Objectives

A bullet list of what an instance of this document type is expected to achieve.
Objectives are the "why" — they justify the document's existence and guide authors on what success looks like.

- Minimum one objective; maximum five.
- Format: plain bullet sentences, no sub-bullets.

### 4. Document Relationships

A table declaring how instances of this document type relate to other document types.
This section defines *type-level* relationships (e.g. "a Proposal precedes an Implementation Plan") — not links to specific sibling documents.

```markdown
## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| [DocumentType] | [verb from vocabulary below] | [When/why] |
```

**Relationship vocabulary** — use these terms consistently across all templates:

| Term | Meaning |
|------|---------|
| `Precedes` | This document type is authored before the related type |
| `Follows` | This document type is authored after the related type |
| `Implements` | This document type describes how to build what the related type specifies |
| `Verifies` | This document type confirms the related type's claims or requirements |
| `Spawns` | This document type produces related documents as output |
| `Supersedes` | This document type replaces the related type in its lifecycle role |
| `Informs` | This document type provides input context without a strict ordering |

### 5. Required Sections

A subsection-per-section definition of every section that an instance document **must** contain.

For each required section, specify:
- Section name (as an H3)
- One sentence: what this section contains
- Format constraint: `table | bullet list | prose | Mermaid | code block | header block`

### 6. Optional Sections

A list of sections that may appear in instances but are not required.
For each entry, state the condition under which it should be included and one sentence describing its content.

Format:
```markdown
### [Section Name]
Include when: [condition].
[One sentence: what this section contains.]
```

### 7. Formatting Rules

A table mapping document elements to their required format.

Required rows (all templates):

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered/sequential; bullets for unordered |

Additional rows are encouraged; the four required rows must not be removed.

### 8. Anti-Patterns

A two-column table of practices to avoid and what to do instead.
Minimum four rows. Column headers: `? Avoid` / `? Instead`.

### 9. File Naming

One paragraph and/or table specifying:
- The naming convention (pattern, case style, suffix).
- At least two concrete examples.

---

## Optional Sections

### Status and Metadata Header
Include when: instances have a lifecycle (e.g. Draft ? Approved ? Superseded) or require attribution.
Defines the structured header block — status, owner, dates.

### Mermaid Diagram Guidance
Include when: instances of this document type frequently use diagrams.
Specifies allowed diagram types, max node count, and stylistic rules.

### AI / Automation Prompt Templates
Include when: instances are directly consumed by AI agents for automated execution.
Provides prompt scaffolding for AI-driven workflows.

### Changelog / Update Frequency
Include when: instances are expected to evolve over time and audit history matters.
Defines update triggers and changelog format.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered/sequential; bullets for unordered |
| Section numbers | `### N. Title Case` for Required Sections entries |
| Required vs optional | Always labelled in section headings |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Defining content inside the meta-template | Define structure only; content belongs in individual templates |
| Skipping Objectives | Every template must justify its existence |
| Skipping Document Relationships | Every template must declare how it fits into the document ecosystem |
| Omitting Out of Scope from constraints | Ambiguous scope causes scope creep in instance documents |
| Mixing type-level and instance-level relationships | Document Relationships = type-level; instance links go in Optional Sections |
| Placing sections out of the mandatory order | Follow the 9-step sequence defined in Required Sections |
| Implicit formatting rules | Every format decision must appear in the Formatting Rules table |
| A template that does not conform to this document | TemplateTemplate is the first rule it enforces |

---

## File Naming

`[DocumentType]Template.md` — PascalCase, suffix `Template`.

| Role | File Name |
|------|-----------|
| This document (meta-template) | `TemplateTemplate.md` |
| Proposal template | `ProposalTemplate.md` |
| Architecture template | `ArchitectureTemplate.md` |
| Any new template | `[DocumentType]Template.md` |
