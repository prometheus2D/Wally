using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Wally.Core.Actors;
using Wally.Core.Providers;

namespace Wally.Core
{
    /// <summary>
    /// Contains the implementation logic for Wally commands.
    /// </summary>
    public static class WallyCommands
    {
        private const int MaxRunbookDepth = 10;

        private static WallyEnvironment? RequireWorkspace(WallyEnvironment env, string commandName)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine($"Command '{commandName}' requires a workspace. Use 'setup <path>' or 'load <path>' first.");
                env.Logger.LogError($"No workspace loaded for command '{commandName}'.", commandName);
                return null;
            }
            return env;
        }

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
                case "repair":
                {
                    string? path = GetFirstPositional(args, 1);
                    HandleRepair(env, path);
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
                    if (args.Length < 2) { Console.WriteLine("Usage: run \"<prompt>\" [-a actor] [-m model] [-w wrapper] [-l name] [--no-history]"); return false; }
                    string? actorName = GetOption(args, "-a") ?? GetOption(args, "--actor");
                    string? model     = GetOption(args, "-m") ?? GetOption(args, "--model");
                    string? wrapper   = GetOption(args, "-w") ?? GetOption(args, "--wrapper");
                    string? loopName  = GetOption(args, "-l") ?? GetOption(args, "--loop-name");
                    bool noHistory    = HasFlag(args, "--no-history");
                    HandleRun(env, args[1], actorName, model, loopName, wrapper, noHistory);
                    return true;
                }
                case "runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: runbook <name> [\"<prompt>\"]"); return false; }
                    string? prompt = args.Length >= 3 ? args[2] : null;
                    return HandleRunbook(env, args[1], prompt, runbookDepth);
                }
                case "list":          HandleList(env);         return true;
                case "list-loops":    HandleListLoops(env);    return true;
                case "list-wrappers": HandleListWrappers(env); return true;
                case "list-runbooks": HandleListRunbooks(env); return true;
                case "info":          HandleInfo(env);         return true;
                case "reload-actors": HandleReloadActors(env); return true;
                case "cleanup":
                {
                    string? cleanupPath = GetFirstPositional(args, 1);
                    HandleCleanup(env, cleanupPath);
                    return true;
                }
                case "clear-history": HandleClearHistory(env); return true;
                case "commands" or "help": HandleHelp();     return true;
                case "tutorial":           HandleTutorial(); return true;

                // ?? Actor CRUD ???????????????????????????????????????????????
                case "add-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string role     = GetOption(args, "-r") ?? GetOption(args, "--role")     ?? "";
                    string criteria = GetOption(args, "-c") ?? GetOption(args, "--criteria") ?? "";
                    string intent   = GetOption(args, "-i") ?? GetOption(args, "--intent")   ?? "";
                    HandleAddActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "edit-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string? role     = GetOption(args, "-r") ?? GetOption(args, "--role");
                    string? criteria = GetOption(args, "-c") ?? GetOption(args, "--criteria");
                    string? intent   = GetOption(args, "-i") ?? GetOption(args, "--intent");
                    HandleEditActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "delete-actor":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-actor <name>"); return false; }
                    HandleDeleteActor(env, args[1]);
                    return true;

                // ?? Loop CRUD ????????????????????????????????????????????????
                case "add-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-loop <name> [-d desc] [-a actor] [-s prompt]"); return false; }
                    string desc        = GetOption(args, "-d") ?? GetOption(args, "--description") ?? "";
                    string actor       = GetOption(args, "-a") ?? GetOption(args, "--actor")        ?? "";
                    string startPrompt = GetOption(args, "-s") ?? GetOption(args, "--start-prompt") ?? "{userPrompt}";
                    HandleAddLoop(env, args[1], desc, actor, startPrompt);
                    return true;
                }
                case "edit-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-loop <name> [-d desc] [-a actor] [-s prompt]"); return false; }
                    string? desc        = GetOption(args, "-d") ?? GetOption(args, "--description");
                    string? actor       = GetOption(args, "-a") ?? GetOption(args, "--actor");
                    string? startPrompt = GetOption(args, "-s") ?? GetOption(args, "--start-prompt");
                    HandleEditLoop(env, args[1], desc, actor, startPrompt);
                    return true;
                }
                case "delete-loop":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-loop <name>"); return false; }
                    HandleDeleteLoop(env, args[1]);
                    return true;

                // ?? Wrapper CRUD ?????????????????????????????????????????????
                case "add-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes] [--no-conversation-history]"); return false; }
                    string desc     = GetOption(args, "-d") ?? GetOption(args, "--description") ?? "";
                    string exe      = GetOption(args, "-e") ?? GetOption(args, "--executable")  ?? "gh";
                    string template = GetOption(args, "-t") ?? GetOption(args, "--template")    ?? "";
                    bool canChange  = HasFlag(args, "--can-make-changes");
                    bool useHistory = !HasFlag(args, "--no-conversation-history");
                    HandleAddWrapper(env, args[1], desc, exe, template, canChange, useHistory);
                    return true;
                }
                case "edit-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes] [--no-conversation-history]"); return false; }
                    string? desc     = GetOption(args, "-d") ?? GetOption(args, "--description");
                    string? exe      = GetOption(args, "-e") ?? GetOption(args, "--executable");
                    string? template = GetOption(args, "-t") ?? GetOption(args, "--template");
                    bool? canChange  = HasFlag(args, "--can-make-changes") ? true : null;
                    bool? useHistory = HasFlag(args, "--no-conversation-history") ? false : null;
                    HandleEditWrapper(env, args[1], desc, exe, template, canChange, useHistory);
                    return true;
                }
                case "delete-wrapper":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-wrapper <name>"); return false; }
                    HandleDeleteWrapper(env, args[1]);
                    return true;

                // ?? Runbook CRUD ?????????????????????????????????????????????
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
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-runbook <name>"); return false; }
                    HandleDeleteRunbook(env, args[1]);
                    return true;

                default:
                    Console.WriteLine($"Unknown command: {verb}. Type 'commands' for help.");
                    return false;
            }
        }

        // ?? Workspace lifecycle ??????????????????????????????????????????????

        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            env.Logger.LogCommand("load", $"Loaded workspace from {path}");
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        public static void HandleSetup(WallyEnvironment env, string? workSourcePath = null, bool verifyOnly = false)
        {
            if (verifyOnly)
            {
                string wsFolder = ResolveWorkspaceFolder(workSourcePath);
                Console.WriteLine($"Verifying workspace at: {wsFolder}");
                Console.WriteLine();
                var issues = WallyHelper.CheckWorkspace(wsFolder);
                if (issues.Count == 0)
                    Console.WriteLine("\u2713 Workspace structure is valid. No issues found.");
                else
                {
                    Console.WriteLine($"Issues found ({issues.Count}):");
                    foreach (var issue in issues) Console.WriteLine($"  {issue}");
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

        /// <summary>
        /// Repairs a workspace by creating any missing standard folders, mailbox folders,
        /// and actor mailbox folders — without touching anything already on disk.
        /// After repair the workspace is reloaded so in-memory state matches.
        /// </summary>
        public static void HandleRepair(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder;
            if (!string.IsNullOrWhiteSpace(workSourcePath))
            {
                string src = Path.IsPathRooted(workSourcePath)
                    ? workSourcePath!
                    : Path.Combine(WallyHelper.GetExeDirectory(), workSourcePath!);
                wsFolder = Path.Combine(Path.GetFullPath(src), WallyHelper.DefaultWorkspaceFolderName);
            }
            else if (env.HasWorkspace)
            {
                wsFolder = env.Workspace!.WorkspaceFolder;
            }
            else
            {
                wsFolder = WallyHelper.GetDefaultWorkspaceFolder();
            }

            wsFolder = Path.GetFullPath(wsFolder);

            Console.WriteLine($"Repairing workspace at: {wsFolder}");
            Console.WriteLine();

            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine("Workspace folder does not exist. Use 'setup' to create a new workspace.");
                env.Logger.LogCommand("repair", $"Workspace not found at {wsFolder}");
                return;
            }

            var config = WallyHelper.ResolveConfig(wsFolder);
            var added  = new List<string>();

            // ?? Standard workspace subfolders ??????????????????????????????
            EnsureDir(wsFolder, config.ActorsFolderName,    added);
            EnsureDir(wsFolder, config.DocsFolderName,      added);
            EnsureDir(wsFolder, config.TemplatesFolderName, added);
            EnsureDir(wsFolder, config.LoopsFolderName,     added);
            EnsureDir(wsFolder, config.WrappersFolderName,  added);
            EnsureDir(wsFolder, config.RunbooksFolderName,  added);
            EnsureDir(wsFolder, config.LogsFolderName,      added);
            EnsureDir(wsFolder, Logging.ConversationLogger.DefaultFolderName, added);

            // ?? Workspace shared mailbox ???????????????????????????????????
            EnsureMailboxDir(wsFolder, "workspace",          added);

            // ?? Per-actor mailboxes ????????????????????????????????????????
            string actorsDir = Path.Combine(wsFolder, config.ActorsFolderName);
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                {
                    string actorName = Path.GetFileName(actorDir);

                    // Actor-level Docs subfolder
                    string docsFolder = Path.Combine(actorDir, "Docs");
                    if (!Directory.Exists(docsFolder))
                    {
                        Directory.CreateDirectory(docsFolder);
                        added.Add($"  Actors/{actorName}/Docs/");
                    }

                    EnsureMailboxDir(actorDir, $"actor '{actorName}'", added);
                }
            }

            // ?? Report ????????????????????????????????????????????????????
            if (added.Count == 0)
            {
                Console.WriteLine("\u2713 Workspace is already complete — nothing to repair.");
            }
            else
            {
                Console.WriteLine($"Repaired {added.Count} missing component(s):");
                foreach (var item in added)
                    Console.WriteLine($"  \u2713 {item}");
            }

            Console.WriteLine();

            // Reload so the UI/in-memory model picks up any new actors/wrappers.
            if (env.HasWorkspace && string.Equals(
                    Path.GetFullPath(env.Workspace!.WorkspaceFolder),
                    wsFolder, StringComparison.OrdinalIgnoreCase))
            {
                env.LoadWorkspace(wsFolder);
                Console.WriteLine("Workspace reloaded.");
            }

            env.Logger.LogCommand("repair",
                added.Count == 0
                    ? $"Repair: workspace at {wsFolder} was already complete."
                    : $"Repair: added {added.Count} component(s) to {wsFolder}.");
        }

        // ?? Repair helpers ????????????????????????????????????????????????????

        private static void EnsureDir(string parent, string subFolder, List<string> added)
        {
            string full = Path.Combine(parent, subFolder);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                added.Add($"{subFolder}/");
            }
        }

        private static void EnsureMailboxDir(string entityDir, string label, List<string> added)
        {
            foreach (string folder in new[]
            {
                WallyHelper.MailboxInboxFolderName,
                WallyHelper.MailboxOutboxFolderName,
                WallyHelper.MailboxPendingFolderName,
                WallyHelper.MailboxActiveFolderName
            })
            {
                string full = Path.Combine(entityDir, folder);
                if (!Directory.Exists(full))
                {
                    Directory.CreateDirectory(full);
                    // Make the reported path relative to the workspace
                    string rel = Path.GetRelativePath(
                        Path.GetDirectoryName(entityDir.TrimEnd(Path.DirectorySeparatorChar)) ?? entityDir,
                        full);
                    added.Add($"{rel}{Path.DirectorySeparatorChar}  [{label} mailbox]");
                }
            }
        }

        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            env.Logger.LogCommand("save", $"Saved workspace to {path}");
            Console.WriteLine($"Workspace saved to: {path}");
        }

        // ?? Run ??????????????????????????????????????????????????????????????

        /// <summary>
        /// Backward-compatible overload — returns raw response strings.
        /// </summary>
        public static List<string> HandleRun(
            WallyEnvironment env,
            string prompt,
            string? actorName  = null,
            string? model      = null,
            string? loopName   = null,
            string? wrapper    = null,
            bool noHistory     = false,
            CancellationToken cancellationToken = default)
        {
            return WallyRunResult.ToStringList(
                HandleRunTyped(env, prompt, actorName, model, loopName, wrapper, noHistory, cancellationToken));
        }

        /// <summary>
        /// Executes a prompt through the pipeline defined by the selected loop definition
        /// (or a direct single-actor call when no loop is specified).
        /// Returns one <see cref="WallyRunResult"/> per step executed.
        /// </summary>
        public static List<WallyRunResult> HandleRunTyped(
            WallyEnvironment env,
            string prompt,
            string? actorName  = null,
            string? model      = null,
            string? loopName   = null,
            string? wrapper    = null,
            bool noHistory     = false,
            CancellationToken cancellationToken = default)
        {
            if (RequireWorkspace(env, "run") == null) return new List<WallyRunResult>();

            // Resolve loop definition
            WallyLoopDefinition? loopDef = null;
            if (!string.IsNullOrWhiteSpace(loopName))
            {
                loopDef = env.GetLoop(loopName!);
                if (loopDef == null)
                {
                    Console.WriteLine($"Loop '{loopName}' not found. Available loops:");
                    foreach (var l in env.Loops) Console.WriteLine($"  {l.Name} \u2014 {l.Description}");
                    env.Logger.LogError($"Loop '{loopName}' not found.", "run");
                    return new List<WallyRunResult>();
                }
            }

            string loopLabel = loopDef != null ? $"[run:{loopDef.Name}]" : "[run]";

            // Multi-step pipeline
            if (loopDef?.HasSteps == true)
                return RunPipeline(env, prompt, loopDef, loopLabel, model, wrapper, noHistory, cancellationToken);

            // Single-actor / direct
            if (string.IsNullOrWhiteSpace(actorName) && !string.IsNullOrWhiteSpace(loopDef?.ActorName))
                actorName = loopDef!.ActorName;

            bool directMode = string.IsNullOrWhiteSpace(actorName);
            Actor? actor = null;
            if (!directMode)
            {
                actor = env.GetActor(actorName!);
                if (actor == null)
                {
                    Console.WriteLine($"Actor '{actorName}' not found.");
                    env.Logger.LogError($"Actor '{actorName}' not found.", "run");
                    return new List<WallyRunResult>();
                }
            }

            string actorLabel = directMode ? "(no actor)" : actorName!;

            // Resolve the prompt — apply StartPrompt template if defined.
            string resolvedPrompt = (loopDef != null && !string.IsNullOrWhiteSpace(loopDef.StartPrompt))
                ? loopDef.StartPrompt.Replace("{userPrompt}", prompt)
                : prompt;

            env.Logger.LogCommand("run", $"Actor='{actorLabel}' loop='{loopDef?.Name ?? "(none)"}' model='{model ?? "(default)"}' wrapper='{wrapper ?? "(default)"}'");
            env.Logger.LogPrompt(actorLabel, resolvedPrompt, model ?? env.Workspace!.Config.DefaultModel);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string response = directMode
                ? env.ExecutePrompt(resolvedPrompt, model, wrapper, skipHistory: noHistory, cancellationToken: cancellationToken)
                : env.ExecuteActor(actor!, resolvedPrompt, model, wrapper, skipHistory: noHistory, cancellationToken: cancellationToken);
            sw.Stop();
            env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, 1);

            Console.WriteLine(response);
            Console.WriteLine();

            return new List<WallyRunResult>
            {
                new WallyRunResult { StepName = null, ActorName = actorLabel, Response = response }
            };
        }

        // ?? Pipeline execution ???????????????????????????????????????????????

        private static List<WallyRunResult> RunPipeline(
            WallyEnvironment env,
            string prompt,
            WallyLoopDefinition loopDef,
            string loopLabel,
            string? model,
            string? wrapper,
            bool noHistory,
            CancellationToken cancellationToken = default)
        {
            var stepDefs = loopDef.Steps;

            env.Logger.LogCommand("run", $"[pipeline] loop='{loopDef.Name}' steps={stepDefs.Count} model='{model ?? "(default)"}' wrapper='{wrapper ?? "(default)"}'");
            Console.WriteLine($"{loopLabel} Pipeline \u2014 {stepDefs.Count} step(s)");
            Console.WriteLine();

            // Resolve actors once
            var stepActors = new (Actor? actor, bool isDirect, string actorLabel)[stepDefs.Count];
            for (int i = 0; i < stepDefs.Count; i++)
            {
                var stepDef = stepDefs[i];
                string resolvedActorName = !string.IsNullOrWhiteSpace(stepDef.ActorName)
                    ? stepDef.ActorName : loopDef.ActorName;

                bool isDirect = string.IsNullOrWhiteSpace(resolvedActorName);
                Actor? stepActor = null;
                if (!isDirect)
                {
                    stepActor = env.GetActor(resolvedActorName!);
                    if (stepActor == null)
                    {
                        Console.WriteLine($"{loopLabel} Step '{stepDef.Name}': actor '{resolvedActorName}' not found.");
                        env.Logger.LogError($"Pipeline '{loopDef.Name}' step '{stepDef.Name}': actor '{resolvedActorName}' not found.", "run");
                        return new List<WallyRunResult>();
                    }
                }

                string actorLabel = isDirect ? "(no actor)" : resolvedActorName!;
                stepActors[i] = (stepActor, isDirect, actorLabel);
                Console.WriteLine($"  Step {i + 1}: [{(string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i+1}" : stepDef.Name)}]  Actor: {actorLabel}");
            }
            Console.WriteLine();

            // Run steps in order
            var results = new List<WallyRunResult>(stepDefs.Count);
            string? previousStepResult = null;

            for (int i = 0; i < stepDefs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepDef = stepDefs[i];
                var (stepActor, isDirect, actorLabel) = stepActors[i];
                string stepName = string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i + 1}" : stepDef.Name;

                string stepPrompt = stepDef.BuildPrompt(prompt, previousStepResult);

                Console.WriteLine($"--- Step {i + 1}: {stepName} ({actorLabel}) ---");

                env.Logger.LogPrompt(actorLabel, stepPrompt, model ?? env.Workspace!.Config.DefaultModel);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                string response = isDirect
                    ? env.ExecutePrompt(stepPrompt, model, wrapper, skipHistory: noHistory, cancellationToken: cancellationToken)
                    : env.ExecuteActor(stepActor!, stepPrompt, model, wrapper, skipHistory: noHistory, cancellationToken: cancellationToken);
                sw.Stop();
                env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, i + 1);

                Console.WriteLine(response);
                Console.WriteLine();

                results.Add(new WallyRunResult
                {
                    StepName  = stepName,
                    ActorName = actorLabel,
                    Response  = response
                });

                previousStepResult = response;
            }

            Console.WriteLine($"{loopLabel} Pipeline complete \u2014 {results.Count} step(s).");
            env.Logger.LogInfo($"Pipeline '{loopDef.Name}' complete: {results.Count} step(s).");
            return results;
        }

        // ?? Runbooks ?????????????????????????????????????????????????????????

        public static bool HandleRunbook(WallyEnvironment env, string runbookName, string? userPrompt = null, int depth = 0)
        {
            if (depth >= MaxRunbookDepth)
            {
                Console.WriteLine($"Runbook nesting depth exceeded (max {MaxRunbookDepth}). Check for circular runbook calls.");
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

            env.Logger.LogCommand("runbook", $"Starting '{runbookName}' ({runbook.Commands.Count} commands, depth={depth})");
            Console.WriteLine($"[runbook] Executing '{runbookName}' ({runbook.Commands.Count} commands)");

            for (int i = 0; i < runbook.Commands.Count; i++)
            {
                string line = runbook.Commands[i]
                    .Replace("{workSourcePath}",  env.WorkSource      ?? "")
                    .Replace("{workspaceFolder}", env.WorkspaceFolder ?? "")
                    .Replace("{userPrompt}",      userPrompt          ?? "");

                Console.WriteLine($"[runbook:{runbookName}] ({i + 1}/{runbook.Commands.Count}) {line}");
                bool success = DispatchCommand(env, SplitArgs(line), depth + 1);
                if (!success)
                {
                    Console.WriteLine($"[runbook:{runbookName}] Stopped at command {i + 1} due to error.");
                    env.Logger.LogError($"Runbook '{runbookName}' stopped at command {i + 1}: {line}", "runbook");
                    return false;
                }
            }

            Console.WriteLine($"[runbook:{runbookName}] Completed ({runbook.Commands.Count} commands).");
            env.Logger.LogInfo($"Runbook '{runbookName}' completed ({runbook.Commands.Count} commands).");
            return true;
        }

        // ?? Workspace inspection ?????????????????????????????????????????????

        public static void HandleListRunbooks(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-runbooks") == null) return;
            env.Logger.LogCommand("list-runbooks");
            var runbooks = env.Runbooks;
            Console.WriteLine($"Runbooks ({runbooks.Count}):");
            if (runbooks.Count == 0) { Console.WriteLine($"  (none \u2014 add .wrb files to {env.WorkspaceFolder}/Runbooks/)"); return; }
            foreach (var rb in runbooks)
            {
                Console.WriteLine($"  [{rb.Name}]");
                if (!string.IsNullOrWhiteSpace(rb.Description)) Console.WriteLine($"    Description: {rb.Description}");
                Console.WriteLine($"    Commands: {rb.Commands.Count}");
                Console.WriteLine($"    File:     {rb.FilePath}");
            }
        }

        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;
            env.Logger.LogCommand("list");
            var ws = env.Workspace!;
            Console.WriteLine($"Actors ({ws.Actors.Count}):");
            if (ws.Actors.Count == 0) Console.WriteLine($"  (none — add a subfolder with actor.json to {ws.WorkspaceFolder}/Actors/)");
            foreach (var actor in ws.Actors)
            {
                Console.WriteLine($"  [{actor.Name}]  folder: {actor.FolderPath}");
                PrintRbaLine("    Role",     actor.RolePrompt);
                PrintRbaLine("    Criteria", actor.CriteriaPrompt);
                PrintRbaLine("    Intent",   actor.IntentPrompt);
                if (!string.IsNullOrEmpty(actor.FolderPath))
                {
                    string docsPath = Path.Combine(actor.FolderPath, actor.DocsFolderName);
                    if (Directory.Exists(docsPath)) Console.WriteLine($"    Docs folder: {docsPath}");
                    if (Directory.Exists(Path.Combine(actor.FolderPath, WallyHelper.MailboxInboxFolderName)))
                        Console.WriteLine($"    Mailbox: Inbox / Outbox / Pending / Active");
                }
            }
        }

        public static void HandleInfo(WallyEnvironment env)
        {
            env.Logger.LogCommand("info");
            if (!env.HasWorkspace)
            {
                Console.WriteLine("Status:           No workspace loaded.");
                Console.WriteLine("                  Use 'load <path>' or 'setup <path>' first.");
                return;
            }
            var ws  = env.Workspace!;
            var cfg = ws.Config;
            Console.WriteLine($"Status:           Workspace loaded");
            Console.WriteLine($"WorkSource:       {ws.WorkSource}");
            Console.WriteLine($"Workspace folder: {ws.WorkspaceFolder}");
            Console.WriteLine($"Actors folder:    {Path.Combine(ws.WorkspaceFolder, cfg.ActorsFolderName)}");
            Console.WriteLine($"Docs folder:      {Path.Combine(ws.WorkspaceFolder, cfg.DocsFolderName)}");
            Console.WriteLine($"Templates folder: {Path.Combine(ws.WorkspaceFolder, cfg.TemplatesFolderName)}");
            Console.WriteLine($"Logs folder:      {Path.Combine(ws.WorkspaceFolder, cfg.LogsFolderName)}");
            bool hasMailbox = Directory.Exists(Path.Combine(ws.WorkspaceFolder, WallyHelper.MailboxInboxFolderName));
            Console.WriteLine($"Workspace mailbox:{(hasMailbox ? " Inbox / Outbox / Pending / Active" : " (not initialised — run setup)")}");
            Console.WriteLine($"Actors:           {ws.Actors.Count}");
            foreach (var a in ws.Actors)
            {
                bool actorHasMailbox = !string.IsNullOrEmpty(a.FolderPath) &&
                    Directory.Exists(Path.Combine(a.FolderPath, WallyHelper.MailboxInboxFolderName));
                Console.WriteLine($"  {a.Name}{(actorHasMailbox ? "  [mailbox]" : "")}");
            }
            Console.WriteLine($"Loops:            {ws.Loops.Count}");
            foreach (var l in ws.Loops)       Console.WriteLine($"  {l.Name}{(string.IsNullOrWhiteSpace(l.Description) ? "" : $" \u2014 {l.Description}")}");
            Console.WriteLine($"Wrappers:         {ws.LlmWrappers.Count}");
            foreach (var w in ws.LlmWrappers) Console.WriteLine($"  {w.Name}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $" \u2014 {w.Description}")}");
            Console.WriteLine($"Runbooks:         {ws.Runbooks.Count}");
            foreach (var r in ws.Runbooks)    Console.WriteLine($"  {r.Name}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" \u2014 {r.Description}")}");
            Console.WriteLine();
            Console.WriteLine($"Default model:    {cfg.DefaultModel   ?? "(none)"}");
            Console.WriteLine($"Default wrapper:  {cfg.DefaultWrapper ?? "(none)"}");
            if (!string.IsNullOrEmpty(cfg.ResolvedDefaultLoop))    Console.WriteLine($"Default loop:     {cfg.ResolvedDefaultLoop}");
            if (!string.IsNullOrEmpty(cfg.ResolvedDefaultRunbook)) Console.WriteLine($"Default runbook:  {cfg.ResolvedDefaultRunbook}");
            if (cfg.DefaultModels.Count   > 0) Console.WriteLine($"Models:           {string.Join(", ", cfg.DefaultModels)}");
            if (cfg.DefaultWrappers.Count > 0) Console.WriteLine($"Wrappers:         {string.Join(", ", cfg.DefaultWrappers)}");
            Console.WriteLine();
            Console.WriteLine($"Session ID:       {env.Logger.SessionId:N}");
            Console.WriteLine($"Session started:  {env.Logger.StartedAt:u}");
            Console.WriteLine($"Session log:      {env.Logger.LogFolder ?? "(not bound)"}");
            Console.WriteLine($"Current log file: {env.Logger.CurrentLogFile ?? "(none)"}");
            Console.WriteLine($"Log rotation:     {(cfg.LogRotationMinutes > 0 ? $"every {cfg.LogRotationMinutes} min" : "disabled")}");
        }

        public static void HandleReloadActors(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "reload-actors") == null) return;
            env.ReloadActors();
            env.Logger.LogCommand("reload-actors", $"Reloaded {env.Actors.Count} actors");
            Console.WriteLine($"Actors reloaded: {env.Actors.Count}");
            foreach (var a in env.Actors) Console.WriteLine($"  {a.Name}");
        }

        public static void HandleListLoops(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-loops") == null) return;
            env.Logger.LogCommand("list-loops");
            var loops = env.Loops;
            Console.WriteLine($"Loops ({loops.Count}):");
            if (loops.Count == 0) { Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Loops/)"); return; }

            foreach (var loop in loops)
            {
                Console.WriteLine($"  [{loop.Name}]");
                if (!string.IsNullOrWhiteSpace(loop.Description))
                    Console.WriteLine($"    Description: {loop.Description}");

                if (loop.HasSteps)
                {
                    Console.WriteLine($"    Mode:        pipeline ({loop.Steps.Count} step(s))");
                    for (int i = 0; i < loop.Steps.Count; i++)
                    {
                        var s = loop.Steps[i];
                        string actorDisplay = string.IsNullOrWhiteSpace(s.ActorName)
                            ? (string.IsNullOrWhiteSpace(loop.ActorName) ? "(direct mode)" : loop.ActorName + " (fallback)")
                            : s.ActorName;
                        Console.WriteLine($"    Step {i + 1}: [{(string.IsNullOrWhiteSpace(s.Name) ? $"step-{i+1}" : s.Name)}]  Actor: {actorDisplay}");
                        if (!string.IsNullOrWhiteSpace(s.Description))
                            Console.WriteLine($"             {s.Description}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Mode:        single-actor");
                    Console.WriteLine($"    Actor:       {(string.IsNullOrWhiteSpace(loop.ActorName) ? "(caller must specify)" : loop.ActorName)}");
                    PrintRbaLine("    Prompt", loop.StartPrompt);
                }
            }
        }

        // ?? Cleanup ??????????????????????????????????????????????????????????

        public static void HandleCleanup(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder = ResolveWorkspaceFolder(workSourcePath);
            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine($"Nothing to clean \u2014 workspace folder does not exist: {wsFolder}");
                env.Logger.LogCommand("cleanup", $"No workspace at {wsFolder}");
                return;
            }
            if (env.HasWorkspace && string.Equals(
                    Path.GetFullPath(env.Workspace!.WorkspaceFolder),
                    Path.GetFullPath(wsFolder), StringComparison.OrdinalIgnoreCase))
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

        public static void HandleClearHistory(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "clear-history") == null) return;
            env.History.ClearHistory();
            env.Logger.LogCommand("clear-history", "Conversation history cleared.");
            Console.WriteLine("Conversation history cleared.");
        }

        // ?? Help / Tutorial ??????????????????????????????????????????????????

        public static void HandleHelp()
        {
            Console.WriteLine("Wally \u2014 AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("Quick start:");
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine("  wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine("  wally run \"What is this project about?\"");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  setup [<path>] [--verify]     Set up / verify a workspace.");
            Console.WriteLine("  repair [<path>]               Add any missing workspace components.");
            Console.WriteLine("  load <path>                   Load an existing .wally/ workspace.");
            Console.WriteLine("  info                          Show workspace info and session details.");
            Console.WriteLine("  tutorial                      Step-by-step guide.");
            Console.WriteLine("  commands                      Show this help message.");
            Console.WriteLine();
            Console.WriteLine("  run \"<prompt>\" [options]");
            Console.WriteLine("    -a, --actor <name>          Actor to use (adds RBA context).");
            Console.WriteLine("    -m, --model <model>         Override the AI model.");
            Console.WriteLine("    -w, --wrapper <name>        Override the LLM wrapper.");
            Console.WriteLine("    -l, --loop-name <name>      Run a named pipeline definition.");
            Console.WriteLine("    --no-history                Suppress conversation history injection.");
            Console.WriteLine();
            Console.WriteLine("  runbook <name> [\"<prompt>\"]   Execute a runbook (.wrb command sequence).");
            Console.WriteLine();
            Console.WriteLine("  Actors:   list | add-actor | edit-actor | delete-actor | reload-actors");
            Console.WriteLine("  Loops:    list-loops | add-loop | edit-loop | delete-loop");
            Console.WriteLine("  Wrappers: list-wrappers | add-wrapper | edit-wrapper | delete-wrapper");
            Console.WriteLine("  Runbooks: list-runbooks | add-runbook | edit-runbook | delete-runbook");
            Console.WriteLine();
            Console.WriteLine("  save <path> | cleanup [<path>] | clear-history");
            Console.WriteLine();
            Console.WriteLine("Mailbox system:");
            Console.WriteLine("  Every workspace and each actor gets four folders created on setup:");
            Console.WriteLine("    Inbox/    — incoming requests and documents");
            Console.WriteLine("    Outbox/   — completed deliverables ready for handoff");
            Console.WriteLine("    Pending/  — items awaiting action or approval");
            Console.WriteLine("    Active/   — work currently in progress");
            Console.WriteLine("  The workspace mailbox (.wally/Inbox/, etc.) is the shared coordination");
            Console.WriteLine("  space. Actor mailboxes (<Actor>/Inbox/, etc.) are private to each actor.");
            Console.WriteLine();
            Console.WriteLine("Pipeline loops:");
            Console.WriteLine("  Define a steps array in the loop JSON. Each step names an actor and a");
            Console.WriteLine("  promptTemplate. Steps run in order; each receives the previous step's");
            Console.WriteLine("  output via {previousStepResult}.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  .\\wally run \"What does this codebase do?\"");
            Console.WriteLine("  .\\wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine("  .\\wally run \"Review the auth module\" -l CodeReview");
            Console.WriteLine("  .\\wally run \"Analyse auth module\" -l AnalyseAndReview");
            Console.WriteLine("  .\\wally add-actor SecurityAuditor -r \"You are a security auditor\"");
        }

        public static void HandleTutorial()
        {
            Console.WriteLine("Wally \u2014 Getting Started Tutorial");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("STEP 1: SET UP A WORKSPACE");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine();
            Console.WriteLine("  This creates .wally/ inside your codebase root, including:");
            Console.WriteLine("    Inbox/, Outbox/, Pending/, Active/  — workspace shared mailbox");
            Console.WriteLine("    Actors/, Docs/, Templates/, Loops/, Wrappers/, Runbooks/, Logs/");
            Console.WriteLine();
            Console.WriteLine("STEP 2: RUN A PROMPT");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally run \"What does this codebase do?\"");
            Console.WriteLine("  wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine();
            Console.WriteLine("STEP 3: ADD AN ACTOR");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally add-actor SecurityAuditor -r \"You are a security auditor\" -c \"Find vulnerabilities\" -i \"Produce a security report\"");
            Console.WriteLine();
            Console.WriteLine("  Each actor gets its own mailbox folders automatically:");
            Console.WriteLine("    .wally/Actors/SecurityAuditor/Inbox/");
            Console.WriteLine("    .wally/Actors/SecurityAuditor/Outbox/");
            Console.WriteLine("    .wally/Actors/SecurityAuditor/Pending/");
            Console.WriteLine("    .wally/Actors/SecurityAuditor/Active/");
            Console.WriteLine();
            Console.WriteLine("STEP 4: USE A PIPELINE LOOP");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally run \"Review the auth module\" -l CodeReview");
            Console.WriteLine();
            Console.WriteLine("Define your own pipeline in .wally/Loops/MyLoop.json:");
            Console.WriteLine("  {");
            Console.WriteLine("    \"name\": \"MyLoop\",");
            Console.WriteLine("    \"steps\": [");
            Console.WriteLine("      { \"name\": \"Analyse\", \"actorName\": \"BusinessAnalyst\", \"promptTemplate\": \"{userPrompt}\"},");
            Console.WriteLine("      { \"name\": \"Review\",  \"actorName\": \"Engineer\", \"promptTemplate\": \"{previousStepResult}\\n\\nOriginal: {userPrompt}\" }");
            Console.WriteLine("    ]");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("STEP 5: INSPECT & EXPLORE");
            Console.WriteLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            Console.WriteLine("  wally info | wally list | wally list-loops | wally list-wrappers");
            Console.WriteLine("  wally commands");
        }

        // ?? List wrappers ????????????????????????????????????????????????????

        public static void HandleListWrappers(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-wrappers") == null) return;
            env.Logger.LogCommand("list-wrappers");
            var wrappers = env.Workspace!.LlmWrappers;
            Console.WriteLine($"Wrappers ({wrappers.Count}):");
            if (wrappers.Count == 0) { Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Wrappers/)"); return; }
            foreach (var w in wrappers)
            {
                Console.WriteLine($"  [{w.Name}]");
                if (!string.IsNullOrWhiteSpace(w.Description)) Console.WriteLine($"    Description:  {w.Description}");
                Console.WriteLine($"    Executable:   {w.Executable}");
                Console.WriteLine($"    Template:     {w.ArgumentTemplate}");
                Console.WriteLine($"    CanMakeChanges:         {w.CanMakeChanges}");
                Console.WriteLine($"    UseConversationHistory: {w.UseConversationHistory}");
            }
        }

        // ?? Actor CRUD ???????????????????????????????????????????????????????

        public static void HandleAddActor(WallyEnvironment env, string name, string rolePrompt, string criteriaPrompt, string intentPrompt)
        {
            if (RequireWorkspace(env, "add-actor") == null) return;
            if (env.GetActor(name) != null) { Console.WriteLine($"Actor '{name}' already exists. Use 'edit-actor' to modify it."); return; }
            var ws = env.Workspace!;
            string actorDir = Path.Combine(ws.WorkspaceFolder, ws.Config.ActorsFolderName, name);
            var actor = new Actor(name, actorDir, rolePrompt, criteriaPrompt, intentPrompt, ws);
            WallyHelper.SaveActor(ws.WorkspaceFolder, ws.Config, actor);
            env.ReloadActors();
            env.Logger.LogCommand("add-actor", $"Created actor '{name}'");
            Console.WriteLine($"Actor '{name}' created at: {actorDir}");
            Console.WriteLine($"  Mailbox: {WallyHelper.MailboxInboxFolderName} / {WallyHelper.MailboxOutboxFolderName} / {WallyHelper.MailboxPendingFolderName} / {WallyHelper.MailboxActiveFolderName}");
        }

        public static void HandleEditActor(WallyEnvironment env, string name, string? rolePrompt, string? criteriaPrompt, string? intentPrompt)
        {
            if (RequireWorkspace(env, "edit-actor") == null) return;
            var actor = env.GetActor(name);
            if (actor == null) { Console.WriteLine($"Actor '{name}' not found."); foreach (var a in env.Actors) Console.WriteLine($"  {a.Name}"); return; }
            if (rolePrompt     != null) actor.RolePrompt     = rolePrompt;
            if (criteriaPrompt != null) actor.CriteriaPrompt = criteriaPrompt;
            if (intentPrompt   != null) actor.IntentPrompt   = intentPrompt;
            WallyHelper.SaveActor(env.Workspace!.WorkspaceFolder, env.Workspace.Config, actor);
            env.Logger.LogCommand("edit-actor", $"Updated actor '{name}'");
            Console.WriteLine($"Actor '{name}' updated.");
        }

        public static void HandleDeleteActor(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-actor") == null) return;
            var actor = env.GetActor(name);
            if (actor == null) { Console.WriteLine($"Actor '{name}' not found."); return; }
            string actorDir = Path.Combine(env.Workspace!.WorkspaceFolder, env.Workspace.Config.ActorsFolderName, name);
            if (Directory.Exists(actorDir))
            {
                Directory.Delete(actorDir, recursive: true);
                env.ReloadActors();
                env.Logger.LogCommand("delete-actor", $"Deleted actor '{name}'");
                Console.WriteLine($"Actor '{name}' deleted.");
            }
            else Console.WriteLine($"Actor folder not found: {actorDir}");
        }

        // ?? Loop CRUD ????????????????????????????????????????????????????????

        public static void HandleAddLoop(WallyEnvironment env, string name, string description, string actorName, string startPrompt)
        {
            if (RequireWorkspace(env, "add-loop") == null) return;
            if (env.GetLoop(name) != null) { Console.WriteLine($"Loop '{name}' already exists. Use 'edit-loop' to modify it."); return; }
            var ws = env.Workspace!;
            string loopsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName);
            Directory.CreateDirectory(loopsDir);
            var loop = new WallyLoopDefinition { Name = name, Description = description, ActorName = actorName, StartPrompt = startPrompt };
            string filePath = Path.Combine(loopsDir, $"{name}.json");
            loop.SaveToFile(filePath);
            ws.ReloadLoops();
            env.Logger.LogCommand("add-loop", $"Created loop '{name}'");
            Console.WriteLine($"Loop '{name}' created at: {filePath}");
        }

        public static void HandleEditLoop(WallyEnvironment env, string name, string? description, string? actorName, string? startPrompt)
        {
            if (RequireWorkspace(env, "edit-loop") == null) return;
            var loop = env.GetLoop(name);
            if (loop == null) { Console.WriteLine($"Loop '{name}' not found."); foreach (var l in env.Loops) Console.WriteLine($"  {l.Name}"); return; }
            if (description != null) loop.Description = description;
            if (actorName   != null) loop.ActorName   = actorName;
            if (startPrompt != null) loop.StartPrompt = startPrompt;
            string filePath = Path.Combine(env.Workspace!.WorkspaceFolder, env.Workspace.Config.LoopsFolderName, $"{name}.json");
            loop.SaveToFile(filePath);
            env.Logger.LogCommand("edit-loop", $"Updated loop '{name}'");
            Console.WriteLine($"Loop '{name}' updated.");
        }

        public static void HandleDeleteLoop(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-loop") == null) return;
            string filePath = Path.Combine(env.Workspace!.WorkspaceFolder, env.Workspace.Config.LoopsFolderName, $"{name}.json");
            if (!File.Exists(filePath)) { Console.WriteLine($"Loop file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Workspace.ReloadLoops();
            env.Logger.LogCommand("delete-loop", $"Deleted loop '{name}'");
            Console.WriteLine($"Loop '{name}' deleted.");
        }

        // ?? Wrapper CRUD ?????????????????????????????????????????????????????

        public static void HandleAddWrapper(WallyEnvironment env, string name, string description, string executable, string argumentTemplate, bool canMakeChanges, bool useConversationHistory = true)
        {
            if (RequireWorkspace(env, "add-wrapper") == null) return;
            if (env.Workspace!.LlmWrappers.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
            { Console.WriteLine($"Wrapper '{name}' already exists. Use 'edit-wrapper' to modify it."); return; }
            var ws = env.Workspace!;
            string wrappersDir = Path.Combine(ws.WorkspaceFolder, ws.Config.WrappersFolderName);
            Directory.CreateDirectory(wrappersDir);
            var wrapper = new LLMWrapper { Name = name, Description = description, Executable = executable, ArgumentTemplate = argumentTemplate, CanMakeChanges = canMakeChanges, UseConversationHistory = useConversationHistory };
            string filePath = Path.Combine(wrappersDir, $"{name}.json");
            wrapper.SaveToFile(filePath);
            ws.ReloadWrappers();
            env.Logger.LogCommand("add-wrapper", $"Created wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' created at: {filePath}");
        }

        public static void HandleEditWrapper(WallyEnvironment env, string name, string? description, string? executable, string? argumentTemplate, bool? canMakeChanges, bool? useConversationHistory = null)
        {
            if (RequireWorkspace(env, "edit-wrapper") == null) return;
            var wrapper = env.Workspace!.LlmWrappers.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
            if (wrapper == null) { Console.WriteLine($"Wrapper '{name}' not found."); foreach (var w in env.Workspace.LlmWrappers) Console.WriteLine($"  {w.Name}"); return; }
            if (description             != null) wrapper.Description            = description;
            if (executable              != null) wrapper.Executable             = executable;
            if (argumentTemplate        != null) wrapper.ArgumentTemplate       = argumentTemplate;
            if (canMakeChanges.HasValue)         wrapper.CanMakeChanges         = canMakeChanges.Value;
            if (useConversationHistory.HasValue) wrapper.UseConversationHistory = useConversationHistory.Value;
            string filePath = Path.Combine(env.Workspace.WorkspaceFolder, env.Workspace.Config.WrappersFolderName, $"{name}.json");
            wrapper.SaveToFile(filePath);
            env.Logger.LogCommand("edit-wrapper", $"Updated wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' updated.");
        }

        public static void HandleDeleteWrapper(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-wrapper") == null) return;
            string filePath = Path.Combine(env.Workspace!.WorkspaceFolder, env.Workspace.Config.WrappersFolderName, $"{name}.json");
            if (!File.Exists(filePath)) { Console.WriteLine($"Wrapper file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Workspace.ReloadWrappers();
            env.Logger.LogCommand("delete-wrapper", $"Deleted wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' deleted.");
        }

        // ?? Runbook CRUD ?????????????????????????????????????????????????????

        public static void HandleAddRunbook(WallyEnvironment env, string name, string description)
        {
            if (RequireWorkspace(env, "add-runbook") == null) return;
            if (env.GetRunbook(name) != null) { Console.WriteLine($"Runbook '{name}' already exists. Use 'edit-runbook' to modify it."); return; }
            var ws = env.Workspace!;
            string runbooksDir = Path.Combine(ws.WorkspaceFolder, ws.Config.RunbooksFolderName);
            Directory.CreateDirectory(runbooksDir);
            var runbook = new WallyRunbook { Name = name, Description = description, Commands = new List<string>(), FilePath = Path.Combine(runbooksDir, $"{name}.wrb") };
            WallyHelper.SaveRunbook(ws.WorkspaceFolder, ws.Config, runbook);
            env.Logger.LogCommand("add-runbook", $"Created runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' created. Edit {runbook.FilePath} to add commands.");
        }

        public static void HandleEditRunbook(WallyEnvironment env, string name, string? description)
        {
            if (RequireWorkspace(env, "edit-runbook") == null) return;
            var runbook = env.GetRunbook(name);
            if (runbook == null) { Console.WriteLine($"Runbook '{name}' not found."); foreach (var r in env.Runbooks) Console.WriteLine($"  {r.Name}"); return; }
            if (description != null) runbook.Description = description;
            WallyHelper.SaveRunbook(env.Workspace!.WorkspaceFolder, env.Workspace.Config, runbook);
            env.Logger.LogCommand("edit-runbook", $"Updated runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' updated.");
        }

        public static void HandleDeleteRunbook(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-runbook") == null) return;
            string filePath = Path.Combine(env.Workspace!.WorkspaceFolder, env.Workspace.Config.RunbooksFolderName, $"{name}.wrb");
            if (!File.Exists(filePath)) { Console.WriteLine($"Runbook file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Logger.LogCommand("delete-runbook", $"Deleted runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' deleted.");
        }

        // ?? Private helpers ??????????????????????????????????????????????????

        private static string ResolveWorkspaceFolder(string? workSourcePath)
        {
            if (!string.IsNullOrWhiteSpace(workSourcePath) && Directory.Exists(workSourcePath))
                return Path.Combine(Path.GetFullPath(workSourcePath), ".wally");
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(Path.GetFullPath(exeFolder), "..", "..", "..", ".wally");
        }

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            Console.WriteLine(header);
            Console.WriteLine(new string('=', header.Length));
            Console.WriteLine();
            Console.WriteLine($"WorkSource:       {env.WorkSource}");
            Console.WriteLine($"Workspace folder: {env.WorkspaceFolder}");
            Console.WriteLine($"Actors folder:    {Path.Combine(env.WorkspaceFolder!, env.Workspace!.Config.ActorsFolderName)}");
            Console.WriteLine($"Docs folder:      {Path.Combine(env.WorkspaceFolder!, env.Workspace!.Config.DocsFolderName)}");
            Console.WriteLine($"Templates folder: {Path.Combine(env.WorkspaceFolder!, env.Workspace!.Config.TemplatesFolderName)}");
            Console.WriteLine($"Logs folder:      {Path.Combine(env.WorkspaceFolder!, env.Workspace!.Config.LogsFolderName)}");
            Console.WriteLine($"Workspace mailbox:{Path.Combine(env.WorkspaceFolder!, WallyHelper.MailboxInboxFolderName)} / Outbox / Pending / Active");
        }

        private static void PrintRbaLine(string label, string value) =>
            Console.WriteLine($"{label}: {(string.IsNullOrWhiteSpace(value) ? "(none)" : value)}");

        private static string? GetOption(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private static bool HasFlag(string[] args, string flag) =>
            args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        private static string? GetFirstPositional(string[] args, int startIndex) =>
            args.Length > startIndex ? args[startIndex] : null;
    }
}