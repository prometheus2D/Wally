# Wally.Instance

This project contains core structures for the Wally AI Actor system.

## Classes and Responsibilities

### WallyInstance
- **Purpose**: Main entry point or container for the Wally instance. Currently empty, intended for future expansion to manage overall AI Actor lifecycle.

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

### Actor
- **Purpose**: Abstract base class for comprehensive Actors that act on environments, taking roles, acceptance criteria, intents, and prompts to perform actions like code changes or text responses.
- **Properties**:
  - `Role`: The associated role.
  - `AcceptanceCriteria`: The acceptance criteria.
  - `Intent`: The intent.
- **Responsibilities**:
  - Implement the `Act` method to process prompts and act accordingly.
  - Concrete implementations: CopilotAutopilotActor, WiggumActor, WallyActor.

### CopilotAutopilotActor
- **Purpose**: Simulates GitHub Copilot's autopilot mode for automatic code suggestions and changes.
- **Responsibilities**: Processes prompts to activate autopilot for code modifications.

### WiggumActor
- **Purpose**: A simple custom Actor for roleplaying responses in a fun, Wiggum-inspired style.
- **Responsibilities**: Provides text responses based on role, intent, and criteria.

### WallyActor
- **Purpose**: A custom Actor that fully integrates RBA components for comprehensive actions.
- **Responsibilities**: Combines all prompts to make decisions on code changes or responses.

### WallyEnvironment
- **Purpose**: Manages a collection of Actors in the Wally system.
- **Properties**:
  - `Actors`: A list of Actors.
- **Responsibilities**:
  - Add Actors to the environment.
  - Run all Actors on a prompt and collect responses.
  - Retrieve Actors by type.

## RBA Namespace (Wally.Instance.RBA)
Contains simplified classes for Role, AcceptanceCriteria, and Intent, each with `Name` and `Prompt` properties.

## Actors\RBA Namespace (Wally.Instance.Actors.RBA)
Updated location for RBA classes with constructors.