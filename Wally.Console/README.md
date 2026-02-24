# Wally Console

Wally is an AI Actor Environment Manager. It helps you create, manage, and interact with AI Actors in a structured workspace.

## Simple Setup

To get started quickly:

1. Download the latest wally.exe from the releases page.
2. Run `wally setup` in your project directory to create the Wally workspace structure with default prompts and templates.
3. If you need a sample project, run `wally create-todo <path>` or `wally create-weather <path>` to add a simple app to work with.

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

### Development Setup

For development:

1. Build the solution: `dotnet build`
2. Run the console app: `dotnet run --project Wally.Console`

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

- `run <prompt>`: Run all Actors on the given prompt and display their responses. Useful for getting AI suggestions or actions on your code.
- `list`: List the Actors and files in your workspace. Useful to see what's loaded.
- `info`: Show details about your workspace setup, like paths and counts. Useful to check your configuration.
- `help`: Show the list of commands. Use this when you need a reminder of what to type.

### Default Projects

- `create-todo <path>`: Create a Todo app at the specified path.
- `create-weather <path>`: Create a Weather app at the specified path.

### Help

- `--help`: Display general help screen.
- `--version`: Display version information.

## Example Workflow

1. Set up a new workspace: `wally setup`
2. Load default Actors: `wally load-Actors Wally.Default/default-Actors.json`
3. Create a Todo app: `wally create-todo ./MyTodoApp`
4. Run Actors on a prompt: `wally run "Implement a new feature"`

For more details on each command, use `wally help <command>`.