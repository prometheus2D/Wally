using System;
using System.Collections.Generic;
using System.IO;
using Wally.Core;
using Wally.Core.Actors;

namespace Wally.Core
{
    /// <summary>
    /// Contains the implementation logic for Wally commands.
    /// Each method accepts the <see cref="WallyEnvironment"/> it should operate on —
    /// no static environment state is held here.
    /// </summary>
    public static class WallyCommands
    {
        // — Guard —————————————————————————————————————————————————————————————

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

        // — Workspace lifecycle ———————————————————————————————————————————————

        /// <summary>Loads a workspace from <paramref name="path"/> into <paramref name="env"/>.</summary>
        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        /// <summary>Scaffolds a new workspace at <paramref name="path"/> and loads it.</summary>
        public static void HandleCreate(WallyEnvironment env, string path)
        {
            env.CreateWorkspace(path, WallyHelper.ResolveConfig(path));
            PrintWorkspaceSummary("Workspace created.", env);
        }

        /// <summary>
        /// Ensures a workspace exists at <paramref name="path"/> (or the default location
        /// when <paramref name="path"/> is null) and loads it.
        /// </summary>
        public static void HandleSetup(WallyEnvironment env, string path = null)
        {
            env.SetupLocal(path);
            PrintWorkspaceSummary("Workspace ready.", env);
        }

        /// <summary>Saves the active workspace config and all actor files to <paramref name="path"/>.</summary>
        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            Console.WriteLine($"Workspace saved to: {path}");
        }

        // — Running actors ————————————————————————————————————————————————————

        public static List<string> HandleRun(WallyEnvironment env, string prompt, string actorName = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();
            return !string.IsNullOrEmpty(actorName)
                ? env.RunActor(prompt, actorName)
                : env.RunActors(prompt);
        }

        /// <summary>
        /// Runs actors iteratively. When <paramref name="actorName"/> is supplied, a single
        /// actor is run; otherwise all actors run together with combined responses fed back
        /// each iteration.
        /// </summary>
        public static List<string> HandleRunIterative(
            WallyEnvironment env, string prompt, string actorName = null, int maxIterationsOverride = 0)
        {
            if (RequireWorkspace(env, "run-iterative") == null) return new List<string>();

            if (maxIterationsOverride > 0)
                env.MaxIterations = maxIterationsOverride;

            if (!string.IsNullOrEmpty(actorName))
            {
                Console.WriteLine(
                    $"Running iterative loop on '{actorName}' (max {env.MaxIterations} iterations)...");

                string result = env.RunActorIterative(
                    prompt, actorName, maxIterationsOverride,
                    (iteration, response) =>
                    {
                        Console.WriteLine($"--- Iteration {iteration} [{actorName}] ---");
                        Console.WriteLine(response);
                    });

                return string.IsNullOrWhiteSpace(result)
                    ? new List<string>()
                    : new List<string> { $"{actorName}: {result}" };
            }

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

        // — Workspace inspection ——————————————————————————————————————————————

        /// <summary>
        /// Lists each loaded actor (name, role/criteria/intent prompts).
        /// </summary>
        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;

            var ws = env.Workspace!;

            Console.WriteLine($"Actors ({ws.Actors.Count}):");
            if (ws.Actors.Count == 0)
                Console.WriteLine($"  (none — add a subfolder with actor.json to {ws.WorkspaceFolder}/Actors/)");

            foreach (var actor in ws.Actors)
            {
                Console.WriteLine($"  [{actor.Name}]  folder: {actor.FolderPath}");
                PrintRbaLine("    Role",     actor.Role.Prompt);
                PrintRbaLine("    Criteria", actor.AcceptanceCriteria.Prompt);
                PrintRbaLine("    Intent",   actor.Intent.Prompt);
            }
        }

        /// <summary>Displays workspace paths, actor count, and settings.</summary>
        public static void HandleInfo(WallyEnvironment env)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine("Status:            No workspace loaded.");
                Console.WriteLine("                   Use 'load <path>' or 'create <path>' first.");
                return;
            }

            var ws  = env.Workspace!;
            var cfg = ws.Config;

            Console.WriteLine($"Status:            Workspace loaded");
            Console.WriteLine($"Workspace folder:  {ws.WorkspaceFolder}");
            Console.WriteLine($"Actors folder:     {Path.Combine(ws.WorkspaceFolder, cfg.ActorsFolderName)}");
            Console.WriteLine($"Actors loaded:     {ws.Actors.Count}");
            foreach (var a in ws.Actors)
                Console.WriteLine($"  {a.Name}");
            Console.WriteLine();
            Console.WriteLine($"Max iterations:    {env.MaxIterations}");
        }

        /// <summary>Re-reads actor folders from disk and rebuilds actors without a full reload.</summary>
        public static void HandleReloadActors(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "reload-actors") == null) return;
            env.ReloadActors();
            Console.WriteLine($"Actors reloaded: {env.Actors.Count}");
            foreach (var a in env.Actors)
                Console.WriteLine($"  {a.Name}");
        }

        // — Help ———————————————————————————————————————————————————————————————

        public static void HandleHelp()
        {
            Console.WriteLine("Wally — AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("No workspace required:");
            Console.WriteLine("  setup [-p <path>]              Scaffold or load a workspace. Defaults to <exeDir>/.wally.");
            Console.WriteLine("  create <path>                  Scaffold a new workspace at <path>.");
            Console.WriteLine("  load <path>                    Load an existing workspace from <path>.");
            Console.WriteLine("  info                           Show workspace info and actor list.");
            Console.WriteLine("  help                           Show this message.");
            Console.WriteLine();
            Console.WriteLine("Workspace required:");
            Console.WriteLine("  save <path>                    Save config and all actor.json files.");
            Console.WriteLine("  list                           List actors and their prompts.");
            Console.WriteLine("  reload-actors                  Re-read actor folders from disk, rebuild actors.");
            Console.WriteLine("  run \"<prompt>\" [actor]         Run all actors, or one by name.");
            Console.WriteLine("  run-iterative \"<prompt>\"       Run all actors iteratively; -m N to cap.");
            Console.WriteLine("  run-iterative \"<prompt>\" -a <actor>  Run one actor iteratively.");
            Console.WriteLine();
            Console.WriteLine("Workspace folder layout:");
            Console.WriteLine("  <WorkspaceFolder>/             e.g. .wally/");
            Console.WriteLine("    wally-config.json");
            Console.WriteLine("    Actors/");
            Console.WriteLine("      <ActorName>/");
            Console.WriteLine("        actor.json               name, rolePrompt,");
            Console.WriteLine("                                 criteriaPrompt, intentPrompt");
            Console.WriteLine();
            Console.WriteLine("Note: Copilot CLI uses the current working directory for file context.");
            Console.WriteLine("      Run wally from your project root so Copilot can see your code.");
        }

        // — Private helpers ———————————————————————————————————————————————————

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            Console.WriteLine(header);
            Console.WriteLine($"  Workspace: {env.WorkspaceFolder}");
            Console.WriteLine($"  Actors:    {env.Actors.Count}");
        }

        private static void PrintRbaLine(string label, string prompt)
        {
            string display = prompt?.Length > 80 ? prompt[..80] + "…" : prompt ?? "";
            Console.WriteLine($"{label}: {display}");
        }
    }
}