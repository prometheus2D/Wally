using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Actors;
using Wally.Core.Mailbox;
using Wally.Core.Providers;
using Wally.Core.Scripting;

namespace Wally.Core
{
    /// <summary>
    /// Entry point for all Wally command dispatch.
    /// The implementation is split across partial class files in the Commands/ folder:
    /// <list type="bullet">
    ///   <item><c>WallyCommands.Workspace.cs</c> — load / setup / repair / save / cleanup</item>
    ///   <item><c>WallyCommands.Run.cs</c>       — run, pipeline execution</item>
    ///   <item><c>WallyCommands.Runbook.cs</c>   — runbook execution, script interpreter</item>
    ///   <item><c>WallyCommands.Crud.cs</c>      — actor / loop / wrapper / runbook CRUD</item>
    ///   <item><c>WallyCommands.Info.cs</c>      — list, info, help, tutorial</item>
    /// </list>
    /// </summary>
    public static partial class WallyCommands
    {
        private const int MaxRunbookDepth = 10;

        private static readonly string[] _knownVerbs =
        {
            "setup", "repair", "load", "save", "run", "runbook", "list", "list-loops",
            "list-wrappers", "list-runbooks", "info", "reload-actors", "cleanup",
            "clear-history", "commands", "help", "tutorial", "tutorial-mode", "clear", "cls",
            "add-actor", "edit-actor", "delete-actor",
            "add-loop", "edit-loop", "delete-loop",
            "add-wrapper", "edit-wrapper", "delete-wrapper",
            "add-runbook", "edit-runbook", "delete-runbook",
            "process-mailboxes", "route-outbox"
        };

        /// <summary>
        /// Gets the list of known command verbs for tab completion and validation.
        /// Single source of truth for both CLI and Forms UI.
        /// </summary>
        public static IReadOnlyList<string> GetVerbs() => _knownVerbs;

        /// <summary>Splits a command line string into arguments using the shared tokenizer.</summary>
        public static string[] SplitArgs(string input) => WallyArgParser.Tokenise(input);

        // ?? Shared guard ??????????????????????????????????????????????????????

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

        // ?? Shared formatting helpers ?????????????????????????????????????????

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
            Console.WriteLine($"Projects folder:  {Path.Combine(env.WorkspaceFolder!, env.Workspace!.Config.ProjectsFolderName)}");
        }

        private static void PrintRbaLine(string label, string value) =>
            Console.WriteLine($"{label}: {(string.IsNullOrWhiteSpace(value) ? "(none)" : value)}");

        // ?? Dispatcher ????????????????????????????????????????????????????????

        public static bool DispatchCommand(WallyEnvironment env, string[] args, int runbookDepth = 0)
        {
            if (args.Length == 0) return true;
            string verb = args[0].ToLowerInvariant();
            switch (verb)
            {
                case "setup":
                {
                    bool verify = WallyArgParser.HasFlag(args, "--verify");
                    string? path = WallyArgParser.GetFirstPositional(args, 1);
                    HandleSetup(env, path, verify);
                    return true;
                }
                case "repair":
                {
                    string? path = WallyArgParser.GetFirstPositional(args, 1);
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
                    string? actorName = WallyArgParser.GetOption(args, "-a", "--actor");
                    string? model     = WallyArgParser.GetOption(args, "-m", "--model");
                    string? wrapper   = WallyArgParser.GetOption(args, "-w", "--wrapper");
                    string? loopName  = WallyArgParser.GetOption(args, "-l", "--loop-name");
                    bool noHistory    = WallyArgParser.HasFlag(args, "--no-history");
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
                    string? cleanupPath = WallyArgParser.GetFirstPositional(args, 1);
                    HandleCleanup(env, cleanupPath);
                    return true;
                }
                case "clear-history": HandleClearHistory(env); return true;
                case "commands" or "help": HandleHelp();     return true;
                case "tutorial":           HandleTutorial(); return true;
                case "tutorial-mode":
                {
                    string? modeValue = args.Length >= 2 ? args[1] : null;
                    HandleTutorialMode(modeValue);
                    return true;
                }

                // ?? Actor CRUD ????????????????????????????????????????????????
                case "add-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string role     = WallyArgParser.GetOption(args, "-r", "--role")     ?? "";
                    string criteria = WallyArgParser.GetOption(args, "-c", "--criteria") ?? "";
                    string intent   = WallyArgParser.GetOption(args, "-i", "--intent")   ?? "";
                    HandleAddActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "edit-actor":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-actor <name> [-r \"role\"] [-c \"criteria\"] [-i \"intent\"]"); return false; }
                    string? role     = WallyArgParser.GetOption(args, "-r", "--role");
                    string? criteria = WallyArgParser.GetOption(args, "-c", "--criteria");
                    string? intent   = WallyArgParser.GetOption(args, "-i", "--intent");
                    HandleEditActor(env, args[1], role, criteria, intent);
                    return true;
                }
                case "delete-actor":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-actor <name>"); return false; }
                    HandleDeleteActor(env, args[1]);
                    return true;

                // ?? Loop CRUD ?????????????????????????????????????????????????
                case "add-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-loop <name> [-d desc] [-a actor] [-s prompt]"); return false; }
                    string desc        = WallyArgParser.GetOption(args, "-d", "--description") ?? "";
                    string actor       = WallyArgParser.GetOption(args, "-a", "--actor")        ?? "";
                    string startPrompt = WallyArgParser.GetOption(args, "-s", "--start-prompt") ?? "{userPrompt}";
                    HandleAddLoop(env, args[1], desc, actor, startPrompt);
                    return true;
                }
                case "edit-loop":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-loop <name> [-d desc] [-a actor] [-s prompt]"); return false; }
                    string? desc        = WallyArgParser.GetOption(args, "-d", "--description");
                    string? actor       = WallyArgParser.GetOption(args, "-a", "--actor");
                    string? startPrompt = WallyArgParser.GetOption(args, "-s", "--start-prompt");
                    HandleEditLoop(env, args[1], desc, actor, startPrompt);
                    return true;
                }
                case "delete-loop":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-loop <name>"); return false; }
                    HandleDeleteLoop(env, args[1]);
                    return true;

                // ?? Wrapper CRUD ??????????????????????????????????????????????
                case "add-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes] [--no-conversation-history]"); return false; }
                    string desc     = WallyArgParser.GetOption(args, "-d", "--description") ?? "";
                    string exe      = WallyArgParser.GetOption(args, "-e", "--executable")  ?? "gh";
                    string template = WallyArgParser.GetOption(args, "-t", "--template")    ?? "";
                    bool canChange  = WallyArgParser.HasFlag(args, "--can-make-changes");
                    bool useHistory = !WallyArgParser.HasFlag(args, "--no-conversation-history");
                    HandleAddWrapper(env, args[1], desc, exe, template, canChange, useHistory);
                    return true;
                }
                case "edit-wrapper":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-wrapper <name> [-d desc] [-e exe] [-t template] [--can-make-changes] [--no-conversation-history]"); return false; }
                    string? desc     = WallyArgParser.GetOption(args, "-d", "--description");
                    string? exe      = WallyArgParser.GetOption(args, "-e", "--executable");
                    string? template = WallyArgParser.GetOption(args, "-t", "--template");
                    bool? canChange  = WallyArgParser.HasFlag(args, "--can-make-changes") ? true : null;
                    bool? useHistory = WallyArgParser.HasFlag(args, "--no-conversation-history") ? false : null;
                    HandleEditWrapper(env, args[1], desc, exe, template, canChange, useHistory);
                    return true;
                }
                case "delete-wrapper":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-wrapper <name>"); return false; }
                    HandleDeleteWrapper(env, args[1]);
                    return true;

                // ?? Runbook CRUD ??????????????????????????????????????????????
                case "add-runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: add-runbook <name> [-d desc]"); return false; }
                    string desc = WallyArgParser.GetOption(args, "-d", "--description") ?? "";
                    HandleAddRunbook(env, args[1], desc);
                    return true;
                }
                case "edit-runbook":
                {
                    if (args.Length < 2) { Console.WriteLine("Usage: edit-runbook <name> [-d desc]"); return false; }
                    string? desc = WallyArgParser.GetOption(args, "-d", "--description");
                    HandleEditRunbook(env, args[1], desc);
                    return true;
                }
                case "delete-runbook":
                    if (args.Length < 2) { Console.WriteLine("Usage: delete-runbook <name>"); return false; }
                    HandleDeleteRunbook(env, args[1]);
                    return true;

                // ?? Mailbox ???????????????????????????????????????????????????
                case "process-mailboxes":
                    HandleProcessMailboxes(env);
                    return true;
                case "route-outbox":
                    HandleRouteOutbox(env);
                    return true;

                default:
                    Console.WriteLine($"Unknown command: {verb}. Type 'commands' for help.");
                    return false;
            }
        }
    }
}
