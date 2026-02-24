using System;
using System.Collections.Generic;
using Wally.Core;

namespace Wally.Core
{
    /// <summary>
    /// Contains the implementation logic for Wally commands.
    /// </summary>
    public static class WallyCommands
    {
        private static WallyEnvironment? _environment;

        /// <summary>
        /// Sets the Wally environment for command operations.
        /// </summary>
        /// <param name="environment">The environment instance.</param>
        public static void SetEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Handles the load command.
        /// </summary>
        /// <param name="path">The path to load from.</param>
        public static void HandleLoad(string path)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.LoadFromWorkspace(path);
            Console.WriteLine($"Workspace loaded from {path}.");
        }

        /// <summary>
        /// Handles the save command.
        /// </summary>
        /// <param name="path">The path to save to.</param>
        public static void HandleSave(string path)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.SaveToWorkspace(path);
            Console.WriteLine($"Environment saved to {path}.");
        }

        /// <summary>
        /// Handles the create command.
        /// </summary>
        /// <param name="path">The path for the new workspace.</param>
        public static void HandleCreate(string path)
        {
            WallyEnvironment.CreateDefaultWorkspace(path);
            Console.WriteLine($"Default workspace created at {path}.");
        }

        /// <summary>
        /// Handles the run command.
        /// </summary>
        /// <param name="prompt">The prompt to run.</param>
        /// <returns>List of responses.</returns>
        public static List<string> HandleRun(string prompt)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            return _environment.RunAgents(prompt);
        }

        /// <summary>
        /// Handles the list command.
        /// </summary>
        public static void HandleList()
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            Console.WriteLine("Agents:");
            foreach (var agent in _environment.Agents)
            {
                Console.WriteLine($"- {agent.GetType().Name}: Role '{agent.Role.Name}', Intent '{agent.Intent.Name}'");
            }
            Console.WriteLine("Configuration Files:");
            foreach (var file in _environment.FilePaths)
            {
                Console.WriteLine($"- {file}");
            }
        }

        /// <summary>
        /// Handles the add-file command.
        /// </summary>
        /// <param name="filePath">The file path to add.</param>
        public static void HandleAddFile(string filePath)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.AddFilePath(filePath);
            Console.WriteLine($"File path added: {filePath}");
        }

        /// <summary>
        /// Handles the load-config command.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file.</param>
        public static void HandleLoadConfig(string jsonPath)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.LoadConfigurationFromJson(jsonPath);
            Console.WriteLine($"Configuration loaded from {jsonPath}");
        }

        /// <summary>
        /// Handles the load-agents command.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file.</param>
        public static void HandleLoadAgents(string jsonPath)
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.LoadDefaultAgents(jsonPath);
            Console.WriteLine($"Agents loaded from {jsonPath}");
        }

        /// <summary>
        /// Handles the ensure-folders command.
        /// </summary>
        public static void HandleEnsureFolders()
        {
            if (_environment == null) throw new InvalidOperationException("Environment not set.");
            _environment.EnsureFoldersExist();
            Console.WriteLine("All required folders ensured to exist.");
        }

        /// <summary>
        /// Handles the setup command.
        /// </summary>
        public static void HandleSetup()
        {
            string currentDir = Directory.GetCurrentDirectory();
            WallyEnvironment.CreateDefaultWorkspace(currentDir);
            Console.WriteLine($"Wally workspace set up in {currentDir}.");
        }

        /// <summary>
        /// Handles the info command.
        /// </summary>
        public static void HandleInfo()
        {
            if (_environment == null)
            {
                Console.WriteLine("No Wally environment set.");
                return;
            }
            Console.WriteLine($"Current Workspace: {_environment.TopFilePath ?? "Not loaded"}");
            Console.WriteLine($"Documentation Folder: {_environment.DocumentationFolder ?? "N/A"}");
            Console.WriteLine($"Working Folder: {_environment.WorkingFolder ?? "N/A"}");
            Console.WriteLine($"Completed Documentation Folder: {_environment.CompletedDocumentationFolder ?? "N/A"}");
            Console.WriteLine($"Agents Loaded: {_environment.Agents.Count}");
            Console.WriteLine($"Files Tracked: {_environment.FilePaths.Count}");
        }

        /// <summary>
        /// Handles the help command.
        /// </summary>
        public static void HandleHelp()
        {
            Console.WriteLine("Wally - AI Agent Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("Available Commands:");
            Console.WriteLine("  load <path>       - Load a Wally workspace from the specified path.");
            Console.WriteLine("  save <path>       - Save the current Wally environment to the specified path.");
            Console.WriteLine("  create <path>     - Create a new default Wally workspace at the specified path.");
            Console.WriteLine("  run <prompt>      - Run all agents on the given prompt.");
            Console.WriteLine("  list              - List agents and configuration files.");
            Console.WriteLine("  add-file <path>   - Add a file path to the Wally environment.");
            Console.WriteLine("  load-config <path>- Load configuration from a JSON file.");
            Console.WriteLine("  load-agents <path>- Load default agents from a JSON file.");
            Console.WriteLine("  ensure-folders    - Ensure all required folders exist in the workspace.");
            Console.WriteLine("  setup             - Set up a Wally workspace in the current directory.");
            Console.WriteLine("  info              - Display information about the current Wally workspace.");
            Console.WriteLine("  help              - Display this help message and workspace info.");
            Console.WriteLine();
            HandleInfo(); // Also show current workspace info
        }
    }
}