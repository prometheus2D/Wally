# Test Plan Template

> Reference: Test Plans define how requirements will be verified.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, BAs |
| **Traceability** | Every test case traces to a requirement ID |
| **Reproducibility** | Any engineer can execute any test case |
| **Maintenance** | Update when requirements change |

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

*Template: TestPlanTemplate.md*
```

### Test Strategy

| Aspect | Approach |
|--------|----------|
| **Test Types** | [Unit, Integration, E2E, etc.] |
| **Environment** | [Local, Staging, etc.] |
| **Automation** | [What's automated vs manual] |

### Test Cases

| TC-ID | Requirement | Description | Steps | Expected Result | Status |
|-------|------------|-------------|-------|-----------------|--------|
| TC-001 | FR-001 | [What is being tested] | 1. [Step] 2. [Verify] | [Expected outcome] | [Not Run/Pass/Fail] |
| TC-002 | FR-002 | [What is being tested] | 1. [Step] 2. [Verify] | [Expected outcome] | [Status] |

### Edge Cases

| TC-ID | Requirement | Description | Input | Expected Result | Status |
|-------|------------|-------------|-------|-----------------|--------|
| TC-E01 | FR-001 | [Boundary condition] | [Invalid/edge input] | [Graceful handling] | [Status] |

### Coverage Matrix

| Requirement ID | Test Cases | Covered? |
|---------------|------------|----------|
| FR-001 | TC-001, TC-E01 | ? |
| FR-002 | TC-002 | ? |
| FR-003 | (none) | ? Gap |

### Done Criteria
- [ ] All **Must** requirement test cases pass
- [ ] No **Critical** bugs remain open
- [ ] Coverage matrix has no gaps for Must/Should requirements

### Defects Found

| Defect ID | TC-ID | Severity | Summary | Status |
|-----------|-------|----------|---------|--------|
| DEF-001 | TC-003 | High | [Brief description] | [Open/Fixed/Won't Fix] |

### References

| Document | Relationship |
|----------|-------------|
| [Requirements doc] | Tests verify these |
| [Implementation plan] | Tests validate this |

---

## Anti-Patterns

- ? Test cases without requirement traceability
- ? Vague expected results ("it should work")
- ? Steps that require tribal knowledge
- ? Missing edge case tests

---

## File Naming

`[FeatureName]TestPlan.md` — PascalCase, suffix `TestPlan`.
