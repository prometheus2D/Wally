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
| **Todo Tracking** | Every execution plan must track orchestration tasks, dependencies, and milestones |
| **Acceptance Criteria** | Every execution plan must define clear delivery criteria and success metrics |
| **Related Documents** | Must reference all implementation plans being orchestrated and related execution contexts |

---

## Objectives

- Provide a single orchestration view across all in-flight implementation plans.
- Make the critical path, blockers, and weekly schedule visible without reading individual plans.
- Supply AI agents with executable prompts for each phase of each implementation plan.
- Enable systematic tracking of cross-plan dependencies and delivery milestones.

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

### Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Finalize implementation plan dependencies | High | ?? In Progress | @architect | 2024-01-15 | Mapping cross-system impacts |
| Assign development teams to systems | High | ? Complete | @lead | 2024-01-10 | Team assignments confirmed |
| Set up CI/CD pipeline | Medium | ?? Blocked | @devops | 2024-01-18 | Waiting on infrastructure approval |
| Schedule weekly milestone reviews | Medium | ?? Paused | @pm | 2024-01-20 | Waiting on team calendar sync |

**Legend**: 
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Paused`

### Acceptance Criteria

#### Must Have (Required for Completion)
- [ ] All implementation plans successfully orchestrated and completed
- [ ] Critical path dependencies resolved without blocking delays
- [ ] Weekly milestones achieved according to schedule
- [ ] All cross-system integrations validated and tested
- [ ] Risk mitigation strategies executed successfully
- [ ] Final deliverables meet quality and acceptance standards

#### Should Have (Preferred for Quality)
- [ ] Execution completed ahead of or on original schedule
- [ ] All high-impact risks successfully avoided or mitigated  
- [ ] Team utilization optimized across all workstreams
- [ ] Continuous integration maintained throughout execution
- [ ] Knowledge transfer completed for all components

#### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Final execution review conducted with stakeholders
- [ ] Lessons learned documented for future execution plans
- [ ] All todo items resolved or transferred to maintenance
- [ ] Status updated to "Complete"

### Done Criteria
Checkboxes for release gates.

### Changelog
| Date | Change | Author |

### Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [SystemAImplementationPlan](./SystemAImplementationPlan.md) | Follows | Core system implementation |
| [SystemBImplementationPlan](./SystemBImplementationPlan.md) | Follows | Supporting system implementation |
| [IntegrationTestPlan](./IntegrationTestPlan.md) | Informs | Cross-system validation approach |

---

## Acceptance Criteria Definition

### Completion Checklist
- All referenced implementation plans successfully executed and completed
- Cross-plan dependencies resolved without critical path delays
- Weekly milestone tracking demonstrates consistent progress
- Risk mitigation strategies effectively implemented
- Final deliverables validated and accepted by stakeholders

### Quality Gates
- Every implementation plan must reach "Complete" status
- Critical path must be maintained within schedule tolerances
- All high-severity risks must be successfully mitigated
- Cross-system integration points must be validated
- Team assignments must be optimally utilized

### Sign-off Requirements
- Project manager approval for schedule and milestone achievement
- Technical lead approval for implementation quality
- Stakeholder acceptance of deliverables and outcomes
- Architecture review of cross-system integration results

---

## Todo Tracker Specification

### Task Categories
- **Orchestration**: Cross-plan coordination, dependency management, resource allocation
- **Scheduling**: Timeline management, milestone tracking, resource scheduling
- **Risk Management**: Risk monitoring, mitigation execution, contingency planning
- **Integration**: Cross-system coordination, validation, testing
- **Reporting**: Status updates, stakeholder communication, progress tracking

### Priority Levels
- **High**: Critical path items, blocking dependencies, stakeholder commitments
- **Medium**: Important coordination tasks, quality improvements, optimization
- **Low**: Administrative tasks, documentation, process improvements

### Status Values
- **?? Blocked**: Cannot proceed due to external dependency or blocker
- **?? In Progress**: Actively being coordinated or executed
- **? Complete**: Finished and validated across all plans
- **?? Paused**: Temporarily stopped, waiting for conditions

### Assignment Rules
- Every task must have a clear owner (@username format)
- Tasks should align with execution phases and cross-plan dependencies
- Blocked tasks must include detailed explanation and escalation path
- Due dates must account for critical path and resource availability

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
| Todo items | `- [ ]` unchecked or `- [x]` checked checkbox format |
| Status indicators | Emoji prefixes: ?? Blocked, ?? In Progress, ? Complete |
| Document relationships | Use standard vocabulary; link with markdown |

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
| Tasks without owners or due dates | Every task must have clear ownership and timeline |
| Execution plans without related document references | Must link to all orchestrated implementation plans |

---

## File Naming

`ExecutionPlan.md` — single file per project or workstream.
