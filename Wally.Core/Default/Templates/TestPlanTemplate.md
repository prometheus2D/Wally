# Test Plan Template

> Reference: Test Plans define how requirements will be verified through systematic testing approaches.

---

## Document Constraints

| Constraint | Rule |
|------------|------|
| **Audience** | Engineers, QA, BAs |
| **Scope** | Verification of specific requirements; test cases, coverage, defects |
| **Out of Scope** | Implementation details; architectural decisions; requirements authoring |
| **Maintenance** | Update when requirements change or new defects are found |
| **Traceability** | Every test case must trace to a requirement ID |
| **Reproducibility** | Any engineer can execute any test case without prior knowledge |
| **Todo Tracking** | Every test plan must track test case development, execution, and defect resolution |
| **Acceptance Criteria** | Every test plan must define clear coverage and quality criteria |
| **Related Documents** | Reference requirements being tested and related test plans for integrated testing |

---

## Objectives

- Verify that all approved requirements are covered by at least one test case.
- Provide a reproducible, traceable record of test execution and outcomes.
- Surface defects found during testing with enough context to drive resolution.
- Enable systematic tracking of test development and execution progress.
- Establish clear quality gates and completion criteria for testing phases.

---

## Test Plan Fundamentals

### Test Types Definition
| Type | Purpose | Scope | Execution |
|------|---------|-------|-----------|
| **Unit** | Verify individual components in isolation | Single function/method | Automated, fast feedback |
| **Integration** | Verify component interactions | Multiple components working together | Mix of automated/manual |
| **End-to-End** | Verify complete user workflows | Full system behavior | Primarily manual, some automation |
| **Manual** | Verify user experience and edge cases | Human validation required | Manual execution with documented steps |

### Testing Environment Types
| Environment | Purpose | Characteristics | Usage |
|-------------|---------|-----------------|-------|
| **Local** | Developer testing | Individual machine, mock dependencies | Unit tests, initial validation |
| **Staging** | Pre-production validation | Production-like, real integrations | Integration and E2E testing |
| **CI** | Continuous validation | Automated pipeline execution | Automated test suite execution |

### Test Case Status Values
| Status | Meaning | Next Action |
|--------|---------|-------------|
| **Not Run** | Test case created but not executed | Schedule for execution |
| **Pass** | Test executed successfully with expected results | Mark as complete, update coverage |
| **Fail** | Test executed but did not meet expected results | Create defect, investigate root cause |

### Defect Severity Levels
| Severity | Definition | Impact | Response Time |
|----------|------------|--------|---------------|
| **Critical** | System unusable, data loss, security breach | Complete functionality failure | Immediate |
| **High** | Major functionality impaired | Significant user impact | Same day |
| **Medium** | Minor functionality impaired | Limited user impact | Within 3 days |
| **Low** | Cosmetic or enhancement | No functional impact | Next release cycle |

### Requirement Priority Mapping
| Requirement Priority | Test Coverage Required | Automation Level |
|---------------------|----------------------|------------------|
| **Must** | 100% coverage with multiple test cases | High automation priority |
| **Should** | Full coverage with key scenarios | Medium automation priority |
| **Could** | Basic coverage with happy path | Low automation priority |
| **Won't** | No testing required | Not applicable |

---

## Document Relationships

| Relates To | Relationship | Notes |
|------------|--------------|-------|
| Requirements Documents | Verifies | Test plans verify requirements; must reference specific requirements document |
| Task Trackers | Validates | Tests validate what was built from the executed task tracker |
| Bug Reports | Spawns | Failed test cases that reveal defects should produce bug documentation |
| Architecture Documents | Informs | System design influences test strategy and approach |

**Relationship Vocabulary**: Precedes / Follows / Implements / Verifies / Spawns / Supersedes / Informs

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
**Test Environment**: [Local | Staging | CI | Production]

*Template: TestPlanTemplate.md*
```

### Test Strategy

| Aspect | Approach | Rationale |
|--------|----------|-----------|
| **Test Types** | Unit / Integration / E2E / Manual | Define mix based on risk and automation capability |
| **Environment** | Local / Staging / CI | Specify where each test type will be executed |
| **Automation** | What is automated vs manual | Balance efficiency with thoroughness |
| **Test Data** | Static / Generated / Production-like | Define data strategy for repeatability |
| **Coverage Goals** | Percentage or criteria-based | Set measurable coverage expectations |

### Test Cases

| TC-ID | Requirement | Description | Steps | Expected Result | Status | Priority |
|-------|------------|-------------|-------|-----------------|--------|----------|

**Status values**: `Not Run / Pass / Fail`  
**Priority values**: `Critical / High / Medium / Low`

### Edge Cases

| TC-ID | Requirement | Description | Input | Expected Result | Status | Risk Level |
|-------|------------|-------------|-------|-----------------|--------|------------|

**Risk Level values**: `High / Medium / Low`

### Coverage Matrix

| Requirement ID | Requirement Priority | Test Cases | Covered? | Gap Analysis |
|---------------|---------------------|------------|----------|--------------|

**Coverage Status**: `? Complete / ?? Partial / ? Gap`

### Test Execution Results

| TC-ID | Execution Date | Environment | Result | Notes | Defects Found |
|-------|---------------|-------------|--------|-------|---------------|

### Todo Tracker

| Task | Category | Priority | Status | Owner | Due Date | Completion Criteria | Notes |
|------|----------|----------|--------|-------|----------|-------------------|-------|
| Write test cases for core requirements | Development | High | ?? In Progress | @qa-engineer | 2024-01-15 | All Must requirements have test cases | 60% complete |
| Set up test automation framework | Development | High | ? Complete | @test-lead | 2024-01-10 | Framework ready for test execution | Framework ready |
| Execute manual test cases | Execution | Medium | ?? Blocked | @qa-team | 2024-01-18 | All manual test cases executed | Waiting on test environment |
| Review edge case coverage | Analysis | Medium | ?? Paused | @analyst | 2024-01-20 | Edge cases identified and documented | Waiting on requirements clarification |
| Update coverage matrix | Management | Low | ?? In Progress | @qa-engineer | 2024-01-16 | Coverage matrix reflects actual test results | Tracking with test execution |

**Task Categories**: `Development / Execution / Analysis / Management`  
**Priority Levels**: `High / Medium / Low`  
**Status Values**: `?? Blocked / ?? In Progress / ? Complete / ?? Paused`

### Acceptance Criteria

#### Must Have (Required for Completion)
- [ ] All "Must" and "Should" requirements have associated test cases
- [ ] Coverage matrix shows no gaps for high-priority requirements  
- [ ] All test cases include reproducible steps and expected results
- [ ] Test execution environment set up and validated
- [ ] All critical test cases pass successfully
- [ ] Test traceability to requirements maintained throughout

#### Should Have (Preferred for Quality)
- [ ] Edge cases identified and covered for all major workflows
- [ ] Test automation implemented for regression-prone areas
- [ ] Performance and security test cases included where applicable
- [ ] Test data management strategy implemented
- [ ] Defect reproduction and tracking process established

#### Quality Gates
- Every "Must Have" requirement must have at least one passing test case
- All critical-path workflows must be covered by automated tests
- Edge cases must be identified and tested for major system functions
- Test case steps must be reproducible by any qualified team member
- Defect severity and resolution must align with project risk tolerance

#### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Test execution completed with documented results
- [ ] All critical and high-severity defects resolved
- [ ] Coverage matrix validated and approved by stakeholders
- [ ] Status updated to "Complete"

### Done Criteria
- [ ] All **Must** requirement test cases pass.
- [ ] No **Critical** bugs remain open.
- [ ] Coverage matrix has no gaps for Must/Should requirements.
- [ ] Test execution results documented and reviewed.
- [ ] Stakeholder sign-off obtained.

### Related Documents

| Document | Relationship | Notes |
|----------|-------------|-------|
| [Requirements Document] | Verifies | Requirements being tested by this plan |
| [Component Test Plans] | Precedes | Detailed component-level testing |
| [Integration Test Plans] | Informs | Cross-component integration testing |
| [Bug Reports] | Spawns | Defects found during test execution |

---

## Test Planning Process

### Phase 1: Planning and Design
1. **Requirements Analysis**: Review and understand requirements to be tested
2. **Test Strategy Definition**: Determine test types, environments, and automation approach
3. **Test Case Design**: Create detailed test cases with clear steps and expected results
4. **Coverage Analysis**: Ensure all requirements have appropriate test coverage
5. **Resource Planning**: Identify testing resources, tools, and timeline

### Phase 2: Environment Setup
1. **Test Environment Preparation**: Configure necessary testing environments
2. **Test Data Creation**: Generate or obtain required test data sets
3. **Tool Configuration**: Set up automation frameworks and testing tools
4. **Access and Permissions**: Ensure team has appropriate system access

### Phase 3: Test Execution
1. **Test Case Execution**: Run test cases according to priority and dependencies
2. **Defect Logging**: Document and track any issues found during testing
3. **Results Recording**: Maintain accurate record of test execution outcomes
4. **Coverage Monitoring**: Track progress against coverage goals

### Phase 4: Analysis and Reporting
1. **Results Analysis**: Evaluate test outcomes and coverage achievement
2. **Quality Assessment**: Determine if quality gates have been met
3. **Stakeholder Reporting**: Communicate test results and recommendations
4. **Process Improvement**: Identify lessons learned for future test cycles

---

## Risk Management

### Testing Risks and Mitigation

| Risk | Likelihood | Impact | Mitigation Strategy |
|------|------------|--------|-------------------|
| **Environment Unavailable** | Medium | High | Backup environment ready, advance scheduling |
| **Requirements Change** | High | Medium | Agile test case management, regular reviews |
| **Resource Constraints** | Medium | Medium | Cross-training, flexible resource allocation |
| **Automation Failures** | Low | Medium | Manual backup procedures, tool redundancy |
| **Data Issues** | Medium | High | Data validation scripts, backup data sets |

### Quality Risk Assessment

| Area | Risk Level | Testing Approach |
|------|------------|------------------|
| **Core Functionality** | High | Comprehensive automated and manual testing |
| **Integration Points** | High | Focused integration testing with error scenarios |
| **Edge Cases** | Medium | Targeted testing of boundary conditions |
| **Performance** | Medium | Load testing under expected usage patterns |
| **Security** | High | Penetration testing and vulnerability assessment |

---

## Metrics and Success Criteria

### Test Metrics

| Metric | Target | Measurement Method | Frequency |
|--------|--------|--------------------|-----------|
| **Requirement Coverage** | 100% for Must/Should | Coverage matrix analysis | Daily during execution |
| **Test Case Pass Rate** | >95% for critical tests | Pass/fail tracking | After each execution cycle |
| **Defect Detection Rate** | Industry standard benchmark | Defects found vs. post-release | Weekly analysis |
| **Automation Coverage** | >80% for regression tests | Automated vs. manual ratio | Sprint retrospectives |

### Success Indicators

| Indicator | Green (Success) | Yellow (Warning) | Red (Risk) |
|-----------|----------------|------------------|------------|
| **Coverage** | 100% Must/Should covered | >90% covered | <90% covered |
| **Pass Rate** | >95% critical tests pass | 85-95% pass rate | <85% pass rate |
| **Defects** | No critical defects open | 1-2 critical defects | >2 critical defects |
| **Schedule** | On track with plan | 1-2 days behind | >2 days behind |

---

## Optional Sections

### Defects Found
Include when: test execution has begun and defects have been recorded.

| Defect ID | TC-ID | Severity | Summary | Status | Assigned To | Resolution Target |
|-----------|-------|----------|---------|--------|-------------|-------------------|

### Performance Test Results
Include when: performance testing is part of the test scope.

| Test Scenario | Metric | Target | Actual | Status | Notes |
|---------------|--------|--------|--------|--------|-------|

### Security Test Results
Include when: security testing is part of the test scope.

| Security Control | Test Method | Expected Outcome | Actual Outcome | Risk Level |
|------------------|-------------|------------------|----------------|------------|

---

## Formatting Rules

| Element | Format | Example |
|---------|--------|---------|
| Test case IDs | `TC-NNN` prefix | TC-001, TC-002 |
| Edge case IDs | `TC-ENNN` prefix | TC-E001, TC-E002 |
| Defect IDs | `DEF-NNN` prefix | DEF-001, DEF-002 |
| Code/identifiers | Backtick inline code | `functionName()` |
| Structured data | Tables preferred | Use tables for systematic data |
| Test steps | Numbered lists | 1. Step one, 2. Step two |
| Done criteria | Checkbox lists | `- [ ]` unchecked, `- [x]` checked |
| Status indicators | Emoji prefixes | ?? Blocked, ?? In Progress, ? Complete |
| Document links | Markdown format | `[Document Name](./path/file.md)` |

---

## Anti-Patterns

| ? Avoid | ? Instead |
|----------|-----------|
| Test cases without requirement traceability | Every TC-ID maps to a specific requirement ID |
| Vague expected results ("it should work") | Concrete, measurable, observable outcomes |
| Steps requiring tribal knowledge | Self-contained, reproducible steps with context |
| Missing edge case coverage | At least one edge case per critical requirement |
| External-only defect tracking | Record defects here with links to external tools |
| Unclear completion criteria | Specific, measurable done criteria |
| Generic test strategies | Tailored approach based on system characteristics |
| Missing risk assessment | Document and mitigate testing risks proactively |

---

## File Naming

`[FeatureName]TestPlan.md` � PascalCase, suffix `TestPlan`.

Examples: `AsyncExecutionTestPlan.md`, `MailboxProtocolTestPlan.md`
