# Execution Plan Template

> Reference: All execution plan documents must conform to this template.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Senior engineers + AI agents; domain expertise assumed |
| **Scope** | Orchestration of one or more implementation plans: dependencies, schedule, assignments, risk |
| **Out of Scope** | Phase-level implementation steps; design rationale; test procedures |
| **Maintenance** | Weekly update; changelog required |
| **Density** | Max info/line; zero redundancy with Implementation Plans |
| **References** | Link to Implementation Plans; never duplicate phase content |
| **Diagrams** | Mermaid only; no ASCII |
| **Length** | Target <150 lines |

---

## Objectives

- Provide a single orchestration view across all in-flight implementation plans.
- Make the critical path, blockers, and weekly schedule visible without reading individual plans.
- Supply AI agents with executable prompts for each phase of each implementation plan.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| ImplementationPlanTemplate | Follows | Execution plans orchestrate implementation plans; never duplicate their content |
| ProposalTemplate | Follows | Proposals are approved before an execution plan is created |
| RequirementsTemplate | Informs | Requirements may constrain schedule or assignment decisions |

---

## Required Sections

### Header
```markdown
# Execution Plan

*Template: [../Templates/ExecutionPlanTemplate.md](../Templates/ExecutionPlanTemplate.md)*
```

### Summary Table
| System | Plan | Days | Deps |

Link to Implementation Plans. No phase details.

### Dependency Graph
Mermaid `flowchart LR`. Show critical path.

### Weekly Schedule
| Week | Systems | Phases | Devs | Milestone |

Reference phases by number; details live in Implementation Plans.

### Key Files
```
[ACTION] path/File.cs — Description
```
Actions: `CREATE`, `MODIFY`, `DELETE`

### AI Iteration Protocol
Prompt templates for: Phase Start, Code Change, Validation, Cross-System Query.

### Dev Assignments
| Pool | Systems | Escalation |

### Risk Matrix
| Risk | P | I | Mitigation | Owner |

P = Probability (L/M/H), I = Impact (L/M/H)

### Done Criteria
Checkboxes for release gates.

### Changelog
| Date | Change | Author |

---

## Optional Sections

### Cross-System Query Table
Include when: multiple systems interact and AI agents need a reference for cross-cutting questions.

| Query | ? | Source |
|-------|---|--------|

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for ordered items; bullets for unordered |
| Key file actions | `CREATE / MODIFY / DELETE` prefix |
| Risk probability/impact | L / M / H abbreviations |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Copying phase steps from Implementation Plans | Reference: "See [Plan] Phase N" |
| ASCII diagrams | Mermaid |
| Prose | Tables and bullets |
| Vague milestones | Concrete: "LLMWrapper.ExecuteAsync compiles" |
| Generic risks | Specific risk + owner |
| Missing changelog entry | Every weekly update must log a change |

---

## File Naming

`ExecutionPlan.md` — single file per project or workstream.
