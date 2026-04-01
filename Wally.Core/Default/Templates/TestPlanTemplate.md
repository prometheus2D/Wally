# Test Plan Template

> Reference: Test Plans define how requirements will be verified through testing.
> See `TemplateTemplate.md` for shared formatting rules and conventions.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, QA, BAs |
| **Scope** | Verification of requirements; test cases, coverage, defects |
| **Out of Scope** | Implementation details; architecture; requirements authoring |
| **Maintenance** | Update when requirements change or new defects are found |
| **Traceability** | Every test case must trace to a requirement ID |

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| RequirementsTemplate | Verifies | Test plans verify requirements |
| TaskTrackerTemplate | Validates | Tests validate what was built |
| BugTemplate | Spawns | Failed tests produce bug reports |

---

## Required Sections

### Header
```markdown
# [Feature] Test Plan

**Status**: [Draft | Executing | Complete]
**Owner**: [Author]
**Created**: [Date]
**Requirements Reference**: [link]
```

### Test Strategy
| Aspect | Approach |
|--------|----------|
| **Test Types** | Unit / Integration / E2E / Manual |
| **Environment** | Local / Staging / CI |
| **Coverage Goals** | Percentage or criteria-based |

### Test Cases
| TC-ID | Requirement | Description | Steps | Expected Result | Status |
|-------|------------|-------------|-------|-----------------|--------|

Status: `Not Run / Pass / Fail`

### Coverage Matrix
| Requirement ID | Priority | Test Cases | Covered? |
|---------------|----------|------------|----------|

### Acceptance Criteria
Must Have / Should Have checkboxes per `TemplateTemplate.md`.

### Done Criteria
- [ ] All Must requirement test cases pass
- [ ] No Critical bugs remain open
- [ ] Coverage matrix has no gaps for Must/Should

---

## Optional Sections

### Defects Found
| Defect ID | TC-ID | Severity | Summary | Status |
|-----------|-------|----------|---------|--------|

---

## File Naming

`[FeatureName]TestPlan.md` — PascalCase, suffix `TestPlan`.

Examples: `AsyncExecutionTestPlan.md`, `MailboxProtocolTestPlan.md`
