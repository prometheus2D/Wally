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
    }
}