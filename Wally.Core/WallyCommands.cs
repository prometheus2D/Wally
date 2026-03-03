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
                    $"Use 'setup <path>' or 'load <path>' first.");
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

        /// <summary>
        /// Ensures a workspace exists at <paramref name="workSourcePath"/> (or the default
        /// location when <paramref name="workSourcePath"/> is null) and loads it.
        /// The <c>.wally/</c> workspace folder is created inside the WorkSource directory.
        /// When <paramref name="verifyOnly"/> is true, reports structural issues without
        /// making changes.
        /// </summary>
        public static void HandleSetup(WallyEnvironment env, string? workSourcePath = null, bool verifyOnly = false)
        {
            if (verifyOnly)
            {
                string wsFolder = ResolveWorkspaceFolder(workSourcePath);

                Console.WriteLine($"Verifying workspace at: {wsFolder}");
                Console.WriteLine();

                var issues = WallyHelper.CheckWorkspace(wsFolder);

                if (issues.Count == 0)
                {
                    Console.WriteLine("? Workspace structure is valid. No issues found.");
                }
                else
                {
                    Console.WriteLine($"Issues found ({issues.Count}):");
                    foreach (var issue in issues)
                        Console.WriteLine($"  {issue}");
                    Console.WriteLine();
                    Console.WriteLine("Run 'setup' without --verify to create/repair the workspace.");
                }

                env.Logger.LogCommand("setup", $"Verified workspace at {wsFolder}: {issues.Count} issue(s)");
                return;
            }

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

        /// <summary>
        /// Unified run command. Every execution goes through a loop — a single-shot
        /// run is simply a loop with <c>MaxIterations=1</c> (the <c>SingleRun</c> definition).
        /// <para>
        /// When <paramref name="looped"/> is <see langword="false"/> and no
        /// <paramref name="loopName"/> is specified, the <c>SingleRun</c> loop
        /// definition is used (one prompt ? one response).
        /// </para>
        /// <para>
        /// When <paramref name="looped"/> is <see langword="true"/> or a
        /// <paramref name="loopName"/> is given, the actor iterates until the
        /// response contains the completion keyword, the error keyword, or
        /// <paramref name="maxIterations"/> is reached.
        /// </para>
        /// <paramref name="wrapper"/> overrides <see cref="WallyConfig.DefaultWrapper"/>
        /// for this run, allowing the caller to choose between a read-only text
        /// response (e.g. <c>Copilot</c>) and agentic code/file changes
        /// (e.g. <c>AutoCopilot</c>).
        /// </summary>
        /// <param name="env">The Wally environment to operate on.</param>
        /// <param name="prompt">The user prompt.</param>
        /// <param name="actorName">The actor to run. When null in Autopilot-style use, all actors are run.</param>
        /// <param name="model">Model override. Null uses the workspace default.</param>
        /// <param name="looped">When true, iterate until completion (even without a named loop).</param>
        /// <param name="loopName">Name of a loop definition from Loops/. Implies looped. Null uses defaults.</param>
        /// <param name="maxIterations">Max iterations override. 0 uses the loop/workspace default.</param>
        /// <param name="wrapper">LLM wrapper override. Null uses the workspace default.</param>
        /// <returns>The list of result strings from all iterations.</returns>
        public static List<string> HandleRun(
            WallyEnvironment env,
            string prompt,
            string? actorName = null,
            string? model = null,
            bool looped = false,
            string? loopName = null,
            int maxIterations = 0,
            string? wrapper = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();

            // A named loop or explicit max > 1 implies --loop.
            bool isLooped = looped
                            || !string.IsNullOrWhiteSpace(loopName)
                            || maxIterations > 1;

            // — Resolve loop definition ———————————————————————————————————————

            WallyLoopDefinition? loopDef = null;

            if (!string.IsNullOrWhiteSpace(loopName))
            {
                loopDef = env.GetLoop(loopName!);
                if (loopDef == null)
                {
                    Console.WriteLine($"Loop definition '{loopName}' not found. Available loops:");
                    foreach (var l in env.Loops)
                        Console.WriteLine($"  {l.Name} — {l.Description}");
                    env.Logger.LogError($"Loop definition '{loopName}' not found.", "run");
                    return new List<string>();
                }
            }
            else if (!isLooped)
            {
                // Single-shot: use the SingleRun definition if available, otherwise synthesize.
                loopDef = env.GetLoop("SingleRun");
                // If no SingleRun on disk, create an inline equivalent.
                loopDef ??= new WallyLoopDefinition
                {
                    Name = "SingleRun",
                    Description = "Single-shot execution",
                    StartPrompt = "{userPrompt}",
                    MaxIterations = 1
                };
            }

            // The definition can specify its own actor — use it unless the caller provided one.
            if (string.IsNullOrWhiteSpace(actorName) && loopDef != null && !string.IsNullOrWhiteSpace(loopDef.ActorName))
                actorName = loopDef.ActorName;

            // — Resolve actor ————————————————————————————————————————————————

            if (string.IsNullOrWhiteSpace(actorName))
            {
                Console.WriteLine("No actor specified. Use: run <actor> \"<prompt>\"");
                env.Logger.LogError("No actor specified.", "run");
                return new List<string>();
            }

            var actor = env.GetActor(actorName!);
            if (actor == null)
            {
                Console.WriteLine($"Actor '{actorName}' not found.");
                env.Logger.LogError($"Actor '{actorName}' not found.", "run");
                return new List<string>();
            }

            // — Resolve iterations ————————————————————————————————————————————

            int iterations;
            if (maxIterations > 0)
                iterations = maxIterations;
            else if (loopDef != null && loopDef.MaxIterations > 0)
                iterations = loopDef.MaxIterations;
            else if (isLooped)
                iterations = env.Workspace!.Config.MaxIterations;
            else
                iterations = 1;

            // — Log ———————————————————————————————————————————————————————————

            string loopLabel = loopDef != null && loopDef.Name != "SingleRun"
                ? $"[run:{loopDef.Name}]"
                : "[run]";

            env.Logger.LogCommand("run",
                $"Actor='{actorName}' loop='{loopDef?.Name ?? "(inline)"}' " +
                $"iterations={iterations} model='{model ?? "(default)"}' " +
                $"wrapper='{wrapper ?? "(default)"}'"
            );

            // — Build the actor action ————————————————————————————————————————

            int iterationNumber = 0;
            string? resolvedModel = model ?? env.Workspace!.Config.DefaultModel;

            Func<string, string> actorAction = currentPrompt =>
            {
                iterationNumber++;
                env.Logger.LogPrompt(actor.Name, currentPrompt, resolvedModel);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                string result = env.ExecuteActor(actor, currentPrompt, model, wrapper);
                sw.Stop();

                env.Logger.LogResponse(actor.Name, result, sw.ElapsedMilliseconds, iterationNumber);
                return result;
            };

            // — Build the loop ———————————————————————————————————————————————

            WallyLoop loop;
            if (loopDef != null)
            {
                loop = WallyLoop.FromDefinition(loopDef, prompt, actorAction, iterations);
            }
            else
            {
                // Looped but no named definition — use inline continue prompts.
                Func<string, string> continuePrompt = previousResult =>
                    $"You are continuing a task. Here is your previous response:\n\n" +
                    $"---\n{previousResult}\n---\n\n" +
                    $"Continue where you left off. " +
                    $"If you are finished, respond with: {WallyLoop.CompletedKeyword}\n" +
                    $"If something went wrong, respond with: {WallyLoop.ErrorKeyword}";

                loop = new WallyLoop(actorAction, prompt, continuePrompt, iterations);
            }

            // — Execute ——————————————————————————————————————————————————————

            if (iterations > 1)
            {
                Console.WriteLine($"{loopLabel} Actor: {actor.Name}  MaxIterations: {iterations}");
                Console.WriteLine();
            }

            loop.Run();

            // — Output ———————————————————————————————————————————————————————

            if (iterations == 1 && loop.Results.Count == 1)
            {
                // Single-shot: print the response directly, no iteration wrappers.
                Console.WriteLine(loop.Results[0]);
                Console.WriteLine();
            }
            else
            {
                for (int i = 0; i < loop.Results.Count; i++)
                {
                    Console.WriteLine($"--- Iteration {i + 1} ---");
                    Console.WriteLine(loop.Results[i]);
                    Console.WriteLine();
                }

                switch (loop.StopReason)
                {
                    case LoopStopReason.Completed:
                        Console.WriteLine($"{loopLabel} Loop completed after {loop.ExecutionCount} iteration(s).");
                        break;
                    case LoopStopReason.Error:
                        Console.WriteLine($"{loopLabel} Loop stopped by error after {loop.ExecutionCount} iteration(s).");
                        break;
                    case LoopStopReason.MaxIterations:
                        Console.WriteLine($"{loopLabel} Loop reached max iterations ({loop.ExecutionCount}).");
                        break;
                }
            }

            if (loop.Results.Count == 0)
                Console.WriteLine("No responses from Actor.");

            env.Logger.LogInfo(
                $"run finished: {loop.ExecutionCount} iteration(s), stopReason={loop.StopReason}");

            return loop.Results;
        }

        // — Workspace inspection ——————————————————————————————————————————————

        /// <summary>
        /// Lists each loaded actor (name, role/criteria/intent prompts, docs folder paths).
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
                if (!string.IsNullOrEmpty(actor.FolderPath))
                {
                    string actorDocsPath = Path.Combine(actor.FolderPath, actor.DocsFolderName);
                    if (Directory.Exists(actorDocsPath))
                        Console.WriteLine($"    Docs folder: {actorDocsPath}");
                }
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
            Console.WriteLine($"Docs folder:       {Path.Combine(ws.WorkspaceFolder, cfg.DocsFolderName)}");
            Console.WriteLine($"Templates folder:  {Path.Combine(ws.WorkspaceFolder, cfg.TemplatesFolderName)}");
            Console.WriteLine($"Logs folder:       {Path.Combine(ws.WorkspaceFolder, cfg.LogsFolderName)}");
            Console.WriteLine($"Actors loaded:     {ws.Actors.Count}");
            foreach (var a in ws.Actors)
                Console.WriteLine($"  {a.Name}");

            Console.WriteLine($"Loops loaded:      {ws.Loops.Count}");
            foreach (var l in ws.Loops)
                Console.WriteLine($"  {l.Name}{(string.IsNullOrWhiteSpace(l.Description) ? "" : $" — {l.Description}")}");

            Console.WriteLine($"Wrappers loaded:  {ws.LlmWrappers.Count}");
            foreach (var w in ws.LlmWrappers)
                Console.WriteLine($"  {w.Name}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $" — {w.Description}")}");

            Console.WriteLine();

            Console.WriteLine($"Default wrapper:   {(string.IsNullOrWhiteSpace(cfg.DefaultWrapper) ? "Copilot" : cfg.DefaultWrapper)}");
            Console.WriteLine($"Default model:     {(string.IsNullOrWhiteSpace(cfg.DefaultModel) ? "(wrapper default)" : cfg.DefaultModel)}");
            if (cfg.DefaultModels.Count > 0)
                Console.WriteLine($"Default models:    {string.Join(", ", cfg.DefaultModels)}");
            if (cfg.DefaultWrappers.Count > 0)
                Console.WriteLine($"Default wrappers:  {string.Join(", ", cfg.DefaultWrappers)}");
            if (cfg.DefaultLoops.Count > 0)
                Console.WriteLine($"Default loops:     {string.Join(", ", cfg.DefaultLoops)}");

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

        /// <summary>Lists all loaded loops with their descriptions and settings.</summary>
        public static void HandleListLoops(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-loops") == null) return;
            env.Logger.LogCommand("list-loops");

            var loops = env.Loops;
            Console.WriteLine($"Loops ({loops.Count}):");
            if (loops.Count == 0)
            {
                Console.WriteLine($"  (none — add .json files to {env.WorkspaceFolder}/Loops/)");
                return;
            }

            foreach (var loop in loops)
            {
                Console.WriteLine($"  [{loop.Name}]");
                if (!string.IsNullOrWhiteSpace(loop.Description))
                    Console.WriteLine($"    Description:    {loop.Description}");
                Console.WriteLine($"    Actor:          {(string.IsNullOrWhiteSpace(loop.ActorName) ? "(caller must specify)" : loop.ActorName)}");
                Console.WriteLine($"    MaxIterations:  {(loop.MaxIterations > 0 ? loop.MaxIterations.ToString() : "(workspace default)")}");
                Console.WriteLine($"    Completed:      {loop.ResolvedCompletedKeyword}");
                Console.WriteLine($"    Error:          {loop.ResolvedErrorKeyword}");
                PrintRbaLine("    StartPrompt", loop.StartPrompt);
            }
        }

        // — Cleanup ———————————————————————————————————————————————————————————

        /// <summary>
        /// Deletes the local <c>.wally/</c> workspace folder so that
        /// <c>setup</c> can scaffold a fresh workspace. If the currently
        /// loaded workspace is the one being deleted, it is closed first.
        /// When <paramref name="workSourcePath"/> is <see langword="null"/>,
        /// the default location (exe directory) is used.
        /// </summary>
        public static void HandleCleanup(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder = ResolveWorkspaceFolder(workSourcePath);

            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine($"Nothing to clean — workspace folder does not exist: {wsFolder}");
                env.Logger.LogCommand("cleanup", $"No workspace at {wsFolder}");
                return;
            }

            // If the loaded workspace is the one we're about to delete, close it.
            if (env.HasWorkspace &&
                string.Equals(
                    Path.GetFullPath(env.Workspace!.WorkspaceFolder),
                    Path.GetFullPath(wsFolder),
                    StringComparison.OrdinalIgnoreCase))
            {
                env.CloseWorkspace();
                Console.WriteLine("Closed active workspace.");
            }

            try
            {
                Directory.Delete(wsFolder, recursive: true);
                Console.WriteLine($"Deleted workspace folder: {wsFolder}");
                Console.WriteLine("Run 'setup' to create a fresh workspace.");
                env.Logger.LogCommand("cleanup", $"Deleted {wsFolder}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cleanup failed: {ex.Message}");
                env.Logger.LogError($"Cleanup failed: {ex.Message}", "cleanup");
            }
        }

        // — Help ———————————————————————————————————————————————————————————————

        public static void HandleHelp()
        {
            Console.WriteLine("Wally — AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("Quick start:");
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine("  cd C:\\repos\\MyApp");
            Console.WriteLine("  .\\wally run Engineer \"Review the auth module and document the architecture\"");
            Console.WriteLine("  .\\wally run BusinessAnalyst \"Write requirements for the search feature\"");
            Console.WriteLine("  .\\wally run Stakeholder \"Define what the payment system must achieve\"");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine();
            Console.WriteLine("  setup [<path>] [-w <path>]       Set up a workspace at <path> (your codebase root).");
            Console.WriteLine("                                   Creates .wally/ inside it with config, actors, and templates.");
            Console.WriteLine("                                   Copies the wally exe so you can run .\\wally from that directory.");
            Console.WriteLine("                                   Defaults to the exe directory. Re-running repairs missing structure.");
            Console.WriteLine("    --verify                       Check workspace structure without making changes.");
            Console.WriteLine("  load <path>                      Load an existing .wally/ workspace folder.");
            Console.WriteLine("  info                             Show workspace paths, actors, model config, and session info.");
            Console.WriteLine("  list                             List all actors and their RBA prompts.");
            Console.WriteLine("  list-loops                       List all loops and their settings.");
            Console.WriteLine("  commands                         Show this help message.");
            Console.WriteLine();
            Console.WriteLine("  run <actor> \"<prompt>\" [options]  Run an actor on a prompt.");
            Console.WriteLine("    -m, --model <model>            Override the AI model (e.g. claude-sonnet-4).");
            Console.WriteLine("    -w, --wrapper <name>           Override the LLM wrapper (e.g. AutoCopilot).");
            Console.WriteLine("    --loop                         Run in iterative loop mode.");
            Console.WriteLine("    -l, --loop-name <name>         Use a named loop definition from Loops/.");
            Console.WriteLine("    -n, --max-iterations <n>       Maximum iterations (implies --loop when > 1).");
            Console.WriteLine();
            Console.WriteLine("  save <path>                      Save config and actor files to disk.");
            Console.WriteLine("  reload-actors                    Re-read actor folders from disk.");
            Console.WriteLine("  cleanup [<path>]                 Delete the local .wally/ folder so setup can run fresh.");
            Console.WriteLine();
            Console.WriteLine("Default actors:");
            Console.WriteLine("  Engineer         — code reviews, architecture docs, proposals, bug reports, test plans");
            Console.WriteLine("  BusinessAnalyst  — requirements, execution plans, project status");
            Console.WriteLine("  Stakeholder      — business needs, priorities, success criteria");
            Console.WriteLine();
            Console.WriteLine("Wrappers:");
            Console.WriteLine("  Copilot          — read-only, returns a text response (default)");
            Console.WriteLine("  AutoCopilot      — agentic, can make code/file changes on disk");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  .\\wally run Engineer \"Review the data access layer for bugs\"");
            Console.WriteLine("  .\\wally run Engineer \"Refactor error handling\" --loop -n 5");
            Console.WriteLine("  .\\wally run Engineer \"Review the auth module\" -l CodeReview");
            Console.WriteLine("  .\\wally run Engineer \"Fix the login bug\" -w AutoCopilot");
            Console.WriteLine("  .\\wally run Engineer \"Explain this module\" -m claude-sonnet-4");
            Console.WriteLine("  .\\wally run BusinessAnalyst \"Write requirements for user authentication\"");
        }

        // — Private helpers ———————————————————————————————————————————————————

        /// <summary>
        /// Resolves a WorkSource path to the <c>.wally/</c> workspace folder path,
        /// applying the same logic as <see cref="WallyEnvironment.SetupLocal"/>.
        /// </summary>
        private static string ResolveWorkspaceFolder(string? workSourcePath)
        {
            if (workSourcePath != null)
            {
                if (!Path.IsPathRooted(workSourcePath))
                    workSourcePath = Path.Combine(WallyHelper.GetExeDirectory(), workSourcePath);
                return Path.Combine(Path.GetFullPath(workSourcePath), WallyHelper.DefaultWorkspaceFolderName);
            }
            return WallyHelper.GetDefaultWorkspaceFolder();
        }

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            Console.WriteLine(header);
            Console.WriteLine($"  WorkSource: {env.WorkSource}");
            Console.WriteLine($"  Workspace:  {env.WorkspaceFolder}");
            Console.WriteLine($"  Actors:     {env.Actors.Count}");
        }

        private static void PrintRbaLine(string label, string prompt)
        {
            string display = prompt?.Length > 80 ? prompt[..80] + "\u2026" : prompt ?? "";
            Console.WriteLine($"{label}: {display}");
        }
    }
}