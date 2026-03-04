using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using Wally.Console.Options;
using Wally.Core;

namespace Wally.Console
{
    public static class Program
    {
        /// <summary>
        /// The single Wally environment for this process lifetime.
        /// Created once at startup; a workspace is loaded into it via 'load' or 'create'.
        /// </summary>
        private static readonly WallyEnvironment _environment = new WallyEnvironment();

        public static int Main(string[] args)
        {
            System.Console.OutputEncoding = Encoding.UTF8;

            try
            {
                if (args.Length == 0)
                    return RunInteractiveMode();
                else
                    return RunOneTimeMode(args);
            }
            finally
            {
                _environment.Logger.Dispose();
            }
        }

        private static int RunOneTimeMode(string[] args) => HandleArguments(args);

        private static int RunInteractiveMode()
        {
            System.Console.WriteLine("Wally Interactive Mode. Type 'help' for commands, 'exit' to quit.");
            while (true)
            {
                System.Console.Write("wally> ");
                string? input = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string[] interactiveArgs = WallyCommands.SplitArgs(input);
                if (interactiveArgs.Length > 0)
                    WallyCommands.DispatchCommand(_environment, interactiveArgs);
            }
            return 0;
        }

        private static int HandleArguments(string[] args)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(WallyEnvironment).Assembly };
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            var result = Parser.Default.ParseArguments(args, types);
            return result.MapResult(
                (opts) =>
                {
                    // ── Workspace lifecycle ───────────────────────────────────
                    if (opts is LoadOptions lo)        { WallyCommands.HandleLoad(_environment, lo.Path); return 0; }
                    if (opts is SaveOptions so)        { WallyCommands.HandleSave(_environment, so.Path); return 0; }
                    if (opts is SetupOptions seto)     { WallyCommands.HandleSetup(_environment, seto.ResolvedPath, seto.Verify); return 0; }

                    // ── Running actors ────────────────────────────────────────
                    if (opts is RunOptions ro)
                    {
                        WallyCommands.HandleRun(
                            _environment,
                            ro.Prompt,
                            ro.ActorName,
                            ro.Model,
                            ro.Looped,
                            ro.LoopName,
                            ro.MaxIterations,
                            ro.Wrapper);
                        return 0;
                    }

                    // ── Runbooks ───────────────────────────────────────────────
                    if (opts is RunbookOptions rbo)       { WallyCommands.HandleRunbook(_environment, rbo.Name, rbo.Prompt); return 0; }
                    if (opts is ListRunbooksOptions)       { WallyCommands.HandleListRunbooks(_environment); return 0; }

                    // ── Inspection ────────────────────────────────────────────
                    if (opts is ListOptions)              { WallyCommands.HandleList(_environment); return 0; }
                    if (opts is ListLoopsOptions)         { WallyCommands.HandleListLoops(_environment); return 0; }
                    if (opts is ListWrappersOptions)      { WallyCommands.HandleListWrappers(_environment); return 0; }
                    if (opts is InfoOptions)              { WallyCommands.HandleInfo(_environment); return 0; }
                    if (opts is ReloadActorsOptions)      { WallyCommands.HandleReloadActors(_environment); return 0; }
                    if (opts is CleanupOptions co)        { WallyCommands.HandleCleanup(_environment, co.Path); return 0; }

                    // ── Help & Tutorial ───────────────────────────────────────
                    if (opts is HelpOptions)              { WallyCommands.HandleHelp(); return 0; }
                    if (opts is TutorialOptions)          { WallyCommands.HandleTutorial(); return 0; }

                    // ── Actor CRUD ────────────────────────────────────────────
                    if (opts is AddActorOptions aao)      { WallyCommands.HandleAddActor(_environment, aao.Name, aao.RolePrompt, aao.CriteriaPrompt, aao.IntentPrompt); return 0; }
                    if (opts is EditActorOptions eao)     { WallyCommands.HandleEditActor(_environment, eao.Name, eao.RolePrompt, eao.CriteriaPrompt, eao.IntentPrompt); return 0; }
                    if (opts is DeleteActorOptions dao)   { WallyCommands.HandleDeleteActor(_environment, dao.Name); return 0; }

                    // ── Loop CRUD ─────────────────────────────────────────────
                    if (opts is AddLoopOptions alo)       { WallyCommands.HandleAddLoop(_environment, alo.Name, alo.Description, alo.ActorName, alo.MaxIterations, alo.StartPrompt); return 0; }
                    if (opts is EditLoopOptions elo)      { WallyCommands.HandleEditLoop(_environment, elo.Name, elo.Description, elo.ActorName, elo.MaxIterations, elo.StartPrompt); return 0; }
                    if (opts is DeleteLoopOptions dlo)    { WallyCommands.HandleDeleteLoop(_environment, dlo.Name); return 0; }

                    // ── Wrapper CRUD ──────────────────────────────────────────
                    if (opts is AddWrapperOptions awo)    { WallyCommands.HandleAddWrapper(_environment, awo.Name, awo.Description, awo.Executable, awo.ArgumentTemplate, awo.CanMakeChanges); return 0; }
                    if (opts is EditWrapperOptions ewo)   { WallyCommands.HandleEditWrapper(_environment, ewo.Name, ewo.Description, ewo.Executable, ewo.ArgumentTemplate, ewo.CanMakeChanges); return 0; }
                    if (opts is DeleteWrapperOptions dwo) { WallyCommands.HandleDeleteWrapper(_environment, dwo.Name); return 0; }

                    // ── Runbook CRUD ──────────────────────────────────────────
                    if (opts is AddRunbookOptions aro)    { WallyCommands.HandleAddRunbook(_environment, aro.Name, aro.Description); return 0; }
                    if (opts is EditRunbookOptions ero)   { WallyCommands.HandleEditRunbook(_environment, ero.Name, ero.Description); return 0; }
                    if (opts is DeleteRunbookOptions dro) { WallyCommands.HandleDeleteRunbook(_environment, dro.Name); return 0; }

                    return 0;
                },
                errs =>
                {
                    System.Console.WriteLine("Invalid command. Type 'help' for available commands.");
                    return 1;
                }
            );
        }
    }
}

