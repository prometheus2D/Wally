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

### Agent
- **Purpose**: Abstract base class for comprehensive agents that act on environments, taking roles, acceptance criteria, intents, and prompts to perform actions like code changes or text responses.
- **Properties**:
  - `Role`: The associated role.
  - `AcceptanceCriteria`: The acceptance criteria.
  - `Intent`: The intent.
- **Responsibilities**:
  - Implement the `Act` method to process prompts and act accordingly.
  - Concrete implementations: CopilotAutopilotAgent, WiggumAgent, WallyAgent.

### CopilotAutopilotAgent
- **Purpose**: Simulates GitHub Copilot's autopilot mode for automatic code suggestions and changes.
- **Responsibilities**: Processes prompts to activate autopilot for code modifications.

### WiggumAgent
- **Purpose**: A simple custom agent for roleplaying responses in a fun, Wiggum-inspired style.
- **Responsibilities**: Provides text responses based on role, intent, and criteria.

### WallyAgent
- **Purpose**: A custom agent that fully integrates RBA components for comprehensive actions.
- **Responsibilities**: Combines all prompts to make decisions on code changes or responses.

## RBA Namespace (Wally.Instance.RBA)
Contains simplified classes for Role, AcceptanceCriteria, and Intent, each with `Name` and `Prompt` properties.