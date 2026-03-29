# Template Template

> Reference: This is the meta-template. Every template document in this folder must conform to it.
> It defines the **structure** all templates share � not the content of any individual template's sections.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Template authors � engineers or AI agents creating or updating a template |
| **Scope** | The mandatory and optional sections every template must contain; section ordering; naming conventions; todo tracking; acceptance criteria; sub-file relationships |
| **Out of Scope** | The content of any individual template's sections; instance-level documents; project-specific conventions |
| **Maintenance** | Update only when a structural rule must change across all templates; changes here require all templates to be audited |
| **Todo Tracking** | Every template instance must include a Todo Tracker section for task management |
| **Acceptance Criteria** | Every template must define clear, testable acceptance criteria for completion |
| **Related Documents** | Every template must support sub-file relationships; documentation within documentation is a key architectural principle |

---

## Objectives

- Define the section structure every template must follow so all document types are consistent.
- Give template authors an unambiguous checklist: what to include, in what order, and to what standard.
- Establish a shared relationship vocabulary so cross-document links are unambiguous regardless of document type.
- Ensure every template instance has built-in todo tracking and acceptance criteria for systematic completion tracking.
- Enable AI agents and human users to systematically track progress toward document completion.
- **Enforce sub-file architecture**: Every template must support splitting large documents into focused, related child documents.

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

- Title: `[DocumentType] Template` � matches the file name without the `.md` suffix.
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
| **Todo Tracking** | Every template instance must include a Todo Tracker section for task management |
| **Acceptance Criteria** | Every template must define clear, testable acceptance criteria for completion |
| **Related Documents** | How instances of this template relate to and reference other documents |

Additional rows are encouraged; the seven required rows must not be removed.

### 3. Objectives

A bullet list of what an instance of this document type is expected to achieve.
Objectives are the "why" � they justify the document's existence and guide authors on what success looks like.

- Minimum one objective; maximum five.
- Format: plain bullet sentences, no sub-bullets.

### 4. Document Relationships

A table declaring how instances of this document type relate to other document types.
This section defines *type-level* relationships (e.g. "a Proposal precedes a Task Tracker") � not links to specific sibling documents.

```markdown
## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| [DocumentType] | [verb from vocabulary below] | [When/why] |
```

**Relationship vocabulary** � use these terms consistently across all templates:

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

**Mandatory sections for ALL templates**:
- **Todo Tracker**: Task management with priority, status, owner, due date
- **Acceptance Criteria**: Completion criteria with Must Have/Should Have breakdown
- **Related Documents**: Cross-references to parent, child, sibling, or dependent documents

### 6. Optional Sections

A list of sections that may appear in instances but are not required.
For each entry, state the condition under which it should be included and one sentence describing its content.

Format:
```markdown
### [Section Name]
Include when: [condition].
[One sentence: what this section contains.]
```

**Note**: The "Related Documents" section is now REQUIRED for all templates to support the sub-file architecture principle.

### 7. Acceptance Criteria Definition

Every template must define the completion criteria for instances of that document type.
This section specifies what constitutes "done" for documents created from this template.

Required subsections:
- **Completion Checklist**: Bulleted list of must-have items
- **Quality Gates**: Measurable criteria for acceptance
- **Sign-off Requirements**: Who must approve the document

### 8. Todo Tracker Specification

Every template must specify the format and requirements for the todo tracker that will appear in instance documents.

Required elements to specify:
- **Task Categories**: What types of tasks should be tracked
- **Priority Levels**: How to categorize task importance
- **Status Values**: Valid status states (e.g., `Not Started | In Progress | Blocked | Complete`)
- **Assignment Rules**: How tasks should be assigned and tracked

### 9. Formatting Rules

A table mapping document elements to their required format.

Required rows (all templates):

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered/sequential; bullets for unordered |
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | Emoji prefixes: ?? Blocked, ?? In Progress, ? Complete |

Additional rows are encouraged; the six required rows must not be removed.

### 10. Anti-Patterns

A two-column table of practices to avoid and what to do instead.
Minimum four rows. Column headers: `? Avoid` / `? Instead`.

### 11. File Naming

One paragraph and/or table specifying:
- The naming convention (pattern, case style, suffix).
- At least two concrete examples.

---

## Optional Sections

### Status and Metadata Header
Include when: instances have a lifecycle (e.g. Draft ? Approved ? Superseded) or require attribution.
Defines the structured header block � status, owner, dates.

### Todo Tracker Template
Include when: the document type requires complex task management beyond basic checklists.
Provides the exact table format and column specifications for todo tracking in instances.

### Acceptance Criteria Template
Include when: the document type has complex or domain-specific completion criteria.
Provides the exact format for acceptance criteria tables in instances.

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

## Mandatory Instance Sections

Every document created from any template **must** include these sections (in addition to template-specific required sections):

### Todo Tracker
**Purpose**: Track the actual work tasks needed to accomplish what this document defines.

```markdown
## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Implement feature X per requirements | High | ?? In Progress | @developer | 2024-01-15 | Backend API 60% complete |
| Deploy new architecture to staging | Medium | ?? Not Started | @devops | 2024-01-18 | Waiting on infrastructure approval |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Paused | ?? Not Started`
```

**Key Distinction**: This tracks actual implementation work, not document completion tasks.

### Acceptance Criteria
**Purpose**: Define completion criteria for the document itself and its content quality.

```markdown
## Acceptance Criteria

### Must Have (Required for Document Completion)
- [ ] Criterion 1: [Specific, measurable requirement for document completeness]
- [ ] Criterion 2: [Specific, measurable requirement for document quality]

### Should Have (Preferred for Quality)
- [ ] Criterion 3: [Quality enhancement for document]
- [ ] Criterion 4: [Quality enhancement for document]

### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Document reviewed by required stakeholders
- [ ] Status updated to final state
- [ ] All todo items resolved or transferred
```

**Key Distinction**: This defines when the document itself is considered complete and high-quality.

### Related Documents
```markdown
## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [ParentDoc](./ParentDoc.md) | Parent | Spawned from this parent |
| [ChildDoc](./ChildDoc.md) | Child � Component | Extracted section |
| [SiblingDoc](./SiblingDoc.md) | Sibling | Parallel workstream |
| [DependencyDoc](./DependencyDoc.md) | Depends on | Must be completed first |

**Relationship Types**:
- `Parent`: This document was split out from the linked document  
- `Child � [Aspect]`: The linked document is extracted from this one
- `Depends on`: This document requires the linked document first
- `Depended on by`: The linked document requires this one first  
- `Sibling`: Independent documents from same parent
- `Supersedes`: This document replaces the linked one
- `Superseded by`: The linked document replaces this one
- `Informs`: Provides context without strict ordering
```

---

## Critical Distinction: Todo Tracking vs Acceptance Criteria

### Todo Tracker = **Work Tasks**
- **What**: Actual implementation, development, deployment, validation work
- **Examples**: 
  - "Implement caching layer per new architecture"
  - "Deploy authentication service to production" 
  - "Refactor API endpoints to match new design"
  - "Write integration tests for new workflow"

### Acceptance Criteria = **Document Completion**
- **What**: Criteria for when the document itself is complete and high-quality
- **Examples**:
  - "All requirements have testable acceptance criteria"
  - "Architecture accurately reflects current implementation"
  - "Proposal reviewed and approved by stakeholders"
  - "Test cases cover all Must/Should requirements"

**Anti-Pattern**: Putting document-writing tasks in Todo Tracker ("Write section X", "Update diagrams")
**Correct Pattern**: Todo Tracker contains the actual work the document defines; Acceptance Criteria defines document completeness
