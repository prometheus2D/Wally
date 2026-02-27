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
                    $"Use 'load <path>' or 'setup <path>' first.");
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
            // path is the WorkSource directory — workspace goes inside <path>/.wally
            string workspaceFolder = Path.Combine(Path.GetFullPath(path), WallyHelper.DefaultWorkspaceFolderName);
            env.CreateWorkspace(workspaceFolder, WallyHelper.ResolveConfig(workspaceFolder));
            PrintWorkspaceSummary("Workspace created.", env);
        }

        /// <summary>
        /// Ensures a workspace exists at <paramref name="workSourcePath"/> (or the default
        /// location when <paramref name="workSourcePath"/> is null) and loads it.
        /// The <c>.wally/</c> workspace folder is created inside the WorkSource directory.
        /// </summary>
        public static void HandleSetup(WallyEnvironment env, string workSourcePath = null)
        {
            env.SetupLocal(workSourcePath);
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

        public static List<string> HandleRun(WallyEnvironment env, string prompt, string actorName = null, string model = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();

            // Apply per-run model override to the target actor(s).
            if (!string.IsNullOrWhiteSpace(model))
            {
                if (!string.IsNullOrEmpty(actorName))
                {
                    var actor = env.GetActor(actorName);
                    if (actor != null) actor.ModelOverride = model;
                }
                else
                {
                    foreach (var a in env.Actors) a.ModelOverride = model;
                }
            }

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
            WallyEnvironment env, string prompt, string actorName = null, int maxIterationsOverride = 0, string model = null)
        {
            if (RequireWorkspace(env, "run-iterative") == null) return new List<string>();

            if (maxIterationsOverride > 0)
                env.MaxIterations = maxIterationsOverride;

            // Apply per-run model override to the target actor(s).
            if (!string.IsNullOrWhiteSpace(model))
            {
                if (!string.IsNullOrEmpty(actorName))
                {
                    var actor = env.GetActor(actorName);
                    if (actor != null) actor.ModelOverride = model;
                }
                else
                {
                    foreach (var a in env.Actors) a.ModelOverride = model;
                }
            }

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
                Console.WriteLine("                   Use 'load <path>' or 'setup <path>' first.");
                return;
            }

            var ws  = env.Workspace!;
            var cfg = ws.Config;

            Console.WriteLine($"Status:            Workspace loaded");
            Console.WriteLine($"WorkSource:        {ws.WorkSource}");
            Console.WriteLine($"Workspace folder:  {ws.WorkspaceFolder}");
            Console.WriteLine($"Actors folder:     {Path.Combine(ws.WorkspaceFolder, cfg.ActorsFolderName)}");
            Console.WriteLine($"Actors loaded:     {ws.Actors.Count}");
            foreach (var a in ws.Actors)
                Console.WriteLine($"  {a.Name}");
            Console.WriteLine();
            Console.WriteLine($"Default model:     {(string.IsNullOrWhiteSpace(cfg.DefaultModel) ? "(copilot default)" : cfg.DefaultModel)}");
            if (cfg.Models.Count > 0)
            {
                Console.WriteLine($"Available models:  {string.Join(", ", cfg.Models)}");
            }
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
            Console.WriteLine("  setup [<path>]                   Scaffold or load a workspace. <path> is the WorkSource");
            Console.WriteLine("                                   directory (your codebase root). .wally/ is created inside it.");
            Console.WriteLine("                                   If <path> doesn't exist, it is created automatically.");
            Console.WriteLine("                                   Defaults to the exe directory when omitted.");
            Console.WriteLine("  create <path>                    Scaffold a new workspace inside <path>/.wally/.");
            Console.WriteLine("                                   Creates <path> if it doesn't exist.");
            Console.WriteLine("  load <path>                      Load an existing workspace from <path> (.wally/ folder).");
            Console.WriteLine("  info                             Show workspace info, model config, and actor list.");
            Console.WriteLine("  help                             Show this message.");
            Console.WriteLine();
            Console.WriteLine("Workspace required:");
            Console.WriteLine("  save <path>                      Save config and all actor.json files.");
            Console.WriteLine("  list                             List actors and their prompts.");
            Console.WriteLine("  reload-actors                    Re-read actor folders from disk, rebuild actors.");
            Console.WriteLine("  run <actor> \"<prompt>\" [-m <model>]  Run a specific actor by name.");
            Console.WriteLine("                                   Use -m default to use the configured DefaultModel.");
            Console.WriteLine("  run-iterative \"<prompt>\" [--model <model>] [-m N]");
            Console.WriteLine("                                   Run all actors iteratively; -m N to cap.");
            Console.WriteLine("  run-iterative \"<prompt>\" -a <actor> [--model <model>] [-m N]");
            Console.WriteLine("                                   Run one actor iteratively.");
            Console.WriteLine();
            Console.WriteLine("Workspace layout:");
            Console.WriteLine("  <WorkSource>/                  Your codebase root (e.g. C:\\repos\\MyApp)");
            Console.WriteLine("    .wally/                      Workspace folder (config + actors)");
            Console.WriteLine("      wally-config.json          DefaultModel, Models, MaxIterations");
            Console.WriteLine("      Actors/");
            Console.WriteLine("        <ActorName>/");
            Console.WriteLine("          actor.json             name, rolePrompt,");
            Console.WriteLine("                                 criteriaPrompt, intentPrompt");
            Console.WriteLine();
            Console.WriteLine("WorkSource:   The directory whose files are given as context to gh copilot.");
            Console.WriteLine("              This is always the parent of the .wally/ workspace folder.");
            Console.WriteLine("              Set via 'setup <path>' where <path> is your codebase root.");
            Console.WriteLine("              If the directory doesn't exist, it is created automatically.");
            Console.WriteLine();
            Console.WriteLine("DefaultModel: The LLM model Copilot uses (--model flag).");
            Console.WriteLine("              Set DefaultModel in wally-config.json.");
            Console.WriteLine("Models:       List of available/allowed model identifiers.");
            Console.WriteLine("              Run 'gh copilot -- --help' and check --model choices.");
        }

        // — Private helpers ———————————————————————————————————————————————————

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            Console.WriteLine(header);
            Console.WriteLine($"  WorkSource: {env.WorkSource}");
            Console.WriteLine($"  Workspace:  {env.WorkspaceFolder}");
            Console.WriteLine($"  Actors:     {env.Actors.Count}");
        }

        private static void PrintRbaLine(string label, string prompt)
        {
            string display = prompt?.Length > 80 ? prompt[..80] + "…" : prompt ?? "";
            Console.WriteLine($"{label}: {display}");
        }
    }
}