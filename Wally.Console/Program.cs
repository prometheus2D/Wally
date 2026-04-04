using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using Wally.Console.Options.Actors;
using Wally.Console.Options.Inspection;
using Wally.Console.Options.Loops;
using Wally.Console.Options.Run;
using Wally.Console.Options.Runbooks;
using Wally.Console.Options.Workspace;
using Wally.Console.Options.Wrappers;
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

        // ── One-shot mode ────────────────────────────────────────────────────

        private static int RunOneTimeMode(string[] args)
        {
            string[] remaining = ExtractWorkspaceArg(args, out string? explicitWorkspacePath);
            remaining = ExtractExitFlag(remaining, out bool exitAfter);

            if (explicitWorkspacePath != null)
            {
                // --workspace flag supplied: load it before dispatching the verb.
                try
                {
                    WallyCommands.HandleLoad(_environment, explicitWorkspacePath);
                    WallyPreferencesStore.RecordWorkspaceLoaded(_environment.WorkspaceFolder!);
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(
                        $"Error: could not load workspace '{explicitWorkspacePath}': {ex.Message}");
                    return 1;
                }
            }
            else
            {
                // No explicit flag: silently attempt to auto-load the last used workspace.
                var prefs = WallyPreferencesStore.Load();
                if (prefs.AutoLoadLast
                    && !string.IsNullOrWhiteSpace(prefs.LastWorkspacePath)
                    && Directory.Exists(prefs.LastWorkspacePath))
                {
                    try
                    {
                        WallyCommands.HandleLoad(_environment, prefs.LastWorkspacePath);
                    }
                    catch
                    {
                        // Non-fatal — the verb may not need a workspace.
                    }
                }
            }

            int exitCode = HandleArguments(remaining);

            if (exitAfter)
                Environment.Exit(exitCode);

            return exitCode;
        }

        /// <summary>
        /// Scans <paramref name="args"/> for a <c>--exit</c> or <c>-x</c> flag,
        /// removes it, and returns whether it was present via <paramref name="exitAfter"/>.
        /// Returns the remaining args array. Does not mutate <paramref name="args"/>.
        /// <para>
        /// When present, the process will call <see cref="Environment.Exit"/> with the
        /// command's exit code immediately after the command completes. This enables
        /// one-shot usage from scripts or CI pipelines where the process must terminate
        /// regardless of whether it was launched in interactive or argument mode.
        /// </para>
        /// </summary>
        private static string[] ExtractExitFlag(string[] args, out bool exitAfter)
        {
            exitAfter = false;
            var result = new List<string>(args.Length);

            foreach (string arg in args)
            {
                if (arg.Equals("--exit", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-x",     StringComparison.OrdinalIgnoreCase))
                {
                    exitAfter = true;
                    continue;
                }
                result.Add(arg);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Scans <paramref name="args"/> for a <c>--workspace &lt;path&gt;</c>
        /// or <c>-ws &lt;path&gt;</c> pair, removes it, and returns the path via
        /// <paramref name="workspacePath"/>.  Returns the remaining args array.
        /// Does not mutate <paramref name="args"/>.
        /// </summary>
        private static string[] ExtractWorkspaceArg(string[] args, out string? workspacePath)
        {
            workspacePath = null;
            var result = new List<string>(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Equals("--workspace", StringComparison.OrdinalIgnoreCase) ||
                     args[i].Equals("-ws", StringComparison.OrdinalIgnoreCase))
                    && i + 1 < args.Length)
                {
                    workspacePath = args[i + 1];
                    i++; // skip the path token
                    continue;
                }
                result.Add(args[i]);
            }

            return result.ToArray();
        }

        // ── Interactive mode ─────────────────────────────────────────────────

        private static int RunInteractiveMode()
        {
            // ── Auto-load the last used workspace ────────────────────────────
            var prefs = WallyPreferencesStore.Load();
            if (prefs.AutoLoadLast
                && !string.IsNullOrWhiteSpace(prefs.LastWorkspacePath)
                && Directory.Exists(prefs.LastWorkspacePath))
            {
                try
                {
                    WallyCommands.HandleLoad(_environment, prefs.LastWorkspacePath);
                    WallyPreferencesStore.RecordWorkspaceLoaded(prefs.LastWorkspacePath);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Warning: could not auto-load last workspace: {ex.Message}");
                    WallyPreferencesStore.RemoveFromRecent(prefs.LastWorkspacePath);
                }
            }

            // ── Single banner line ────────────────────────────────────────────
            string workspaceLabel = _environment.HasWorkspace
                ? $"workspace: {_environment.WorkSource}"
                : "no workspace \u2014 use 'setup <path>' or 'load <path>'";
            System.Console.WriteLine($"Wally \u2014 {workspaceLabel}  ('exit' to quit)");

            // ── Tutorial hint (at most 2 lines, so total startup \u2264 3 lines) ───
            if (WallyPreferencesStore.Load().ShowTutorialOnStartup)
                WallyCommands.HandleTutorialSummary(_environment);

            while (true)
            {
                System.Console.Write("wally> ");
                string? input = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string[] interactiveArgs = WallyCommands.SplitArgs(input);
                if (interactiveArgs.Length == 0) continue;

                interactiveArgs = ExtractExitFlag(interactiveArgs, out bool exitAfter);
                if (interactiveArgs.Length == 0 && exitAfter) break;

                string verb = interactiveArgs[0].ToLowerInvariant();

                bool success = WallyCommands.DispatchCommand(_environment, interactiveArgs);

                // Update prefs after workspace-mutating commands.
                if (success)
                {
                    if ((verb == "load" || verb == "setup") && _environment.HasWorkspace)
                        WallyPreferencesStore.RecordWorkspaceLoaded(_environment.WorkspaceFolder!);
                    else if (verb == "cleanup" && interactiveArgs.Length >= 2)
                        WallyPreferencesStore.RemoveFromRecent(
                            Path.GetFullPath(interactiveArgs[1]));
                }

                if (exitAfter) break;
            }
            return 0;
        }

        // ── Argument dispatch ────────────────────────────────────────────────

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
                    if (opts is LoadOptions lo)
                    {
                        WallyCommands.HandleLoad(_environment, lo.Path);
                        if (_environment.HasWorkspace)
                            WallyPreferencesStore.RecordWorkspaceLoaded(_environment.WorkspaceFolder!);
                        return 0;
                    }
                    if (opts is SaveOptions so)    { WallyCommands.HandleSave(_environment, so.Path); return 0; }
                    if (opts is SetupOptions seto)
                    {
                        WallyCommands.HandleSetup(_environment, seto.ResolvedPath, seto.Verify);
                        if (_environment.HasWorkspace)
                            WallyPreferencesStore.RecordWorkspaceLoaded(_environment.WorkspaceFolder!);
                        return 0;
                    }
                    if (opts is CleanupOptions co)
                    {
                        string cleanupPath = co.Path != null
                            ? Path.GetFullPath(co.Path)
                            : (_environment.HasWorkspace
                                ? _environment.WorkspaceFolder!
                                : WallyHelper.GetDefaultWorkspaceFolder());
                        WallyCommands.HandleCleanup(_environment, co.Path);
                        WallyPreferencesStore.RemoveFromRecent(cleanupPath);
                        return 0;
                    }

                    // ── Running ───────────────────────────────────────────────
                    if (opts is RunOptions ro)
                    {
                        return HandleRunOptions(ro);
                    }
                    if (opts is RunbookOptions rbo) { WallyCommands.HandleRunbook(_environment, rbo.Name, rbo.Prompt); return 0; }
                    if (opts is DiagramOptions dio)
                    {
                        return WallyCommands.HandleDiagram(
                            _environment,
                            dio.TargetType,
                            dio.Name,
                            dio.SecondaryName,
                            dio.Format,
                            dio.OutputPath)
                            ? 0
                            : 1;
                    }

                    // ── Inspection ────────────────────────────────────────────
                    if (opts is InfoOptions)          { WallyCommands.HandleInfo(_environment); return 0; }
                    if (opts is HelpOptions)          { WallyCommands.HandleHelp(); return 0; }
                    if (opts is TutorialOptions)      { WallyCommands.HandleTutorial(); return 0; }
                    if (opts is TutorialModeOptions tmo)
                    {
                        WallyCommands.HandleTutorialMode(tmo.Value);
                        return 0;
                    }

                    // ── Actors ────────────────────────────────────────────────
                    if (opts is ListActorsOptions)      { WallyCommands.HandleList(_environment); return 0; }
                    if (opts is ReloadActorsOptions)    { WallyCommands.HandleReloadActors(_environment); return 0; }
                    if (opts is AddActorOptions aao)    { WallyCommands.HandleAddActor(_environment, aao.Name, aao.RolePrompt, aao.CriteriaPrompt, aao.IntentPrompt); return 0; }
                    if (opts is EditActorOptions eao)   { WallyCommands.HandleEditActor(_environment, eao.Name, eao.RolePrompt, eao.CriteriaPrompt, eao.IntentPrompt); return 0; }
                    if (opts is DeleteActorOptions dao) { WallyCommands.HandleDeleteActor(_environment, dao.Name); return 0; }

                    // ── Loops ─────────────────────────────────────────────────
                    if (opts is ListLoopsOptions)       { WallyCommands.HandleListLoops(_environment); return 0; }
                    if (opts is AddLoopOptions alo)     { WallyCommands.HandleAddLoop(_environment, alo.Name, alo.Description, alo.ActorName, alo.StartPrompt); return 0; }
                    if (opts is EditLoopOptions elo)    { WallyCommands.HandleEditLoop(_environment, elo.Name, elo.Description, elo.ActorName, elo.StartPrompt); return 0; }
                    if (opts is DeleteLoopOptions dlo)  { WallyCommands.HandleDeleteLoop(_environment, dlo.Name); return 0; }

                    // ── Wrappers ──────────────────────────────────────────────
                    if (opts is ListWrappersOptions)      { WallyCommands.HandleListWrappers(_environment); return 0; }
                    if (opts is AddWrapperOptions awo)    { WallyCommands.HandleAddWrapper(_environment, awo.Name, awo.Description, awo.Executable, awo.ArgumentTemplate, awo.CanMakeChanges, !awo.NoConversationHistory); return 0; }
                    if (opts is EditWrapperOptions ewo)   { WallyCommands.HandleEditWrapper(_environment, ewo.Name, ewo.Description, ewo.Executable, ewo.ArgumentTemplate, ewo.CanMakeChanges, ewo.NoConversationHistory.HasValue ? !ewo.NoConversationHistory.Value : null); return 0; }
                    if (opts is DeleteWrapperOptions dwo) { WallyCommands.HandleDeleteWrapper(_environment, dwo.Name); return 0; }

                    // ── Runbooks ──────────────────────────────────────────────
                    if (opts is ListRunbooksOptions)      { WallyCommands.HandleListRunbooks(_environment); return 0; }
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

        private static int HandleRunOptions(RunOptions options)
        {
            string runPrompt = options.Prompt ?? string.Empty;
            WallyLoopDefinition? loopDef = !string.IsNullOrWhiteSpace(options.LoopName)
                ? _environment.GetLoop(options.LoopName)
                : null;

            if (InvestigationInteractionStore.IsInvestigationLoop(options.LoopName))
            {
                if (InvestigationInteractionStore.TryLoadWaiting(_environment, out InvestigationInteractionState? waitingState) &&
                    waitingState != null && string.IsNullOrWhiteSpace(runPrompt))
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine(InvestigationInteractionStore.BuildWaitingDisplayText(_environment, waitingState));
                    System.Console.Write("answer> ");
                    runPrompt = System.Console.ReadLine() ?? string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(runPrompt) &&
                    InvestigationInteractionStore.TryRecordResponse(
                        _environment,
                        runPrompt,
                        "Console",
                        out InvestigationInteractionState? interactionState))
                {
                    string runSuffix = InvestigationInteractionStore.TryLoadCurrentRunId(_environment, out string runId)
                        ? $" for investigation {runId}"
                        : string.Empty;
                    System.Console.WriteLine(
                        $"Recorded answer batch {interactionState!.QuestionBatchId}{runSuffix}.");
                    runPrompt = string.Empty;
                }

                if (string.IsNullOrWhiteSpace(runPrompt) &&
                    (loopDef == null ||
                     !WallyLoopExecutionStateStore.TryLoadCurrent(_environment, loopDef, out _)))
                {
                    System.Console.Error.WriteLine(
                        $"Loop '{options.LoopName}' has no persisted state to resume. Provide an initial request to start a new investigation.");
                    return 1;
                }
            }
            else if (string.IsNullOrWhiteSpace(runPrompt) && loopDef?.UsesExecutionState == true)
            {
                if (!WallyLoopExecutionStateStore.TryLoadCurrent(_environment, loopDef, out _))
                {
                    System.Console.Error.WriteLine(
                        $"Loop '{options.LoopName}' has no persisted state to resume. Provide an initial request to start a new run.");
                    return 1;
                }
            }

            WallyCommands.HandleRun(
                _environment,
                runPrompt,
                options.ActorName,
                options.Model,
                options.LoopName,
                options.Wrapper,
                options.NoHistory);

            RenderPendingInteraction(options.LoopName);
            return 0;
        }

        private static void RenderPendingInteraction(string? loopName)
        {
            if (!InvestigationInteractionStore.IsInvestigationLoop(loopName))
                return;

            if (!InvestigationInteractionStore.TryLoadWaiting(_environment, out InvestigationInteractionState? state) ||
                state == null)
            {
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine(InvestigationInteractionStore.BuildWaitingDisplayText(_environment, state));
            System.Console.WriteLine();
        }
    }
}

