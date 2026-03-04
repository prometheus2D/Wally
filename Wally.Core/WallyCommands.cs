using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Providers;
using Wally.Core.RBA;

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
                    if (args.Length < 2) { Console.WriteLine("Usage: run \"<prompt>\" [-a actor] [-m model] [-w wrapper] [--loop] [-l name] [-n max]"); return false; }
                    string? actorName = GetOption(args, "-a") ?? GetOption(args, "--actor");
                    string? model = GetOption(args, "-m") ?? GetOption(args, "--model");
                    string? wrapper = GetOption(args, "-w") ?? GetOption(args, "--wrapper");
                    string? loopName = GetOption(args, "-l") ?? GetOption(args, "--loop-name");
                    string? maxStr = GetOption(args, "-n") ?? GetOption(args, "--max-iterations");
                    bool looped = HasFlag(args, "--loop");
                    int maxIter = int.TryParse(maxStr, out int n) ? n : 0;
                    HandleRun(env, args[1], actorName, model, looped, loopName, maxIter, wrapper);
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

                case "list-wrappers":
                    HandleListWrappers(env);
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

                case "tutorial":
                    HandleTutorial();
                    return true;

                // — Actor CRUD ——————————————————————————————————————————
                case "add-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string? role = GetOption(args, "-r") ?? GetOption(args, "--role") ?? "";
                    string? criteria = GetOption(args, "-c") ?? GetOption(args, "--criteria") ?? "";
                    string? intent = GetOption(args, "-i") ?? GetOption(args, "--intent") ?? "";
                    HandleAddActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "edit-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string? role = GetOption(args, "-r") ?? GetOption(args, "--role");
                    string? criteria = GetOption(args, "-c") ?? GetOption(args, "--criteria");
                    string? intent = GetOption(args, "-i") ?? GetOption(args, "--intent");
                    HandleEditActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "delete-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-actor <name>"); return false; }
                    HandleDeleteActor(env, args[1]);
                    return true;
                }

                // — Loop CRUD ——————————————————————————————————————————
                case "add-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-loop <name> [-d desc] [-a actor] [-n max] [-s prompt]"); return false; }
                    string desc = GetOption(args, "-d") ?? GetOption(args, "--description") ?? "";
                    string actor = GetOption(args, "-a") ?? GetOption(args, "--actor") ?? "";
                    string? maxStr2 = GetOption(args, "-n") ?? GetOption(args, "--max-iterations");
                    int max = int.TryParse(maxStr2, out int m) ? m : 5;
                    string startPrompt = GetOption(args, "-s") ?? GetOption(args, "--start-prompt") ?? "{userPrompt}";
                    HandleAddLoop(env, args[1], desc, actor, max, startPrompt);
                    return true;
                }
                case "edit-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-loop <name> [-d desc] [-a actor] [-n max] [-s prompt]"); return false; }
                    string? desc = GetOption(args, "-d") ?? GetOption(args, "--description");
                    string? actor = GetOption(args, "-a") ?? GetOption(args, "--actor");
                    string? maxStr2 = GetOption(args, "-n") ?? GetOption(args, "--max-iterations");
                    int? max = int.TryParse(maxStr2, out int m) ? m : null;
                    string? startPrompt = GetOption(args, "-s") ?? GetOption(args, "--start-prompt");
                    HandleEditLoop(env, args[1], desc, actor, max, startPrompt);
                    return true;
                }
                case "delete-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-loop <name>"); return false; }
                    HandleDeleteLoop(env, args[1]);
                    return true;
                }

                // — Wrapper CRUD ————————————————————————————————————————
                case "add-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes]"); return false; }
                    string desc = GetOption(args, "-d") ?? GetOption(args, "--description") ?? "";
                    string exe = GetOption(args, "-e") ?? GetOption(args, "--executable") ?? "gh";
                    string template = GetOption(args, "-t") ?? GetOption(args, "--template") ?? "";
                    bool canChange = HasFlag(args, "--can-make-changes");
                    HandleAddWrapper(env, args[1], desc, exe, template, canChange);
                    return true;
                }
                case "edit-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes]"); return false; }
                    string? desc = GetOption(args, "-d") ?? GetOption(args, "--description");
                    string? exe = GetOption(args, "-e") ?? GetOption(args, "--executable");
                    string? template = GetOption(args, "-t") ?? GetOption(args, "--template");
                    bool? canChange = HasFlag(args, "--can-make-changes") ? true : null;
                    HandleEditWrapper(env, args[1], desc, exe, template, canChange);
                    return true;
                }
                case "delete-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-wrapper <name>"); return false; }
                    HandleDeleteWrapper(env, args[1]);
                    return true;
                }

                // — Runbook CRUD ————————————————————————————————————————
                case "add-runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-runbook <name> [-d desc]"); return false; }
                    string desc = GetOption(args, "-d") ?? GetOption(args, "--description") ?? "";
                    HandleAddRunbook(env, args[1], desc);
                    return true;
                }
                case "edit-runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-runbook <name> [-d desc]"); return false; }
                    string? desc = GetOption(args, "-d") ?? GetOption(args, "--description");
                    HandleEditRunbook(env, args[1], desc);
                    return true;
                }
                case "delete-runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-runbook <name>"); return false; }
                    HandleDeleteRunbook(env, args[1]);
                    return true;
                }

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
        /// When no actor is specified, the prompt is sent directly to the AI without
        /// any actor context (RBA enrichment is skipped).
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

            // — Resolve actor (null means direct/no-actor mode) ——————————

            Actor? actor = null;
            bool directMode = string.IsNullOrWhiteSpace(actorName);

            if (!directMode)
            {
                actor = env.GetActor(actorName!);
                if (actor == null)
                {
                    Console.WriteLine($"Actor '{actorName}' not found.");
                    env.Logger.LogError($"Actor '{actorName}' not found.", "run");
                    return new List<string>();
                }
            }

            // — Resolve iterations ———————————————————————————————————————

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

            string actorLabel = directMode ? "(no actor)" : actorName!;
            string loopLabel = loopDef != null && loopDef.Name != "SingleRun"
                ? $"[run:{loopDef.Name}]"
                : "[run]";

            env.Logger.LogCommand("run",
                $"Actor='{actorLabel}' loop='{loopDef?.Name ?? "(inline)"}' " +
                $"iterations={iterations} model='{model ?? "(default)"}' " +
                $"wrapper='{wrapper ?? "(default)"}'"
            );

            if (directMode)
            {
                Console.WriteLine($"{loopLabel} Running without actor context (direct mode).");
                Console.WriteLine();
            }

            // — Build the action ————————————————————————————————————————

            int iterationNumber = 0;
            string? resolvedModel = model ?? env.Workspace!.Config.DefaultModel;

            Func<string, string> runAction = currentPrompt =>
            {
                iterationNumber++;

                if (directMode)
                {
                    env.Logger.LogPrompt("(no actor)", currentPrompt, resolvedModel);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    string result = env.ExecutePrompt(currentPrompt, model, wrapper);
                    sw.Stop();
                    env.Logger.LogResponse("(no actor)", result, sw.ElapsedMilliseconds, iterationNumber);
                    return result;
                }
                else
                {
                    env.Logger.LogPrompt(actor!.Name, currentPrompt, resolvedModel);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    string result = env.ExecuteActor(actor!, currentPrompt, model, wrapper);
                    sw.Stop();
                    env.Logger.LogResponse(actor!.Name, result, sw.ElapsedMilliseconds, iterationNumber);
                    return result;
                }
            };

            // — Build the loop ——————————————————————————————————————————

            WallyLoop loop;
            if (loopDef != null)
            {
                loop = WallyLoop.FromDefinition(loopDef, prompt, runAction, iterations);
            }
            else
            {
                Func<string, string> continuePrompt = previousResult =>
                    $"You are continuing a task. Here is your previous response:\n\n" +
                    $"---\n{previousResult}\n---\n\n" +
                    $"Continue where you left off. " +
                    $"If you are finished, respond with: {WallyLoop.CompletedKeyword}\n" +
                    $"If something went wrong, respond with: {WallyLoop.ErrorKeyword}";

                loop = new WallyLoop(runAction, prompt, continuePrompt, iterations);
            }

            // — Execute ———————————————————————————————————————————————

            if (iterations > 1)
            {
                Console.WriteLine($"{loopLabel} {(directMode ? "Direct mode" : $"Actor: {actor!.Name}")}  MaxIterations: {iterations}");
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
                Console.WriteLine("No responses from AI.");

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
            Console.WriteLine("  .\\wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine("  .\\wally run \"Write requirements for the search feature\" -a BusinessAnalyst");
            Console.WriteLine("  .\\wally run \"What is this project about?\"              # no actor, direct AI prompt");
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
            Console.WriteLine("  tutorial                         Step-by-step guide to setting up and using Wally.");
            Console.WriteLine("  commands                         Show this help message.");
            Console.WriteLine();
            Console.WriteLine("  run \"<prompt>\" [options]          Run a prompt through the AI.");
            Console.WriteLine("    -a, --actor <name>             Run through an actor (adds RBA context). Omit for direct mode.");
            Console.WriteLine("    -m, --model <model>            Override the AI model (e.g. claude-sonnet-4).");
            Console.WriteLine("    -w, --wrapper <name>           Override the LLM wrapper (e.g. AutoCopilot).");
            Console.WriteLine("    --loop                         Run in iterative loop mode.");
            Console.WriteLine("    -l, --loop-name <name>         Use a named loop definition from Loops/.");
            Console.WriteLine("    -n, --max-iterations <n>       Maximum iterations (implies --loop when > 1).");
            Console.WriteLine();
            Console.WriteLine("  When no actor is specified, the prompt is sent directly to the AI without");
            Console.WriteLine("  any Role/AcceptanceCriteria/Intent context. This is useful for general");
            Console.WriteLine("  questions or tasks that don't need a specific persona.");
            Console.WriteLine();
            Console.WriteLine("  runbook <name> [\"<prompt>\"]      Execute a runbook (.wrb command sequence).");
            Console.WriteLine();
            Console.WriteLine("  Actors:");
            Console.WriteLine("    list                             List all actors and their RBA prompts.");
            Console.WriteLine("    add-actor <name> [-r] [-c] [-i]  Create a new actor.");
            Console.WriteLine("    edit-actor <name> [-r] [-c] [-i] Edit an actor's prompts.");
            Console.WriteLine("    delete-actor <name>              Delete an actor.");
            Console.WriteLine("    reload-actors                    Re-read actor folders from disk.");
            Console.WriteLine();
            Console.WriteLine("  Loops:");
            Console.WriteLine("    list-loops                       List all loop definitions.");
            Console.WriteLine("    add-loop <name> [-d] [-a] [-n] [-s]  Create a new loop.");
            Console.WriteLine("    edit-loop <name> [-d] [-a] [-n] [-s] Edit a loop definition.");
            Console.WriteLine("    delete-loop <name>               Delete a loop.");
            Console.WriteLine();
            Console.WriteLine("  Wrappers:");
            Console.WriteLine("    list-wrappers                    List all LLM wrapper definitions.");
            Console.WriteLine("    add-wrapper <name> [-d] [-e] [-t] [--can-make-changes]  Create a wrapper.");
            Console.WriteLine("    edit-wrapper <name> [-d] [-e] [-t] [--can-make-changes] Edit a wrapper.");
            Console.WriteLine("    delete-wrapper <name>            Delete a wrapper.");
            Console.WriteLine();
            Console.WriteLine("  Runbooks:");
            Console.WriteLine("    list-runbooks                    List all runbook definitions.");
            Console.WriteLine("    add-runbook <name> [-d]          Create a runbook scaffold.");
            Console.WriteLine("    edit-runbook <name> [-d]         Edit a runbook's description.");
            Console.WriteLine("    delete-runbook <name>            Delete a runbook.");
            Console.WriteLine();
            Console.WriteLine("  save <path>                      Save config and actor files to disk.");
            Console.WriteLine("  cleanup [<path>]                 Delete the local .wally/ folder so setup can run fresh.");
            Console.WriteLine();
            Console.WriteLine("Default actors:");
            Console.WriteLine("  Engineer         \u2014 code reviews, architecture docs, proposals, bug reports, test plans");
            Console.WriteLine("  BusinessAnalyst  \u2014 requirements, execution plans, project status");
            Console.WriteLine("  Stakeholder      \u2014 business needs, priorities, success criteria");
            Console.WriteLine("  (no actor)       \u2014 omit -a to send your prompt directly without any actor context");
            Console.WriteLine();
            Console.WriteLine("Wrappers:");
            Console.WriteLine("  Copilot          \u2014 read-only, returns a text response (default)");
            Console.WriteLine("  AutoCopilot      \u2014 agentic, can make code/file changes on disk");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  .\\wally run \"What does this codebase do?\"                     # direct, no actor");
            Console.WriteLine("  .\\wally run \"Review the data access layer for bugs\" -a Engineer");
            Console.WriteLine("  .\\wally run \"Refactor error handling\" -a Engineer --loop -n 5");
            Console.WriteLine("  .\\wally run \"Review the auth module\" -a Engineer -l CodeReview");
            Console.WriteLine("  .\\wally run \"Fix the login bug\" -a Engineer -w AutoCopilot");
            Console.WriteLine("  .\\wally run \"Explain this module\" -m claude-sonnet-4");
            Console.WriteLine("  .\\wally run \"Write requirements for user authentication\" -a BusinessAnalyst");
            Console.WriteLine("  .\\wally runbook full-analysis \"Improve the logging module\"");
            Console.WriteLine("  .\\wally add-actor QA -r \"You are a QA engineer\" -c \"Find all bugs\" -i \"Ensure quality\"");
            Console.WriteLine("  .\\wally add-loop BugHunt -d \"Bug hunting loop\" -a Engineer -n 5");
            Console.WriteLine("  .\\wally tutorial                   Step-by-step guide");
        }

        // — Tutorial ————————————————————————————————————————————————————

        /// <summary>
        /// Displays a step-by-step tutorial walking the user through Wally setup,
        /// basic usage, building custom objects, and advanced patterns.
        /// </summary>
        public static void HandleTutorial()
        {
            Console.WriteLine("Wally \u2014 Getting Started Tutorial");
            Console.WriteLine("===================================");
            Console.WriteLine();

            Console.WriteLine("STEP 1: SET UP A WORKSPACE");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Point Wally at your codebase root. This creates a .wally/ folder");
            Console.WriteLine("with config, actors, loops, wrappers, and runbooks.");
            Console.WriteLine();
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine("  cd C:\\repos\\MyApp");
            Console.WriteLine();
            Console.WriteLine("Verify the workspace structure is correct:");
            Console.WriteLine("  wally setup --verify");
            Console.WriteLine();

            Console.WriteLine("STEP 2: RUN YOUR FIRST PROMPT");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("You can run a prompt directly without any actor, or use an actor to add");
            Console.WriteLine("Role/AcceptanceCriteria/Intent context to your prompt.");
            Console.WriteLine();
            Console.WriteLine("Direct mode (no actor — prompt sent as-is):");
            Console.WriteLine("  wally run \"What does this codebase do?\"");
            Console.WriteLine("  wally run \"Summarize the main architecture\"");
            Console.WriteLine();
            Console.WriteLine("With an actor (adds RBA persona context):");
            Console.WriteLine("  wally run \"Review the auth module for security issues\" -a Engineer");
            Console.WriteLine("  wally run \"Write requirements for the search feature\" -a BusinessAnalyst");
            Console.WriteLine("  wally run \"Define success criteria for the payment system\" -a Stakeholder");
            Console.WriteLine();
            Console.WriteLine("Override the model or wrapper:");
            Console.WriteLine("  wally run \"Review auth\" -a Engineer -m claude-sonnet-4");
            Console.WriteLine("  wally run \"Fix the login bug\" -a Engineer -w AutoCopilot");
            Console.WriteLine();

            Console.WriteLine("STEP 3: USE LOOPS FOR ITERATIVE TASKS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Loops run an actor multiple times. Each iteration sees the previous response");
            Console.WriteLine("and decides whether to continue. The actor stops when it says [LOOP COMPLETED].");
            Console.WriteLine();
            Console.WriteLine("Use the built-in CodeReview loop:");
            Console.WriteLine("  wally run \"Review the data access layer\" -a Engineer -l CodeReview");
            Console.WriteLine();
            Console.WriteLine("Or use inline loop mode with a custom iteration count:");
            Console.WriteLine("  wally run \"Refactor error handling\" -a Engineer --loop -n 5");
            Console.WriteLine();

            Console.WriteLine("STEP 4: USE RUNBOOKS FOR COMMAND SEQUENCES");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Runbooks are .wrb files with one Wally command per line. They chain");
            Console.WriteLine("multiple actors and loops into a repeatable workflow.");
            Console.WriteLine();
            Console.WriteLine("Run the shipped full-analysis runbook:");
            Console.WriteLine("  wally runbook full-analysis \"Improve the logging module\"");
            Console.WriteLine();
            Console.WriteLine("List available runbooks:");
            Console.WriteLine("  wally list-runbooks");
            Console.WriteLine();

            Console.WriteLine("STEP 5: BUILD YOUR OWN ACTORS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Actors are defined by a folder + actor.json with three prompts:");
            Console.WriteLine("  Role           \u2014 who the actor is (e.g. \"You are a security auditor...\")");
            Console.WriteLine("  Criteria       \u2014 what success looks like");
            Console.WriteLine("  Intent         \u2014 what the actor aims to accomplish");
            Console.WriteLine();
            Console.WriteLine("Create from the command line:");
            Console.WriteLine("  wally add-actor SecurityAuditor -r \"You are a security auditor\" -c \"Find all vulnerabilities\" -i \"Produce a security report\"");
            Console.WriteLine();
            Console.WriteLine("Or create the folder manually:");
            Console.WriteLine("  .wally/Actors/SecurityAuditor/actor.json");
            Console.WriteLine("  wally reload-actors");
            Console.WriteLine();
            Console.WriteLine("Edit or remove actors:");
            Console.WriteLine("  wally edit-actor SecurityAuditor -r \"You are a senior security auditor\"");
            Console.WriteLine("  wally delete-actor SecurityAuditor");
            Console.WriteLine("  wally list");
            Console.WriteLine();

            Console.WriteLine("STEP 6: BUILD YOUR OWN LOOPS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Loops are JSON files in .wally/Loops/. They define the start prompt,");
            Console.WriteLine("continuation template, stop keywords, and max iterations.");
            Console.WriteLine();
            Console.WriteLine("Create a loop:");
            Console.WriteLine("  wally add-loop SecurityScan -d \"Iterative security scan\" -a SecurityAuditor -n 3 -s \"Scan for vulnerabilities: {userPrompt}\"");
            Console.WriteLine();
            Console.WriteLine("Edit or remove loops:");
            Console.WriteLine("  wally edit-loop SecurityScan -n 5");
            Console.WriteLine("  wally delete-loop SecurityScan");
            Console.WriteLine("  wally list-loops");
            Console.WriteLine();

            Console.WriteLine("STEP 7: BUILD YOUR OWN WRAPPERS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Wrappers are JSON files in .wally/Wrappers/ that define how to call");
            Console.WriteLine("an LLM CLI tool. Each wrapper specifies the executable, argument template,");
            Console.WriteLine("and placeholders ({prompt}, {model}, {sourcePath}).");
            Console.WriteLine();
            Console.WriteLine("Default wrappers:");
            Console.WriteLine("  Copilot       \u2014 read-only, runs gh copilot (default)");
            Console.WriteLine("  AutoCopilot   \u2014 agentic, can edit files on disk");
            Console.WriteLine();
            Console.WriteLine("Create a custom wrapper:");
            Console.WriteLine("  wally add-wrapper OllamaChat -d \"Local Ollama\" -e ollama -t \"run {model} {prompt}\"");
            Console.WriteLine();
            Console.WriteLine("Edit or remove wrappers:");
            Console.WriteLine("  wally edit-wrapper OllamaChat -e /usr/bin/ollama");
            Console.WriteLine("  wally delete-wrapper OllomaChat");
            Console.WriteLine("  wally list-wrappers");
            Console.WriteLine();

            Console.WriteLine("STEP 8: BUILD YOUR OWN RUNBOOKS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("Runbooks are .wrb text files in .wally/Runbooks/. Each line is a");
            Console.WriteLine("Wally command. Use {userPrompt} to pass in the runtime prompt.");
            Console.WriteLine();
            Console.WriteLine("Create a runbook scaffold:");
            Console.WriteLine("  wally add-runbook security-review -d \"Full security review pipeline\"");
            Console.WriteLine();
            Console.WriteLine("Then edit .wally/Runbooks/security-review.wrb to add commands:");
            Console.WriteLine("  # Full security review pipeline");
            Console.WriteLine("  run \"{userPrompt}\" -a SecurityAuditor -l SecurityScan");
            Console.WriteLine("  run \"{userPrompt}\" -a Engineer -l CodeReview");
            Console.WriteLine();
            Console.WriteLine("Edit or remove runbooks:");
            Console.WriteLine("  wally edit-runbook security-review -d \"Updated description\"");
            Console.WriteLine("  wally delete-runbook security-review");
            Console.WriteLine("  wally list-runbooks");
            Console.WriteLine();

            Console.WriteLine("STEP 9: INSPECT & EXPLORE");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally info               Show workspace paths, actors, wrappers, session info");
            Console.WriteLine("  wally list               List all actors and their RBA prompts");
            Console.WriteLine("  wally list-loops         List all loop definitions");
            Console.WriteLine("  wally list-wrappers      List all LLM wrapper definitions");
            Console.WriteLine("  wally list-runbooks      List all runbook definitions");
            Console.WriteLine("  wally commands           Show the full command reference");
            Console.WriteLine();

            Console.WriteLine("REFERENCE: ALL MANAGEMENT COMMANDS");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  Actors:    add-actor, edit-actor, delete-actor, list, reload-actors");
            Console.WriteLine("  Loops:     add-loop, edit-loop, delete-loop, list-loops");
            Console.WriteLine("  Wrappers:  add-wrapper, edit-wrapper, delete-wrapper, list-wrappers");
            Console.WriteLine("  Runbooks:  add-runbook, edit-runbook, delete-runbook, list-runbooks");
            Console.WriteLine();
            Console.WriteLine("Type 'commands' for the full command reference with all options.");
        }

        // — List wrappers ——————————————————————————————————————————————

        /// <summary>Lists all loaded LLM wrapper definitions.</summary>
        public static void HandleListWrappers(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-wrappers") == null) return;
            env.Logger.LogCommand("list-wrappers");

            var wrappers = env.Workspace!.LlmWrappers;
            Console.WriteLine($"Wrappers ({wrappers.Count}):");
            if (wrappers.Count == 0)
            {
                Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Wrappers/)");
                return;
            }

            foreach (var w in wrappers)
            {
                Console.WriteLine($"  [{w.Name}]");
                if (!string.IsNullOrWhiteSpace(w.Description))
                    Console.WriteLine($"    Description:  {w.Description}");
                Console.WriteLine($"    Executable:   {w.Executable}");
                Console.WriteLine($"    Template:     {w.ArgumentTemplate}");
                Console.WriteLine($"    CanMakeChanges: {w.CanMakeChanges}");
            }
        }

        // — Actor CRUD ——————————————————————————————————————————————————

        /// <summary>Creates a new actor with RBA prompts and saves it to disk.</summary>
        public static void HandleAddActor(
            WallyEnvironment env, string name,
            string rolePrompt, string criteriaPrompt, string intentPrompt)
        {
            if (RequireWorkspace(env, "add-actor") == null) return;

            var existing = env.GetActor(name);
            if (existing != null)
            {
                Console.WriteLine($"Actor '{name}' already exists. Use 'edit-actor' to modify it.");
                return;
            }

            var ws = env.Workspace!;
            string actorDir = Path.Combine(ws.WorkspaceFolder, ws.Config.ActorsFolderName, name);

            var actor = new Actor(
                name, actorDir,
                new Role(name, rolePrompt),
                new AcceptanceCriteria(name, criteriaPrompt),
                new Intent(name, intentPrompt),
                ws);

            WallyHelper.SaveActor(ws.WorkspaceFolder, ws.Config, actor);
            env.ReloadActors();

            env.Logger.LogCommand("add-actor", $"Created actor '{name}'");
            Console.WriteLine($"Actor '{name}' created at: {actorDir}");
            Console.WriteLine("  Edit .wally/Actors/" + name + "/actor.json to refine prompts.");
        }

        /// <summary>Edits an existing actor's RBA prompts (only fields that are provided).</summary>
        public static void HandleEditActor(
            WallyEnvironment env, string name,
            string? rolePrompt, string? criteriaPrompt, string? intentPrompt)
        {
            if (RequireWorkspace(env, "edit-actor") == null) return;

            var actor = env.GetActor(name);
            if (actor == null)
            {
                Console.WriteLine($"Actor '{name}' not found. Available actors:");
                foreach (var a in env.Actors)
                    Console.WriteLine($"  {a.Name}");
                return;
            }

            if (rolePrompt != null) actor.Role.Prompt = rolePrompt;
            if (criteriaPrompt != null) actor.AcceptanceCriteria.Prompt = criteriaPrompt;
            if (intentPrompt != null) actor.Intent.Prompt = intentPrompt;

            WallyHelper.SaveActor(env.Workspace!.WorkspaceFolder, env.Workspace.Config, actor);
            env.Logger.LogCommand("edit-actor", $"Updated actor '{name}'");
            Console.WriteLine($"Actor '{name}' updated.");
        }

        /// <summary>Deletes an actor folder from the workspace.</summary>
        public static void HandleDeleteActor(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-actor") == null) return;

            var actor = env.GetActor(name);
            if (actor == null)
            {
                Console.WriteLine($"Actor '{name}' not found.");
                return;
            }

            string actorDir = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.ActorsFolderName, name);

            if (Directory.Exists(actorDir))
            {
                Directory.Delete(actorDir, recursive: true);
                env.ReloadActors();
                env.Logger.LogCommand("delete-actor", $"Deleted actor '{name}'");
                Console.WriteLine($"Actor '{name}' deleted.");
            }
            else
            {
                Console.WriteLine($"Actor folder not found: {actorDir}");
            }
        }

        // — Loop CRUD ——————————————————————————————————————————————————

        /// <summary>Creates a new loop definition and saves it to disk.</summary>
        public static void HandleAddLoop(
            WallyEnvironment env, string name, string description,
            string actorName, int maxIterations, string startPrompt)
        {
            if (RequireWorkspace(env, "add-loop") == null) return;

            var existing = env.GetLoop(name);
            if (existing != null)
            {
                Console.WriteLine($"Loop '{name}' already exists. Use 'edit-loop' to modify it.");
                return;
            }

            var ws = env.Workspace!;
            string loopsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName);
            Directory.CreateDirectory(loopsDir);

            var loop = new WallyLoopDefinition
            {
                Name = name,
                Description = description,
                ActorName = actorName,
                MaxIterations = maxIterations,
                StartPrompt = startPrompt
            };

            string filePath = Path.Combine(loopsDir, $"{name}.json");
            loop.SaveToFile(filePath);
            ws.ReloadLoops();

            env.Logger.LogCommand("add-loop", $"Created loop '{name}'");
            Console.WriteLine($"Loop '{name}' created at: {filePath}");
        }

        /// <summary>Edits an existing loop definition (only fields that are provided).</summary>
        public static void HandleEditLoop(
            WallyEnvironment env, string name,
            string? description, string? actorName, int? maxIterations, string? startPrompt)
        {
            if (RequireWorkspace(env, "edit-loop") == null) return;

            var loop = env.GetLoop(name);
            if (loop == null)
            {
                Console.WriteLine($"Loop '{name}' not found. Available loops:");
                foreach (var l in env.Loops)
                    Console.WriteLine($"  {l.Name}");
                return;
            }

            if (description != null) loop.Description = description;
            if (actorName != null) loop.ActorName = actorName;
            if (maxIterations.HasValue) loop.MaxIterations = maxIterations.Value;
            if (startPrompt != null) loop.StartPrompt = startPrompt;

            var ws = env.Workspace!;
            string filePath = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName, $"{name}.json");
            loop.SaveToFile(filePath);

            env.Logger.LogCommand("edit-loop", $"Updated loop '{name}'");
            Console.WriteLine($"Loop '{name}' updated.");
        }

        /// <summary>Deletes a loop definition file from the workspace.</summary>
        public static void HandleDeleteLoop(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-loop") == null) return;

            var ws = env.Workspace!;
            string filePath = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName, $"{name}.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Loop file not found: {filePath}");
                return;
            }

            File.Delete(filePath);
            ws.ReloadLoops();

            env.Logger.LogCommand("delete-loop", $"Deleted loop '{name}'");
            Console.WriteLine($"Loop '{name}' deleted.");
        }

        // — Wrapper CRUD ————————————————————————————————————————————————

        /// <summary>Creates a new LLM wrapper definition and saves it to disk.</summary>
        public static void HandleAddWrapper(
            WallyEnvironment env, string name, string description,
            string executable, string argumentTemplate, bool canMakeChanges)
        {
            if (RequireWorkspace(env, "add-wrapper") == null) return;

            var existing = env.Workspace!.LlmWrappers.FirstOrDefault(w =>
                string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Console.WriteLine($"Wrapper '{name}' already exists. Use 'edit-wrapper' to modify it.");
                return;
            }

            var ws = env.Workspace!;
            string wrappersDir = Path.Combine(ws.WorkspaceFolder, ws.Config.WrappersFolderName);
            Directory.CreateDirectory(wrappersDir);

            var wrapper = new LLMWrapper
            {
                Name = name,
                Description = description,
                Executable = executable,
                ArgumentTemplate = argumentTemplate,
                CanMakeChanges = canMakeChanges
            };

            string filePath = Path.Combine(wrappersDir, $"{name}.json");
            wrapper.SaveToFile(filePath);
            ws.ReloadWrappers();

            env.Logger.LogCommand("add-wrapper", $"Created wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' created at: {filePath}");
        }

        /// <summary>Edits an existing LLM wrapper definition (only fields that are provided).</summary>
        public static void HandleEditWrapper(
            WallyEnvironment env, string name,
            string? description, string? executable, string? argumentTemplate, bool? canMakeChanges)
        {
            if (RequireWorkspace(env, "edit-wrapper") == null) return;

            var wrapper = env.Workspace!.LlmWrappers.FirstOrDefault(w =>
                string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
            if (wrapper == null)
            {
                Console.WriteLine($"Wrapper '{name}' not found. Available wrappers:");
                foreach (var w in env.Workspace.LlmWrappers)
                    Console.WriteLine($"  {w.Name}");
                return;
            }

            if (description != null) wrapper.Description = description;
            if (executable != null) wrapper.Executable = executable;
            if (argumentTemplate != null) wrapper.ArgumentTemplate = argumentTemplate;
            if (canMakeChanges.HasValue) wrapper.CanMakeChanges = canMakeChanges.Value;

            string filePath = Path.Combine(
                env.Workspace.WorkspaceFolder, env.Workspace.Config.WrappersFolderName, $"{name}.json");
            wrapper.SaveToFile(filePath);

            env.Logger.LogCommand("edit-wrapper", $"Updated wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' updated.");
        }

        /// <summary>Deletes an LLM wrapper definition file from the workspace.</summary>
        public static void HandleDeleteWrapper(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-wrapper") == null) return;

            var ws = env.Workspace!;
            string filePath = Path.Combine(ws.WorkspaceFolder, ws.Config.WrappersFolderName, $"{name}.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Wrapper file not found: {filePath}");
                return;
            }

            File.Delete(filePath);
            ws.ReloadWrappers();

            env.Logger.LogCommand("delete-wrapper", $"Deleted wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' deleted.");
        }

        // — Runbook CRUD ————————————————————————————————————————————————

        /// <summary>Creates a new runbook scaffold (.wrb file) in the workspace.</summary>
        public static void HandleAddRunbook(WallyEnvironment env, string name, string description)
        {
            if (RequireWorkspace(env, "add-runbook") == null) return;

            var existing = env.GetRunbook(name);
            if (existing != null)
            {
                Console.WriteLine($"Runbook '{name}' already exists. Use 'edit-runbook' to modify it.");
                return;
            }

            var ws = env.Workspace!;
            string runbooksDir = Path.Combine(ws.WorkspaceFolder, ws.Config.RunbooksFolderName);
            Directory.CreateDirectory(runbooksDir);

            string filePath = Path.Combine(runbooksDir, $"{name}.wrb");
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(description))
                lines.Add($"# {description}");
            else
                lines.Add($"# {name}");
            lines.Add("");
            lines.Add("# Add your commands below, one per line.");
            lines.Add("# Use {userPrompt} to pass in the runtime prompt.");
            lines.Add("# Example:");
            lines.Add("# run \"{userPrompt}\" -a Engineer");

            File.WriteAllLines(filePath, lines);
            ws.ReloadRunbooks();

            env.Logger.LogCommand("add-runbook", $"Created runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' created at: {filePath}");
            Console.WriteLine("  Edit the .wrb file to add commands.");
        }

        /// <summary>Edits the description comment of an existing runbook.</summary>
        public static void HandleEditRunbook(WallyEnvironment env, string name, string? description)
        {
            if (RequireWorkspace(env, "edit-runbook") == null) return;

            var runbook = env.GetRunbook(name);
            if (runbook == null)
            {
                Console.WriteLine($"Runbook '{name}' not found. Available runbooks:");
                foreach (var r in env.Runbooks)
                    Console.WriteLine($"  {r.Name}");
                return;
            }

            if (description == null)
            {
                Console.WriteLine($"Runbook '{name}': {runbook.Description}");
                Console.WriteLine($"  File: {runbook.FilePath}");
                Console.WriteLine("  Use -d \"new description\" to update.");
                return;
            }

            // Rewrite the file with the updated description line.
            if (!File.Exists(runbook.FilePath))
            {
                Console.WriteLine($"Runbook file not found: {runbook.FilePath}");
                return;
            }

            var allLines = new List<string>(File.ReadAllLines(runbook.FilePath));
            bool replaced = false;
            for (int i = 0; i < allLines.Count; i++)
            {
                if (allLines[i].TrimStart().StartsWith('#'))
                {
                    allLines[i] = $"# {description}";
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
                allLines.Insert(0, $"# {description}");

            File.WriteAllLines(runbook.FilePath, allLines);
            env.Workspace!.ReloadRunbooks();

            env.Logger.LogCommand("edit-runbook", $"Updated runbook '{name}' description");
            Console.WriteLine($"Runbook '{name}' description updated.");
        }

        /// <summary>Deletes a runbook (.wrb) file from the workspace.</summary>
        public static void HandleDeleteRunbook(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-runbook") == null) return;

            var ws = env.Workspace!;
            string filePath = Path.Combine(ws.WorkspaceFolder, ws.Config.RunbooksFolderName, $"{name}.wrb");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Runbook file not found: {filePath}");
                return;
            }

            File.Delete(filePath);
            ws.ReloadRunbooks();

            env.Logger.LogCommand("delete-runbook", $"Deleted runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' deleted.");
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