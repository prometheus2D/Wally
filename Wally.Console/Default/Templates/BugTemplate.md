# Bug Report Template

> Reference: Bug reports track defects with symptoms, investigation, and resolution.

---

## Document Purpose

Bug documents serve as:
- **Investigation tracker**: Document symptoms, hypotheses, and attempted fixes
- **Knowledge base**: Preserve context for future similar issues
- **Communication tool**: Share detailed technical analysis with team

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
2-3 sentences describing the bug from a user's perspective. What breaks? What's the expected behavior?

### Environment
Table of relevant environment details:

| Property | Value |
|----------|-------|
| **Platform** | Server / Client / Both |
| **Network Mode** | Local / Remote / Hosted |
| **Occurrence** | Always / Intermittent / Specific Conditions |
| **Impact** | [Description of user impact] |

### Symptoms
Observable manifestations of the bug. Use bullet points for clarity.

**What happens:**
- [Symptom 1]
- [Symptom 2]
- [Symptom 3]

**What should happen:**
- [Expected behavior]

### Reproduction Steps
Numbered list of minimum steps to reproduce:

1. [Step 1]
2. [Step 2]
3. [Step 3]
4. **Observe**: [Bug manifestation]

Include configuration or setup requirements if needed.

### Investigation

#### Data Flow Analysis
Trace the path data takes through the system:

```
Component A ? Component B ? Component C
             ? [Issue likely here]
```

#### Hypotheses
Numbered list of potential root causes:

1. **[Hypothesis 1]**: [Description]
   - **Evidence for**: [Supporting observations]
   - **Evidence against**: [Contradicting observations]
   - **Status**: [Untested | Testing | Confirmed | Ruled Out]

2. **[Hypothesis 2]**: [Description]
   - **Evidence for**: [Supporting observations]
   - **Evidence against**: [Contradicting observations]
   - **Status**: [Untested | Testing | Confirmed | Ruled Out]

#### Code Locations
Table of relevant files and their roles:

| File | Role in Bug | Lines of Interest |
|------|-------------|-------------------|
| `Path/To/File.cs` | [What this code does] | `123-145` |

### Attempted Fixes

Document all attempted solutions chronologically:

#### Fix Attempt #[N]: [Short Description]
**Date**: [Date]  
**Approach**: [What was tried]  
**Changes Made**:
- File: `Path/To/File.cs` - [Description of change]
- File: `Path/To/File2.cs` - [Description of change]

**Result**: [Success | Partial | Failed]  
**Notes**: [Why it worked/didn't work, observations]

---

### Potential Solutions

List of untried approaches that might solve the issue:

#### Option [N]: [Solution Name]
**Complexity**: [Low | Medium | High]  
**Risk**: [Low | Medium | High]  
**Description**: [What this solution would do]

**Pros**:
- [Benefit 1]
- [Benefit 2]

**Cons**:
- [Drawback 1]
- [Drawback 2]

**Status**: [Not Attempted | In Progress | Attempted | Rejected]

---

### Related Issues
Links to related bugs, architecture docs, or relevant discussions:

- [Related bug #1]
- [Architecture doc that might be relevant]
- [GitHub issue or PR]

---

### Resolution (When Resolved)

**Root Cause**: [Final determined cause]

**Solution**: [What fixed it]

**Files Changed**:
- `Path/To/File.cs` - [Description]

**Prevention**: [How to prevent similar bugs in future]

**Lessons Learned**:
- [Lesson 1]
- [Lesson 2]

---

## Formatting Rules

| Element | Format |
|---------|--------|
| File paths | `BacktickCode` |
| Component names | `BacktickCode` |
| Data flows | `A ? B ? C` |
| Status tags | **Bold**: [Value] |
| Code snippets | Only when essential; <10 lines |
| Logs | Formatted as code blocks; truncate to relevant lines |

---

## File Naming

`[BugID]-[ShortDescription].md` — Use issue numbers if available, otherwise descriptive name.

Examples: 
- `BUG-123-TileOrganismSync.md`
- `NetworkDesync-ChunkLoading.md`
- `EntityPathfinding-NegativeCoords.md`

---

## Update Frequency

Update the bug document:
- ? After each fix attempt
- ? When new evidence is discovered
- ? When hypothesis status changes
- ? When resolved
- ? Not for minor investigation notes (use code comments)
