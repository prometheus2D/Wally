using System;
using System.Collections.Generic;
using System.IO;
using Wally.Core;

namespace Wally.Core
{
    /// <summary>
    /// Contains the implementation logic for Wally commands.
    /// Each method accepts the <see cref="WallyEnvironment"/> it should operate on —
    /// no static environment state is held here.
    /// </summary>
    public static class WallyCommands
    {
        // ?? Guard ?????????????????????????????????????????????????????????????

        private static WallyEnvironment? RequireWorkspace(WallyEnvironment env, string commandName)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine(
                    $"Command '{commandName}' requires a workspace. " +
                    $"Use 'load <path>' or 'create <path>' first.");
                return null;
            }
            return env;
        }

        // ?? Workspace lifecycle ???????????????????????????????????????????????

        /// <summary>Loads a workspace from <paramref name="path"/> into <paramref name="env"/>.</summary>
        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        /// <summary>Scaffolds a new workspace at <paramref name="path"/> and loads it.</summary>
        public static void HandleCreate(WallyEnvironment env, string path)
        {
            env.CreateWorkspace(path, WallyHelper.ResolveConfig());
            PrintWorkspaceSummary("Workspace created.", env);
        }

        /// <summary>Self-assembles a workspace in the exe directory and loads it.</summary>
        public static void HandleSetup(WallyEnvironment env)
        {
            env.SetupLocal();
            PrintWorkspaceSummary("Workspace ready.", env);
        }

        /// <summary>Saves the active workspace config and all prompt files to <paramref name="path"/>.</summary>
        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            Console.WriteLine($"Workspace saved to: {path}");
        }

        // ?? Reference management ??????????????????????????????????????????????

        public static void HandleAddFolder(WallyEnvironment env, string folderPath)
        {
            if (RequireWorkspace(env, "add-folder") == null) return;
            env.AddFolderReference(folderPath);
            Console.WriteLine($"Folder reference added: {folderPath}");
        }

        public static void HandleAddFile(WallyEnvironment env, string filePath)
        {
            if (RequireWorkspace(env, "add-file") == null) return;
            env.AddFileReference(filePath);
            Console.WriteLine($"File reference added: {filePath}");
        }

        public static void HandleRemoveFolder(WallyEnvironment env, string folderPath)
        {
            if (RequireWorkspace(env, "remove-folder") == null) return;
            bool removed = env.RemoveFolderReference(folderPath);
            Console.WriteLine(removed
                ? $"Folder reference removed: {folderPath}"
                : $"Folder reference not found: {folderPath}");
        }

        public static void HandleRemoveFile(WallyEnvironment env, string filePath)
        {
            if (RequireWorkspace(env, "remove-file") == null) return;
            bool removed = env.RemoveFileReference(filePath);
            Console.WriteLine(removed
                ? $"File reference removed: {filePath}"
                : $"File reference not found: {filePath}");
        }

        public static void HandleClearReferences(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "clear-refs") == null) return;
            env.ClearReferences();
            Console.WriteLine("All folder and file references cleared.");
        }

        // ?? Running actors ????????????????????????????????????????????????????

        public static List<string> HandleRun(WallyEnvironment env, string prompt, string actorName = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();
            return !string.IsNullOrEmpty(actorName)
                ? env.RunActor(prompt, actorName)
                : env.RunActors(prompt);
        }

        public static List<string> HandleRunIterative(
            WallyEnvironment env, string prompt, int maxIterationsOverride = 0)
        {
            if (RequireWorkspace(env, "run-iterative") == null) return new List<string>();

            if (maxIterationsOverride > 0)
                env.MaxIterations = maxIterationsOverride;

            Console.WriteLine($"Running iterative mode (max {env.MaxIterations} iterations)...");

            return env.RunActorsIterative(prompt, (iteration, responses) =>
            {
                Console.WriteLine($"--- Iteration {iteration} ---");
                foreach (var response in responses)
                    Console.WriteLine(response);
                if (responses.Count == 0)
                    Console.WriteLine("No responses. Stopping early.");
            });
        }

        // ?? Workspace inspection ??????????????????????????????????????????????

        /// <summary>
        /// Lists each loaded agent (name, tier, role/criteria/intent prompts),
        /// then folder and file references.
        /// </summary>
        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;

            var ws = env.Workspace!;

            Console.WriteLine($"Agents ({ws.AgentDefinitions.Count}):");
            if (ws.AgentDefinitions.Count == 0)
                Console.WriteLine("  (none — add a subfolder to .wally/Agents/)");

            foreach (var agent in ws.AgentDefinitions)
            {
                Console.WriteLine($"  [{agent.Name}]  folder: {agent.FolderPath}");
                PrintRbaLine("    Role",     agent.Role.Prompt,     agent.Role.Tier);
                PrintRbaLine("    Criteria", agent.Criteria.Prompt, agent.Criteria.Tier);
                PrintRbaLine("    Intent",   agent.Intent.Prompt,   agent.Intent.Tier);
            }

            Console.WriteLine($"Folder References ({ws.FolderReferences.Count}):");
            if (ws.FolderReferences.Count == 0) Console.WriteLine("  (none)");
            foreach (var f in ws.FolderReferences) Console.WriteLine($"  {f}");

            Console.WriteLine($"File References ({ws.FileReferences.Count}):");
            if (ws.FileReferences.Count == 0) Console.WriteLine("  (none)");
            foreach (var f in ws.FileReferences) Console.WriteLine($"  {f}");
        }

        /// <summary>Displays workspace paths, agent count, reference counts, and settings.</summary>
        public static void HandleInfo(WallyEnvironment env)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine("Status:           No workspace loaded.");
                Console.WriteLine("                  Use 'load <path>' or 'create <path>' first.");
                return;
            }

            var ws  = env.Workspace!;
            var cfg = ws.Config;

            Console.WriteLine($"Status:           Workspace loaded");
            Console.WriteLine($"Parent folder:    {ws.ParentFolder}");
            Console.WriteLine($"Project folder:   {ws.ProjectFolder}");
            Console.WriteLine($"Workspace folder: {ws.WorkspaceFolder}");
            Console.WriteLine();
            Console.WriteLine($"Agents folder:    {Path.Combine(ws.WorkspaceFolder, cfg.AgentsFolderName)}");
            Console.WriteLine($"Agents loaded:    {ws.AgentDefinitions.Count}");
            foreach (var a in ws.AgentDefinitions)
                Console.WriteLine($"  {a.Name}");
            Console.WriteLine();
            Console.WriteLine($"Folder refs:      {ws.FolderReferences.Count}");
            Console.WriteLine($"File refs:        {ws.FileReferences.Count}");
            Console.WriteLine($"Max iterations:   {env.MaxIterations}");
        }

        /// <summary>Re-reads agent folders from disk and rebuilds actors without a full reload.</summary>
        public static void HandleReloadAgents(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "reload-agents") == null) return;
            env.ReloadAgents();
            Console.WriteLine($"Agents reloaded: {env.AgentDefinitions.Count}");
            foreach (var a in env.AgentDefinitions)
                Console.WriteLine($"  {a.Name}");
        }

        // ?? Help ??????????????????????????????????????????????????????????????

        public static void HandleHelp()
        {
            Console.WriteLine("Wally — AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("No workspace required:");
            Console.WriteLine("  setup                     Scaffold workspace + default agents next to exe.");
            Console.WriteLine("  create <path>             Scaffold a new workspace at <path>.");
            Console.WriteLine("  load <path>               Load an existing workspace from <path>.");
            Console.WriteLine("  info                      Show workspace paths, agent list, and settings.");
            Console.WriteLine("  help                      Show this message.");
            Console.WriteLine();
            Console.WriteLine("Workspace required:");
            Console.WriteLine("  save <path>               Save config and all agent prompt files.");
            Console.WriteLine("  list                      List agents, prompts, and references.");
            Console.WriteLine("  reload-agents             Re-read agent folders from disk, rebuild actors.");
            Console.WriteLine("  add-folder <path>         Register a folder for Copilot context.");
            Console.WriteLine("  add-file <path>           Register a file for Copilot context.");
            Console.WriteLine("  remove-folder <path>      Deregister a folder.");
            Console.WriteLine("  remove-file <path>        Deregister a file.");
            Console.WriteLine("  clear-refs                Clear all folder and file references.");
            Console.WriteLine("  run \"<prompt>\" [agent]    Run all agents, or one by name.");
            Console.WriteLine("  run-iterative \"<prompt>\"  Run agents iteratively; -m N to cap.");
            Console.WriteLine();
            Console.WriteLine("Agent folders:");
            Console.WriteLine("  .wally/Agents/<AgentName>/");
            Console.WriteLine("    role.txt      — Role prompt  (optional first line: # Tier: task)");
            Console.WriteLine("    criteria.txt  — Acceptance criteria prompt");
            Console.WriteLine("    intent.txt    — Intent prompt");
            Console.WriteLine("  Add a new subfolder to create a new agent.");
            Console.WriteLine("  Each agent is independent — no shared RBA state.");
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            Console.WriteLine(header);
            Console.WriteLine($"  Parent:    {env.ParentFolder}");
            Console.WriteLine($"  Project:   {env.ProjectFolder}");
            Console.WriteLine($"  Workspace: {env.WorkspaceFolder}");
            Console.WriteLine($"  Agents:    {env.AgentDefinitions.Count}");
        }

        private static void PrintRbaLine(string label, string prompt, string? tier)
        {
            string tierTag = string.IsNullOrWhiteSpace(tier) ? "" : $"  [{tier}]";
            // Truncate long prompts for display
            string display = prompt?.Length > 80 ? prompt[..80] + "…" : prompt ?? "";
            Console.WriteLine($"{label}{tierTag}: {display}");
        }
    }
}