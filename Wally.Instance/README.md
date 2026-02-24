# Wally.Instance

This project contains core structures for the Wally AI agent system.

## Classes and Responsibilities

### WallyInstance
- **Purpose**: Main entry point or container for the Wally instance. Currently empty, intended for future expansion to manage overall AI agent lifecycle.

### Role
- **Purpose**: Defines a role to be roleplayed by an AI, based on human-provided prompts.
- **Properties**:
  - `RoleToPlay`: The specific role (e.g., "detective", "teacher").
  - `Intent`: The goal or intent of the role.
  - `AcceptanceCriteria`: An instance of `AcceptanceCriteria` for evaluating the success of the roleplay.
- **Responsibilities**: Holds the core prompts and serves as input for AI brains to perform roleplaying.

### AcceptanceCriteria
- **Purpose**: Defines the criteria for accepting the outcome of a roleplay.
- **Properties**:
  - `Description`: A string describing the acceptance criteria.
- **Responsibilities**: Provides a structured way to specify and evaluate roleplay success criteria.

### Brain
- **Purpose**: Abstract base class for AI brains, which are LLM wrappers responsible for "thinking" and executing roles.
- **Properties**:
  - `Name`: Identifier for the brain implementation.
- **Responsibilities**:
  - Implement the `PerformRole` method to handle roleplaying logic using LLMs.
  - Examples of concrete implementations: WiggumBrain, AutopilotBrain, LoopingAIBrain.