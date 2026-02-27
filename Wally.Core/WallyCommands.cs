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
                env.Logger.LogError($"No workspace loaded for command '{commandName}'.", commandName);
                return null;
            }
            return env;
        }

        // — Workspace lifecycle ———————————————————————————————————————————————

        /// <summary>Loads a workspace from <paramref name="path"/> into <paramref name="env"/>.</summary>
        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            env.Logger.LogCommand("load", $"Loaded workspace from {path}");
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        /// <summary>Scaffolds a new workspace at <paramref name="path"/> and loads it.</summary>
        public static void HandleCreate(WallyEnvironment env, string path)
        {
            // If relative, resolve against the exe directory.
            if (!Path.IsPathRooted(path))
                path = Path.Combine(WallyHelper.GetExeDirectory(), path);

            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(fullPath);
            string workspaceFolder = Path.Combine(fullPath, WallyHelper.DefaultWorkspaceFolderName);
            env.CreateWorkspace(workspaceFolder, WallyHelper.ResolveConfig(workspaceFolder));
            env.Logger.LogCommand("create", $"Created workspace at {workspaceFolder}");
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
            env.Logger.LogCommand("setup", $"Workspace ready at {env.WorkspaceFolder}");
            PrintWorkspaceSummary("Workspace ready.", env);
        }

        /// <summary>Saves the active workspace config and all actor files to <paramref name="path"/>.</summary>
        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            env.Logger.LogCommand("save", $"Saved workspace to {path}");
            Console.WriteLine($"Workspace saved to: {path}");
        }

        // — Running actors ————————————————————————————————————————————————————

        public static List<string> HandleRun(WallyEnvironment env, string prompt, string actorName = null, string model = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();

            env.Logger.LogCommand("run", actorName != null
                ? $"Running actor '{actorName}' with model override '{model ?? "(none)"}'"
                : $"Running all actors with model override '{model ?? "(none)"}'"
            );

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

            var responses = !string.IsNullOrEmpty(actorName)
                ? env.RunActor(prompt, actorName)
                : env.RunActors(prompt);

            // Print a role header before each response for console context.
            foreach (var response in responses)
            {
                Console.WriteLine(response);
                Console.WriteLine();
            }

            if (responses.Count == 0)
                Console.WriteLine("No responses from Actors.");

            return responses;
        }

        /// <summary>
        /// Runs an actor inside a <see cref="WallyLoop"/>. The actor generates
        /// the start and continue prompts from the user's raw input, wrapping it
        /// in its RBA context. The loop iterates until the actor's response
        /// contains <see cref="WallyLoop.CompletedKeyword"/>, 
        /// <see cref="WallyLoop.ErrorKeyword"/>, or
        /// <paramref name="maxIterations"/> is reached.
        /// <para>
        /// When the actor is a <see cref="CopilotActor"/>, a shared Copilot
        /// session ID is assigned so all iterations continue the same
        /// conversation — the LLM retains full context across iterations.
        /// </para>
        /// </summary>
        public static List<string> HandleRunLoop(
            WallyEnvironment env, string prompt, string actorName,
            string model = null, int maxIterations = 0)
        {
            if (RequireWorkspace(env, "run-loop") == null) return new List<string>();

            var actor = env.GetActor(actorName);
            if (actor == null)
            {
                Console.WriteLine($"Actor '{actorName}' not found.");
                env.Logger.LogError($"Actor '{actorName}' not found.", "run-loop");
                return new List<string>();
            }

            // Apply per-run model override.
            if (!string.IsNullOrWhiteSpace(model))
                actor.ModelOverride = model;

            int iterations = maxIterations > 0
                ? maxIterations
                : env.Workspace!.Config.MaxIterations;

            env.Logger.LogCommand("run-loop",
                $"Running actor '{actorName}' in loop (max {iterations}) " +
                $"with model override '{model ?? "(none)"}'"
            );

            // If the actor is a CopilotActor, assign a shared session ID so all
            // iterations continue the same Copilot conversation with full context.
            string? sessionId = null;
            if (actor is CopilotActor copilotActor)
            {
                sessionId = Guid.NewGuid().ToString();
                copilotActor.CopilotSessionId = sessionId;
            }

            // The actor generates the full prompt (RBA wrapper + user input).
            string startPrompt    = actor.GeneratePrompt(prompt);
            string continuePrompt = actor.GeneratePrompt(
                $"Continue the previous task. If you are finished, respond with: {WallyLoop.CompletedKeyword}\n" +
                $"If something went wrong, respond with: {WallyLoop.ErrorKeyword}");

            var loop = new WallyLoop(
                action:          currentPrompt => actor.Act(currentPrompt),
                startPrompt:     startPrompt,
                continuePrompt:  continuePrompt,
                maxIterations:   iterations
            );

            Console.WriteLine($"[run-loop] Actor: {actor.Name}  MaxIterations: {iterations}");
            if (sessionId != null)
                Console.WriteLine($"[run-loop] Copilot session: {sessionId}");
            Console.WriteLine();

            loop.Run();

            // Clean up the session so it doesn't leak into subsequent runs.
            if (actor is CopilotActor ca)
                ca.ResetSession();

            // Print each iteration result.
            for (int i = 0; i < loop.Results.Count; i++)
            {
                Console.WriteLine($"--- Iteration {i + 1} ---");
                Console.WriteLine(loop.Results[i]);
                Console.WriteLine();
            }

            // Summary based on stop reason.
            switch (loop.StopReason)
            {
                case LoopStopReason.Completed:
                    Console.WriteLine($"[run-loop] Loop completed after {loop.ExecutionCount} iteration(s).");
                    break;
                case LoopStopReason.Error:
                    Console.WriteLine($"[run-loop] Loop stopped by error after {loop.ExecutionCount} iteration(s).");
                    break;
                case LoopStopReason.MaxIterations:
                    Console.WriteLine($"[run-loop] Loop reached max iterations ({loop.ExecutionCount}).");
                    break;
            }

            env.Logger.LogInfo(
                $"run-loop finished: {loop.ExecutionCount} iteration(s), " +
                $"stopReason={loop.StopReason}");

            return loop.Results;
        }

        // — Workspace inspection ——————————————————————————————————————————————

        /// <summary>
        /// Lists each loaded actor (name, role/criteria/intent prompts).
        /// </summary>
        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;
            env.Logger.LogCommand("list");

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
            env.Logger.LogCommand("info");

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
            Console.WriteLine($"Logs folder:       {Path.Combine(ws.WorkspaceFolder, cfg.LogsFolderName)}");
            Console.WriteLine($"Actors loaded:     {ws.Actors.Count}");
            foreach (var a in ws.Actors)
                Console.WriteLine($"  {a.Name}");
            Console.WriteLine();
            Console.WriteLine($"Default model:     {(string.IsNullOrWhiteSpace(cfg.DefaultModel) ? "(copilot default)" : cfg.DefaultModel)}");
            if (cfg.Models.Count > 0)
            {
                Console.WriteLine($"Available models:  {string.Join(", ", cfg.Models)}");
            }
            Console.WriteLine();
            Console.WriteLine($"Session ID:        {env.Logger.SessionId:N}");
            Console.WriteLine($"Session started:   {env.Logger.StartedAt:u}");
            Console.WriteLine($"Session log:       {env.Logger.LogFolder ?? "(not bound — no workspace loaded)"}");
            Console.WriteLine($"Current log file:  {env.Logger.CurrentLogFile ?? "(none)"}");
            Console.WriteLine($"Log rotation:      {(cfg.LogRotationMinutes > 0 ? $"every {cfg.LogRotationMinutes} min" : "disabled")}");
        }

        /// <summary>Re-reads actor folders from disk and rebuilds actors without a full reload.</summary>
        public static void HandleReloadActors(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "reload-actors") == null) return;
            env.ReloadActors();
            env.Logger.LogCommand("reload-actors", $"Reloaded {env.Actors.Count} actors");
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
            Console.WriteLine("  setup [<path>] [-w <path>]       Scaffold or load a workspace. <path> is the WorkSource");
            Console.WriteLine("                                   directory (your codebase root). .wally/ is created inside it.");
            Console.WriteLine("                                   -w / --worksource is an explicit alternative to the positional arg.");
            Console.WriteLine("                                   If <path> doesn't exist, it is created automatically.");
            Console.WriteLine("                                   Defaults to the exe directory when omitted.");
            Console.WriteLine("  create <path>                    Scaffold a new workspace inside <path>/.wally/.");
            Console.WriteLine("                                   Creates <path> if it doesn't exist.");
            Console.WriteLine("  load <path>                      Load an existing workspace from <path> (.wally/ folder).");
            Console.WriteLine("  info                             Show workspace info, session info, and actor list.");
            Console.WriteLine("  help                             Show this message.");
            Console.WriteLine();
            Console.WriteLine("Workspace required:");
            Console.WriteLine("  save <path>                      Save config and all actor.json files.");
            Console.WriteLine("  list                             List actors and their prompts.");
            Console.WriteLine("  reload-actors                    Re-read actor folders from disk, rebuild actors.");
            Console.WriteLine("  run <actor> \"<prompt>\" [-m <model>]  Run a specific actor by name.");
            Console.WriteLine("                                   Use -m default to use the configured DefaultModel.");
            Console.WriteLine("  run-loop <actor> \"<prompt>\" [-m <model>] [-n <max>]");
            Console.WriteLine("                                   Run an actor in an iterative loop.");
            Console.WriteLine("                                   The actor generates wrapped prompts from its RBA context.");
            Console.WriteLine("                                   Loop ends when the actor responds with [LOOP COMPLETED],");
            Console.WriteLine("                                   [LOOP ERROR], or max iterations (-n) is reached.");
            Console.WriteLine();
            Console.WriteLine("Workspace layout:");
            Console.WriteLine("  <WorkSource>/                  Your codebase root (e.g. C:\\repos\\MyApp)");
            Console.WriteLine("    .wally/                      Workspace folder (config + actors + logs)");
            Console.WriteLine("      wally-config.json          DefaultModel, Models, LogsFolderName");
            Console.WriteLine("      Actors/");
            Console.WriteLine("        <ActorName>/");
            Console.WriteLine("          actor.json             name, rolePrompt,");
            Console.WriteLine("                                 criteriaPrompt, intentPrompt");
            Console.WriteLine("      Logs/                      Session logs (auto-created on first run)");
            Console.WriteLine("        <timestamp_guid>/        One folder per session");
            Console.WriteLine("          <timestamp>.txt         Rotated log files");
            Console.WriteLine();
            Console.WriteLine("Logging:      All commands, prompts, and responses are logged per session.");
            Console.WriteLine("              Log files rotate every LogRotationMinutes (default: 2 min).");
            Console.WriteLine("              Set to 0 in wally-config.json to disable rotation.");
            Console.WriteLine();
            Console.WriteLine("WorkSource:   The directory whose files are given as context to gh copilot.");
            Console.WriteLine("              This is always the parent of the .wally/ workspace folder.");
            Console.WriteLine("              Set via 'setup <path>' where <path> is your codebase root.");
            Console.WriteLine("              If the directory doesn't exist, it is created automatically.");
            Console.WriteLine();
            Console.WriteLine("DefaultModel: The LLM model Copilot uses (--model flag).");
            Console.WriteLine("              Set DefaultModel in wally-config.json.");
            Console.WriteLine();
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