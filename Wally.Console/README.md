# Wally Console

Wally is an AI Actor Environment Manager. It helps you create, manage, and interact with AI Actors in a structured workspace.

## Simple Setup

To get started quickly:

1. Download the latest wally.exe from the releases page.
2. Run `wally setup` to create the Wally workspace folder in the exe directory with default prompts and templates.
3. Create a sample project to work with: `wally create-todo ./MyTodoApp`

That's it! Your workspace is ready for AI Actor interactions.

## Setup

1. Ensure you have .NET 8 installed (for building from source).
2. Clone the repository: `git clone https://github.com/prometheus2D/Wally.git`
3. Navigate to the project directory: `cd Wally`
4. Publish the app: `dotnet publish Wally.Console -c Release -r win-x64 --self-contained`
5. Navigate to the publish directory: `cd Wally.Console\bin\Release\net8.0\win-x64\publish`
6. (Optional) Add the publish directory to your PATH environment variable to run `wally` from anywhere.
7. Run the executable: `wally`

This creates a standalone exe named wally.exe that can be run directly on Windows without dotnet.

### Interactive Mode

For iterative use, run `wally` without arguments to enter interactive mode, where you can issue multiple commands in a session, and the environment persists across commands.

### Development Setup

For development:

1. Build the solution: `dotnet build`
2. Run the console app: `dotnet run --project Wally.Console`

## Using Wally for Code Assistance like GitHub Copilot

Wally integrates with AI Actors to provide code assistance similar to GitHub Copilot in Visual Studio. You can operate in two modes: Ask Mode for getting suggestions and explanations, and Agent Mode for autonomous code changes.

### Ask Mode

In Ask Mode, Wally queries the loaded Actors for suggestions, explanations, or code snippets without modifying your files.

1. Set up your workspace: `wally setup`
2. Load the workspace: `wally load WallyWorkspace`
3. Add files to the environment (optional): `wally add-file <path-to-your-code-file>`
4. Run a prompt: `wally run "Implement a function to reverse a string"`

The Actors will respond with AI-generated suggestions based on the loaded Roles, Acceptance Criteria, and Intents. Responses are displayed in the console.

### Agent Mode

In Agent Mode, Wally allows Actors to autonomously apply code changes to your files based on the prompt.

To use Agent Mode:

1. Set up your workspace as above.
2. Load the workspace: `wally load WallyWorkspace`
3. Run a prompt: `wally run "Add error handling to the login method"`

Actors that decide to make changes will apply them directly to the code files. Note that currently, the default Actors provide suggestions; for autonomous changes, you may need to load or configure Actors like CopilotAutopilotActor that implement `ShouldMakeChanges` to return true.

For a simple actor/RBA combination mimicking Copilot, use the default WallyActor with the Developer Role, CodeQuality Acceptance Criteria, and ImplementFeature Intent. This combination leverages GitHub Copilot CLI for intelligent responses.

## Basic Usage

After running the app, use these commands to manage your Wally environment.

### Workspace Management

- `setup`: Set up a Wally workspace in the current directory by creating the default structure.
- `create <path>`: Create a new default Wally workspace at the specified path.
- `load <path>`: Load a Wally workspace from the specified path.
- `save <path>`: Save the current Wally environment to the specified path.

### Configuration and Actors

- `load-config <path>`: Load configuration from a JSON file.
- `load-Actors <path>`: Load default Actors from a JSON file.
- `add-file <path>`: Add a file path to the Wally environment.
- `ensure-folders`: Ensure all required folders exist in the workspace.

### Interaction

- `run <prompt> [actor]`: Run all Actors on the given prompt and display their responses, or a specific Actor if specified. Useful for getting AI suggestions or actions on your code.
- `list`: List the Actors and files in your workspace. Useful to see what's loaded.
- `info`: Show details about your workspace setup, like paths and counts. Useful to check your configuration.
- `commands`: Display the list of commands. Use this when you need a reminder of what to type.

### Default Projects

- `create-todo <path>`: Create a Todo app at the specified path.
- `create-weather <path>`: Create a Weather app at the specified path.

### Help

- `--help`: Display general help screen.
- `--version`: Display version information.

## Example Workflow

1. Set up a new workspace: `wally setup`
2. Load the workspace: `wally load WallyWorkspace`
3. Create a Todo app: `wally create-todo ./MyTodoApp`
4. Run Actors on a prompt: `wally run "Implement a new feature"`

For more details on each command, use `wally commands`.