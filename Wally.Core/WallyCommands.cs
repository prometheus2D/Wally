using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // — Constants ————————————————————————————————————————————————————

        /// <summary>Maximum runbook nesting depth to prevent infinite recursion.</summary>
        private const int MaxRunbookDepth = 10;

        // — Guard ————————————————————————————————————————————————————————

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

        // — Shared arg splitting ————————————————————————————————————————

        /// <summary>
        /// Splits a raw input line into arguments, respecting double-quoted strings.
        /// <c>run "Add input validation" Developer</c> ?
        /// <c>["run", "Add input validation", "Developer"]</c>.
        /// </summary>
        public static string[] SplitArgs(string input)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in input)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) args.Add(current.ToString());
            return args.ToArray();
        }

        // — Shared command dispatcher ———————————————————————————————————

        /// <summary>
        /// Routes a parsed argument array to the correct handler.
        /// Returns <see langword="true"/> on success, <see langword="false"/> on error.
        /// This is the single dispatch point used by Console interactive mode,
        /// Forms CommandPanel, and runbook execution.
        /// </summary>
        public static bool DispatchCommand(WallyEnvironment env, string[] args, int runbookDepth = 0)
        {
            if (args.Length == 0) return true;

            string verb = args[0].ToLowerInvariant();

            switch (verb)
            {
                case "setup":
                {
                    bool verify = HasFlag(args, "--verify");
                    string? path = GetFirstPositional(args, 1);
                    HandleSetup(env, path, verify);
                    return true;
                }

                case "load":
                    if (args.Length < 2) { Console.WriteLine("Usage: load <path>"); return false; }
                    HandleLoad(env, args[1]);
                    return true;

                case "save":
                    if (args.Length < 2) { Console.WriteLine("Usage: save <path>"); return false; }
                    HandleSave(env, args[1]);
                    return true;

                case "run":
                {
                    if (args.Length < 3) { Console.WriteLine("Usage: run <actor> \"<prompt>\" [-m model] [-w wrapper] [--loop] [-l name] [-n max]"); return false; }
                    string? model = GetOption(args, "-m") ?? GetOption(args, "--model");
                    string? wrapper = GetOption(args, "-w") ?? GetOption(args, "--wrapper");
                    string? loopName = GetOption(args, "-l") ?? GetOption(args, "--loop-name");
                    string? maxStr = GetOption(args, "-n") ?? GetOption(args, "--max-iterations");
                    bool looped = HasFlag(args, "--loop");
                    int maxIter = int.TryParse(maxStr, out int n) ? n : 0;
                    HandleRun(env, args[2], args[1], model, looped, loopName, maxIter, wrapper);
                    return true;
                }

                case "runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: runbook <name> [\"<prompt>\"]"); return false; }
                    string? prompt = args.Length >= 3 ? args[2] : null;
                    return HandleRunbook(env, args[1], prompt, runbookDepth);
                }

                case "list":
                    HandleList(env);
                    return true;

                case "list-loops":
                    HandleListLoops(env);
                    return true;

                case "list-runbooks":
                    HandleListRunbooks(env);
                    return true;

                case "info":
                    HandleInfo(env);
                    return true;

                case "reload-actors":
                    HandleReloadActors(env);
                    return true;

                case "cleanup":
                {
                    string? cleanupPath = GetFirstPositional(args, 1);
                    HandleCleanup(env, cleanupPath);
                    return true;
                }

                case "commands" or "help":
                    HandleHelp();
                    return true;

                default:
                    Console.WriteLine($"Unknown command: {verb}. Type 'commands' for help.");
                    return false;
            }
        }

        // — Workspace lifecycle —————————————————————————————————————————

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
                    Console.WriteLine("\u2713 Workspace structure is valid. No issues found.");
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

        // — Running actors ——————————————————————————————————————————————

        /// <summary>
        /// Unified run command. Every execution goes through a loop — a single-shot
        /// run is simply a loop with <c>MaxIterations=1</c> (the <c>SingleRun</c> definition).
        /// </summary>
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

            // — Resolve loop definition ——————————————————————————————————

            WallyLoopDefinition? loopDef = null;

            if (!string.IsNullOrWhiteSpace(loopName))
            {
                loopDef = env.GetLoop(loopName!);
                if (loopDef == null)
                {
                    Console.WriteLine($"Loop definition '{loopName}' not found. Available loops:");
                    foreach (var l in env.Loops)
                        Console.WriteLine($"  {l.Name} \u2014 {l.Description}");
                    env.Logger.LogError($"Loop definition '{loopName}' not found.", "run");
                    return new List<string>();
                }
            }
            else if (!isLooped)
            {
                loopDef = env.GetLoop("SingleRun");
                loopDef ??= new WallyLoopDefinition
                {
                    Name = "SingleRun",
                    Description = "Single-shot execution",
                    StartPrompt = "{userPrompt}",
                    MaxIterations = 1
                };
            }

            if (string.IsNullOrWhiteSpace(actorName) && loopDef != null && !string.IsNullOrWhiteSpace(loopDef.ActorName))
                actorName = loopDef.ActorName;

            // — Resolve actor ————————————————————————————————————————————

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

            // — Resolve iterations ——————————————————————————————————————

            int iterations;
            if (maxIterations > 0)
                iterations = maxIterations;
            else if (loopDef != null && loopDef.MaxIterations > 0)
                iterations = loopDef.MaxIterations;
            else if (isLooped)
                iterations = env.Workspace!.Config.MaxIterations;
            else
                iterations = 1;

            // — Log —————————————————————————————————————————————————————

            string loopLabel = loopDef != null && loopDef.Name != "SingleRun"
                ? $"[run:{loopDef.Name}]"
                : "[run]";

            env.Logger.LogCommand("run",
                $"Actor='{actorName}' loop='{loopDef?.Name ?? "(inline)"}' " +
                $"iterations={iterations} model='{model ?? "(default)"}' " +
                $"wrapper='{wrapper ?? "(default)"}'"
            );

            // — Build the actor action ——————————————————————————————————

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

            // — Build the loop ——————————————————————————————————————————

            WallyLoop loop;
            if (loopDef != null)
            {
                loop = WallyLoop.FromDefinition(loopDef, prompt, actorAction, iterations);
            }
            else
            {
                Func<string, string> continuePrompt = previousResult =>
                    $"You are continuing a task. Here is your previous response:\n\n" +
                    $"---\n{previousResult}\n---\n\n" +
                    $"Continue where you left off. " +
                    $"If you are finished, respond with: {WallyLoop.CompletedKeyword}\n" +
                    $"If something went wrong, respond with: {WallyLoop.ErrorKeyword}";

                loop = new WallyLoop(actorAction, prompt, continuePrompt, iterations);
            }

            // — Execute ———————————————————————————————————————————————

            if (iterations > 1)
            {
                Console.WriteLine($"{loopLabel} Actor: {actor.Name}  MaxIterations: {iterations}");
                Console.WriteLine();
            }

            loop.Run();

            // — Output —————————————————————————————————————————————————

            if (iterations == 1 && loop.Results.Count == 1)
            {
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

        // — Runbooks ————————————————————————————————————————————————————

        /// <summary>
        /// Executes a runbook — a sequence of Wally commands from a <c>.wrb</c> file.
        /// Each line is dispatched through <see cref="DispatchCommand"/>,
        /// so runbooks use the exact same syntax as the terminal.
        /// Stops on the first error. Nesting is capped at <see cref="MaxRunbookDepth"/>.
        /// </summary>
        public static bool HandleRunbook(
            WallyEnvironment env,
            string runbookName,
            string? userPrompt = null,
            int depth = 0)
        {
            if (depth >= MaxRunbookDepth)
            {
                Console.WriteLine(
                    $"Runbook nesting depth exceeded (max {MaxRunbookDepth}). " +
                    "Check for circular runbook calls.");
                env.Logger.LogError($"Runbook depth exceeded for '{runbookName}'.", "runbook");
                return false;
            }

            if (RequireWorkspace(env, "runbook") == null) return false;

            var runbook = env.GetRunbook(runbookName);
            if (runbook == null)
            {
                Console.WriteLine($"Runbook '{runbookName}' not found. Available runbooks:");
                foreach (var r in env.Runbooks)
                    Console.WriteLine($"  {r.Name}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" \u2014 {r.Description}")}");
                env.Logger.LogError($"Runbook '{runbookName}' not found.", "runbook");
                return false;
            }

            env.Logger.LogCommand("runbook",
                $"Starting '{runbookName}' ({runbook.Commands.Count} commands, depth={depth})");

            Console.WriteLine($"[runbook] Executing '{runbookName}' ({runbook.Commands.Count} commands)");

            for (int i = 0; i < runbook.Commands.Count; i++)
            {
                string rawLine = runbook.Commands[i];

                // Substitute placeholders.
                string line = rawLine
                    .Replace("{workSourcePath}", env.WorkSource ?? "")
                    .Replace("{workspaceFolder}", env.WorkspaceFolder ?? "")
                    .Replace("{userPrompt}", userPrompt ?? "");

                Console.WriteLine($"[runbook:{runbookName}] ({i + 1}/{runbook.Commands.Count}) {line}");

                string[] cmdArgs = SplitArgs(line);
                bool success = DispatchCommand(env, cmdArgs, depth + 1);

                if (!success)
                {
                    Console.WriteLine($"[runbook:{runbookName}] Stopped at command {i + 1} due to error.");
                    env.Logger.LogError(
                        $"Runbook '{runbookName}' stopped at command {i + 1}: {line}", "runbook");
                    return false;
                }
            }

            Console.WriteLine($"[runbook:{runbookName}] Completed ({runbook.Commands.Count} commands).");
            env.Logger.LogInfo($"Runbook '{runbookName}' completed ({runbook.Commands.Count} commands).");
            return true;
        }

        /// <summary>Lists all loaded runbooks.</summary>
        public static void HandleListRunbooks(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-runbooks") == null) return;
            env.Logger.LogCommand("list-runbooks");

            var runbooks = env.Runbooks;
            Console.WriteLine($"Runbooks ({runbooks.Count}):");
            if (runbooks.Count == 0)
            {
                Console.WriteLine($"  (none \u2014 add .wrb files to {env.WorkspaceFolder}/Runbooks/)");
                return;
            }

            foreach (var rb in runbooks)
            {
                Console.WriteLine($"  [{rb.Name}]");
                if (!string.IsNullOrWhiteSpace(rb.Description))
                    Console.WriteLine($"    Description: {rb.Description}");
                Console.WriteLine($"    Commands:    {rb.Commands.Count}");
                Console.WriteLine($"    File:        {rb.FilePath}");
            }
        }

        // — Workspace inspection ————————————————————————————————————————

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
                Console.WriteLine($"  (none \u2014 add a subfolder with actor.json to {ws.WorkspaceFolder}/Actors/)");

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
                Console.WriteLine($"  {l.Name}{(string.IsNullOrWhiteSpace(l.Description) ? "" : $" \u2014 {l.Description}")}");

            Console.WriteLine($"Wrappers loaded:   {ws.LlmWrappers.Count}");
            foreach (var w in ws.LlmWrappers)
                Console.WriteLine($"  {w.Name}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $" \u2014 {w.Description}")}");

            Console.WriteLine($"Runbooks loaded:   {ws.Runbooks.Count}");
            foreach (var r in ws.Runbooks)
                Console.WriteLine($"  {r.Name}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" \u2014 {r.Description}")}");

            Console.WriteLine();

            Console.WriteLine($"Default wrapper:   {(string.IsNullOrWhiteSpace(cfg.DefaultWrapper) ? "Copilot" : cfg.DefaultWrapper)}");
            Console.WriteLine($"Default model:     {(string.IsNullOrWhiteSpace(cfg.DefaultModel) ? "(wrapper default)" : cfg.DefaultModel)}");
            if (cfg.DefaultModels.Count > 0)
                Console.WriteLine($"Default models:    {string.Join(", ", cfg.DefaultModels)}");
            if (cfg.DefaultWrappers.Count > 0)
                Console.WriteLine($"Default wrappers:  {string.Join(", ", cfg.DefaultWrappers)}");
            if (cfg.DefaultLoops.Count > 0)
                Console.WriteLine($"Default loops:     {string.Join(", ", cfg.DefaultLoops)}");
            if (cfg.DefaultRunbooks.Count > 0)
                Console.WriteLine($"Default runbooks:  {string.Join(", ", cfg.DefaultRunbooks)}");

            Console.WriteLine();
            Console.WriteLine($"Session ID:        {env.Logger.SessionId:N}");
            Console.WriteLine($"Session started:   {env.Logger.StartedAt:u}");
            Console.WriteLine($"Session log:       {env.Logger.LogFolder ?? "(not bound \u2014 no workspace loaded)"}");
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
                Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Loops/)");
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

        // — Cleanup —————————————————————————————————————————————————————

        /// <summary>
        /// Deletes the local <c>.wally/</c> workspace folder so that
        /// <c>setup</c> can scaffold a fresh workspace.
        /// </summary>
        public static void HandleCleanup(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder = ResolveWorkspaceFolder(workSourcePath);

            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine($"Nothing to clean \u2014 workspace folder does not exist: {wsFolder}");
                env.Logger.LogCommand("cleanup", $"No workspace at {wsFolder}");
                return;
            }

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

        // — Help ————————————————————————————————————————————————————————

        public static void HandleHelp()
        {
            Console.WriteLine("Wally \u2014 AI Actor Environment Manager");
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
            Console.WriteLine("  list-runbooks                    List all loaded runbook definitions.");
            Console.WriteLine("  commands                         Show this help message.");
            Console.WriteLine();
            Console.WriteLine("  run <actor> \"<prompt>\" [options]  Run an actor on a prompt.");
            Console.WriteLine("    -m, --model <model>            Override the AI model (e.g. claude-sonnet-4).");
            Console.WriteLine("    -w, --wrapper <name>           Override the LLM wrapper (e.g. AutoCopilot).");
            Console.WriteLine("    --loop                         Run in iterative loop mode.");
            Console.WriteLine("    -l, --loop-name <name>         Use a named loop definition from Loops/.");
            Console.WriteLine("    -n, --max-iterations <n>       Maximum iterations (implies --loop when > 1).");
            Console.WriteLine();
            Console.WriteLine("  runbook <name> [\"<prompt>\"]      Execute a runbook (.wrb command sequence).");
            Console.WriteLine();
            Console.WriteLine("  save <path>                      Save config and actor files to disk.");
            Console.WriteLine("  reload-actors                    Re-read actor folders from disk.");
            Console.WriteLine("  cleanup [<path>]                 Delete the local .wally/ folder so setup can run fresh.");
            Console.WriteLine();
            Console.WriteLine("Default actors:");
            Console.WriteLine("  Engineer         \u2014 code reviews, architecture docs, proposals, bug reports, test plans");
            Console.WriteLine("  BusinessAnalyst  \u2014 requirements, execution plans, project status");
            Console.WriteLine("  Stakeholder      \u2014 business needs, priorities, success criteria");
            Console.WriteLine();
            Console.WriteLine("Wrappers:");
            Console.WriteLine("  Copilot          \u2014 read-only, returns a text response (default)");
            Console.WriteLine("  AutoCopilot      \u2014 agentic, can make code/file changes on disk");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  .\\wally run Engineer \"Review the data access layer for bugs\"");
            Console.WriteLine("  .\\wally run Engineer \"Refactor error handling\" --loop -n 5");
            Console.WriteLine("  .\\wally run Engineer \"Review the auth module\" -l CodeReview");
            Console.WriteLine("  .\\wally run Engineer \"Fix the login bug\" -w AutoCopilot");
            Console.WriteLine("  .\\wally run Engineer \"Explain this module\" -m claude-sonnet-4");
            Console.WriteLine("  .\\wally run BusinessAnalyst \"Write requirements for user authentication\"");
            Console.WriteLine("  .\\wally runbook full-analysis \"Improve the logging module\"");
        }

        // — Private helpers ———————————————————————————————————————————————

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

        private static string? GetOption(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static bool HasFlag(string[] args, string flag) =>
            Array.Exists(args, a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        private static string? GetFirstPositional(string[] args, int startIndex)
        {
            for (int i = startIndex; i < args.Length; i++)
                if (!args[i].StartsWith('-'))
                    return args[i];
            return null;
        }
    }
}