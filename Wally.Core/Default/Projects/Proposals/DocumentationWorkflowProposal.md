# Documentation Workflow Proposal

**Status**: Draft  
**Author**: System Architect  
**Created**: 2024-01-15  
**Last Updated**: 2024-01-15  

*Template: [../../Templates/ProposalTemplate.md](../../Templates/ProposalTemplate.md)*

## Problem Statement

Currently actors have document creation abilities but work in isolation. There's no systematic way for actors to review project documentation, identify gaps, and collaborate through their mailbox system to maintain comprehensive project documentation. We need a simple loop-based approach where actors can reflect on the current state of documentation and create tasks (including mailbox messages) to improve it.

## Resolution

Create a simple "DocumentationReflection" loop where actors iteratively review existing project documentation, identify what needs to be done, create actionable tasks (including send_message actions to other actors), then process their mailbox communications. This creates a self-sustaining documentation maintenance cycle using the existing Wally loop architecture and mailbox system.

---

## Phases

| Phase | Description | Effort (Days) | Dependencies |
|-------|-------------|---------------|--------------|
| 1 | Create DocumentationReflection loop definition | 1 | Existing loop system |
| 2 | Define actor reflection prompts for document review | 2 | Actor definitions, template knowledge |
| 3 | Test and refine the reflection?task?message cycle | 3 | Mailbox system operational |
| 4 | Create example workflows and usage patterns | 1 | Phase 3 completion |

---

## Concepts

**Documentation Reflection**: `Process where an actor reviews current project documentation, identifies gaps or needed updates, and creates actionable tasks to address them.`

**Reflection Loop**: `Wally loop that iterates actors through: (1) review current state, (2) identify tasks, (3) execute tasks, (4) process mailbox, (5) repeat.`

**Task Creation**: `During reflection, actors create todo items that may include creating documents, updating existing docs, or sending messages to other actors.`

**Mailbox Processing**: `After creating tasks, actors check their inbox for messages from other actors and respond appropriately.`

---

## Simple Loop Architecture

### DocumentationReflection Loop

```json
{
  "Name": "DocumentationReflection",
  "Enabled": true,
  "Description": "Actors reflect on project documentation state and create tasks to improve it",
  "Steps": [
    {
      "Name": "BusinessAnalystReflection",
      "ActorName": "BusinessAnalyst", 
      "PromptTemplate": "Review the current project documentation and identify what needs to be done..."
    },
    {
      "Name": "EngineerReflection", 
      "ActorName": "Engineer",
      "PromptTemplate": "Review technical documentation and implementation status..."
    },
    {
      "Name": "RequirementsReflection",
      "ActorName": "RequirementsExtractor", 
      "PromptTemplate": "Review requirements documents for completeness and clarity..."
    },
    {
      "Name": "MailboxProcessing",
      "ActorName": "BusinessAnalyst",
      "PromptTemplate": "Process inbox messages and coordinate follow-up actions..."
    }
  ]
}
```

### Reflection Cycle

```
Round 1: Each actor reflects on documentation state
  ? BusinessAnalyst: Reviews requirements, execution plans, project status
  ? Engineer: Reviews technical docs, implementation, architecture  
  ? RequirementsExtractor: Reviews requirements clarity and completeness
  ? Each creates todo tasks and sends messages as needed

Round 2: Mailbox processing
  ? Actors process inbox messages from previous round
  ? Create responses, update documents, assign new tasks
  ? Send follow-up messages as needed

Repeat cycle until stable state reached
```

---

## Actor Reflection Patterns

### BusinessAnalyst Reflection
```markdown
**Current Documentation Review**:
- What requirements documents exist and are they complete?
- Are there execution plans for active projects? 
- What's the current project status and are there blockers?
- Are there stakeholder communications that need requirements extraction?

**Tasks to Create**:
- Update project status documents
- Create execution plans for approved requirements  
- Send messages to RequirementsExtractor for stakeholder input
- Send messages to Engineer for technical proposal requests

**Mailbox Actions**:
- send_message to RequirementsExtractor: "Please extract requirements from stakeholder conversation X"
- send_message to Engineer: "Please create proposal for approved requirements Y"
```

### Engineer Reflection
```markdown
**Current Documentation Review**:
- What technical proposals need to be created?
- Are there approved proposals missing implementation plans?
- Is current architecture properly documented?
- Are there bugs that need formal bug reports?
- Do implemented features have test plans?

**Tasks to Create**:
- Write missing architecture documentation
- Create implementation plans for approved proposals
- Document bug reports for known issues
- Create test plans for new features

**Mailbox Actions**:
- send_message to BusinessAnalyst: "Implementation plan complete for proposal X"
- send_message to BusinessAnalyst: "Bug report created for issue Y, needs prioritization"
```

### RequirementsExtractor Reflection  
```markdown
**Current Documentation Review**:
- Are there stakeholder conversations that need requirements extraction?
- Do existing requirements documents need updates based on new input?
- Are there requirements that are unclear and need stakeholder clarification?

**Tasks to Create**:
- Extract requirements from unprocessed stakeholder input
- Update existing requirements based on new information
- Flag unclear requirements for stakeholder review

**Mailbox Actions**:
- send_message to BusinessAnalyst: "Requirements document updated for feature Z"
- send_message to Stakeholder: "Need clarification on requirement ABC"  
```

---

## Loop Execution Example

### Iteration 1: Reflection Phase
```
BusinessAnalyst reflects:
- "Project X has requirements but no execution plan"
- Creates task: "Write execution plan for Project X"  
- Creates message: send_message to Engineer requesting technical proposal

Engineer reflects:
- "Approved proposal Y needs implementation plan"
- "Feature Z implementation lacks architecture documentation"
- Creates tasks for missing documents
- Creates message: send_message to BusinessAnalyst with implementation status

RequirementsExtractor reflects:
- "Stakeholder conversation from last week needs extraction"
- Creates task: "Extract requirements from stakeholder session 2024-01-10"
- Creates message: send_message to BusinessAnalyst when complete
```

### Iteration 2: Mailbox Processing
```
BusinessAnalyst processes inbox:
- Received: "Implementation complete for feature Z" from Engineer
- Action: Update project status, mark feature complete
- Sends: Acknowledgment message to Engineer

Engineer processes inbox: 
- Received: "Please create proposal for new feature W" from BusinessAnalyst
- Action: Create technical proposal document
- Sends: "Proposal ready for review" message when complete

RequirementsExtractor processes inbox:
- No new messages this round
- Continues working on extraction tasks from reflection
```

### Iteration 3: Next Reflection Round
Actors reflect on new state including completed tasks and processed messages, creating next round of tasks...

---

## Simple Implementation

### New Loop Definition
```json
{
  "Name": "DocumentationReflection",
  "Enabled": true,
  "Description": "Actors review documentation state and create improvement tasks",
  "Steps": [
    {
      "Name": "BusinessReflection",
      "ActorName": "BusinessAnalyst",
      "PromptTemplate": "Review current project documentation state:\n\n{browse_workspace}\n\nIdentify missing or outdated documents. Create todo tasks for what needs to be done. Send messages to other actors if their help is needed. Focus on: requirements coverage, execution plans, project status, coordination needs."
    },
    {
      "Name": "TechnicalReflection", 
      "ActorName": "Engineer",
      "PromptTemplate": "Review technical documentation state:\n\n{browse_workspace}\n\nIdentify missing technical documents, outdated architecture, unimplemented approved proposals. Create todo tasks and send messages for coordination. Focus on: proposals, implementation plans, architecture docs, bug reports, test plans."
    },
    {
      "Name": "RequirementsReflection",
      "ActorName": "RequirementsExtractor",
      "PromptTemplate": "Review requirements documentation:\n\n{browse_workspace}\n\nIdentify incomplete requirements, stakeholder input needing extraction, unclear specifications. Create todo tasks and send coordination messages. Focus on: requirements completeness, stakeholder conversation processing."
    },
    {
      "Name": "MailboxCoordination",
      "ActorName": "BusinessAnalyst", 
      "PromptTemplate": "Process mailbox messages from this reflection round:\n\n{browse_workspace --folder=Actors/*/Inbox/}\n\nReview new messages, create responses, coordinate follow-up actions. Update project status based on communications received."
    }
  ]
}
```

---

## Impact

| System/File | Change | Risk Level |
|-------------|--------|------------|
| `Wally.Core/Default/Loops/DocumentationReflection.json` | New loop definition | Low - standard loop format |
| Actor prompt awareness | Include documentation reflection in role prompts | Low - prompt updates only |
| Loop usage patterns | New usage: `wally run --loop=DocumentationReflection` | Low - uses existing infrastructure |

---

## Benefits

- **Uses Existing Architecture**: Leverages current loop system and mailbox messaging
- **Self-Sustaining**: Actors continuously identify and address documentation gaps
- **Simple Operation**: Just run the DocumentationReflection loop periodically  
- **Organic Coordination**: Actors naturally communicate through mailbox when needed
- **Scalable**: Works for small projects or large, complex documentation sets
- **Auditable**: All actor decisions and communications are logged and traceable

---

## Risks

- **Loop Convergence**: Actors might get stuck in cycles creating tasks for each other. *Mitigation: Include loop termination conditions and task prioritization.*
- **Message Overload**: High-activity projects might generate too many messages per cycle. *Mitigation: Start with longer intervals between loop runs.*
- **Task Prioritization**: Actors might create low-value tasks cluttering their todo lists. *Mitigation: Include task priority guidance in reflection prompts.*

---

## Todo Tracker

| Task | Priority | Status | Owner | Due Date | Notes |
|------|----------|--------|-------|----------|-------|
| Create DocumentationReflection.json loop definition | High | ?? In Progress | @engineer | 2024-01-16 | JSON structure 90% complete |
| Update actor prompts for documentation awareness | Medium | ?? Not Started | @engineer | 2024-01-17 | Add reflection guidance to role prompts |
| Test loop with sample project documentation | High | ?? Not Started | @qa | 2024-01-18 | Validate loop behavior and termination |
| Create usage examples and best practices | Low | ?? Not Started | @business-analyst | 2024-01-20 | Document how to use effectively |

**Legend**:
- Priority: `High | Medium | Low`
- Status: `?? Blocked | ?? In Progress | ? Complete | ?? Not Started`

---

## Acceptance Criteria

### Must Have (Required for Approval)
- [ ] DocumentationReflection loop defined and operational
- [ ] Actor reflection prompts include documentation review guidance
- [ ] Loop can execute complete cycles without errors
- [ ] Actors create appropriate todo tasks during reflection
- [ ] Mailbox messaging works correctly within loop context

### Should Have (Preferred for Quality)
- [ ] Loop termination conditions prevent infinite cycles
- [ ] Task prioritization prevents low-value work
- [ ] Usage examples demonstrate effective workflow patterns
- [ ] Performance acceptable for typical project sizes

### Completion Checklist
- [ ] All "Must Have" criteria completed
- [ ] Loop tested with real project documentation
- [ ] Usage patterns documented for different project types
- [ ] Integration with existing loop system validated

---

## Open Questions

1. **Loop Termination**: How should the loop determine when documentation is "complete enough" to stop iterating? *Recommendation: Include convergence detection when actors create no new tasks for two consecutive rounds.*

2. **Actor Sequence**: Should actors reflect simultaneously or in a specific order? Current proposal uses sequential steps - is this optimal? *Recommendation: Start with sequential to avoid conflicts, evaluate parallel execution later.*

3. **Loop Frequency**: How often should DocumentationReflection run - daily, weekly, on-demand? *Recommendation: Start with manual on-demand execution, add scheduling later based on usage patterns.*

4. **Task Persistence**: Should todo tasks created during reflection persist across loop runs, or reset each time? *Recommendation: Persist tasks but allow actors to mark completed tasks as done in subsequent reflections.*

5. **Mailbox Integration**: Should mailbox processing be a separate loop step or integrated into each actor's reflection? *Recommendation: Separate step allows better coordination, but could experiment with both approaches.*

6. **State Tracking**: How should the loop track progress across iterations to avoid redundant work? *Recommendation: Use document timestamps and todo task status to identify what's changed since last reflection.*

---

## Related Documents

| Document | Relationship | Notes |
|----------|--------------|-------|
| [MailboxProtocolProposal](./MailboxProtocolProposal.md) | Depends on | Loop uses send_message actions for actor coordination |
| Existing Loop Definitions | Informs | DocumentationReflection follows same JSON structure pattern |
| Actor Definitions | Informs | Reflection prompts must align with actor capabilities and roles |
| Template Documents | Implements | Loop ensures all templates are used appropriately for document creation |