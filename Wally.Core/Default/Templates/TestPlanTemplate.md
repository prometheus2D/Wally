# Test Plan Template

> Reference: Test Plans define how requirements will be verified.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, QA, BAs |
| **Scope** | Verification of a specific requirements document; test cases, coverage, defects |
| **Out of Scope** | Implementation details; architectural decisions; requirements authoring |
| **Maintenance** | Update when requirements change or new defects are found |
| **Traceability** | Every test case must trace to a requirement ID |
| **Reproducibility** | Any engineer can execute any test case without prior knowledge |

---

## Objectives

- Verify that all approved requirements are covered by at least one test case.
- Provide a reproducible, traceable record of test execution and outcomes.
- Surface defects found during testing with enough context to drive resolution.

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| RequirementsTemplate | Follows | Test plans verify requirements; must reference a requirements document |
| ImplementationPlanTemplate | Follows | Tests validate what was built per the implementation plan |
| BugTemplate | Spawns | Failed test cases that reveal defects should produce a bug document |

---

## Required Sections

### Header
```markdown
# [Feature] Test Plan

**Status**: [Draft | In Review | Approved | Executing | Complete]
**Owner**: [Author]
**Created**: [Date]
**Last Updated**: [Date]
**Requirements Reference**: [Link to Requirements document]

*Template: [../Templates/TestPlanTemplate.md](../Templates/TestPlanTemplate.md)*
```

### Test Strategy

| Aspect | Approach |
|--------|----------|
| **Test Types** | Unit / Integration / E2E / Manual |
| **Environment** | Local / Staging / CI |
| **Automation** | What is automated vs manual |

### Test Cases

| TC-ID | Requirement | Description | Steps | Expected Result | Status |
|-------|------------|-------------|-------|-----------------|--------|

Status values: `Not Run / Pass / Fail`.

### Edge Cases

| TC-ID | Requirement | Description | Input | Expected Result | Status |
|-------|------------|-------------|-------|-----------------|--------|

### Coverage Matrix

| Requirement ID | Test Cases | Covered? |
|---------------|------------|----------|

Flag gaps with: `? Gap`.

### Done Criteria
- [ ] All **Must** requirement test cases pass.
- [ ] No **Critical** bugs remain open.
- [ ] Coverage matrix has no gaps for Must/Should requirements.

### References

| Document | Relationship |
|----------|-------------|

Use relationship vocabulary from TemplateTemplate: `Precedes / Follows / Implements / Verifies / Spawns / Supersedes / Informs`.

---

## Optional Sections

### Defects Found
Include when: test execution has begun and defects have been recorded.

| Defect ID | TC-ID | Severity | Summary | Status |
|-----------|-------|----------|---------|--------|

Link each Defect ID to its Bug document.

---

## Formatting Rules

| Element | Format |
|---------|--------|
| Code / identifiers | Backtick inline code |
| Diagrams | Mermaid only |
| Structured data | Tables preferred over prose |
| Lists | Numbered for test steps; bullets for done criteria |
| Test case IDs | `TC-NNN` prefix |
| Edge case IDs | `TC-ENNN` prefix |
| Defect IDs | `DEF-NNN` prefix |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Test cases without requirement traceability | Every TC-ID maps to a requirement ID |
| Vague expected results ("it should work") | Concrete, observable outcome |
| Steps requiring tribal knowledge | Self-contained, reproducible steps |
| Missing edge case coverage | At least one edge case per Must requirement |
| Defects tracked only in external tools | Record here with link; keep the plan self-contained |

---

## File Naming

`[FeatureName]TestPlan.md` — PascalCase, suffix `TestPlan`.

Examples: `AsyncExecutionTestPlan.md`, `MailboxProtocolTestPlan.md`
