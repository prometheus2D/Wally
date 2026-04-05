using System;
using System.Collections.Generic;
using System.IO;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        private static string ExecuteTaskTrackerReadHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            string trackerPathArgument = NormalizeInputPath(RequireTaskExecutionArgument(arguments, "trackerPath", stepDef));
            string statePathArgument = RequireTaskExecutionArgument(arguments, "statePath", stepDef);
            string outcomePathArgument = RequireTaskExecutionArgument(arguments, "outcomePath", stepDef);

            try
            {
                string trackerFullPath = env.ResolveWorkspaceFilePath(trackerPathArgument);
                WallyTaskTrackerStore.Load(trackerFullPath);

                var state = new WallyTaskExecutionSessionState
                {
                    TrackerPath = ToWorkspaceScopedPath(env, trackerFullPath),
                    SelectedTaskNumber = null
                };

                EnsureHandlerWriteAllowed(env, stepDef, statePathArgument, stepDef.HandlerName);
                WallyTaskTrackerStore.SaveSessionState(state, env.ResolveWorkspaceFilePath(statePathArgument));
                return "TRACKER_READY";
            }
            catch (Exception ex)
            {
                WriteOutcomeForFailure(env, stepDef, outcomePathArgument, trackerPathArgument, ex.Message);
                return "EXECUTION_FAILED";
            }
        }

        private static string ExecuteTaskTrackerSelectHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            string statePathArgument = RequireTaskExecutionArgument(arguments, "statePath", stepDef);
            string selectedTaskPathArgument = RequireTaskExecutionArgument(arguments, "selectedTaskPath", stepDef);
            string outcomePathArgument = RequireTaskExecutionArgument(arguments, "outcomePath", stepDef);

            try
            {
                string stateFullPath = env.ResolveWorkspaceFilePath(statePathArgument);
                WallyTaskExecutionSessionState state = WallyTaskTrackerStore.LoadSessionState(stateFullPath);
                if (string.IsNullOrWhiteSpace(state.TrackerPath))
                    throw new InvalidOperationException("Task execution state does not contain a tracker path.");

                string trackerFullPath = env.ResolveWorkspaceFilePath(state.TrackerPath);
                WallyTaskTrackerDocument tracker = WallyTaskTrackerStore.Load(trackerFullPath);
                WallyTaskSelectionResult selection = WallyTaskTrackerStore.SelectNextTask(tracker);

                if (selection.Task != null)
                {
                    state.SelectedTaskNumber = selection.Task.Number;
                    EnsureHandlerWriteAllowed(env, stepDef, statePathArgument, stepDef.HandlerName);
                    WallyTaskTrackerStore.SaveSessionState(state, stateFullPath);

                    EnsureHandlerWriteAllowed(env, stepDef, selectedTaskPathArgument, stepDef.HandlerName);
                    WriteScopedTextDocument(
                        env,
                        stepDef,
                        selectedTaskPathArgument,
                        WallyTaskTrackerStore.BuildSelectedTaskMarkdown(tracker, selection.Task, state.TrackerPath));

                    return selection.Outcome;
                }

                WriteOutcome(
                    env,
                    stepDef,
                    outcomePathArgument,
                    WallyTaskTrackerStore.BuildOutcomeMarkdown(selection.Outcome, state.TrackerPath, null, selection.Reason));
                return selection.Outcome;
            }
            catch (Exception ex)
            {
                WriteOutcomeForFailure(env, stepDef, outcomePathArgument, string.Empty, ex.Message);
                return "EXECUTION_FAILED";
            }
        }

        private static string ExecuteTaskTrackerBeginHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            string statePathArgument = RequireTaskExecutionArgument(arguments, "statePath", stepDef);
            string selectedTaskPathArgument = RequireTaskExecutionArgument(arguments, "selectedTaskPath", stepDef);
            string outcomePathArgument = RequireTaskExecutionArgument(arguments, "outcomePath", stepDef);

            try
            {
                string stateFullPath = env.ResolveWorkspaceFilePath(statePathArgument);
                WallyTaskExecutionSessionState state = WallyTaskTrackerStore.LoadSessionState(stateFullPath);
                int selectedTaskNumber = state.SelectedTaskNumber
                    ?? throw new InvalidOperationException("Task execution state does not contain a selected task.");

                string trackerFullPath = env.ResolveWorkspaceFilePath(state.TrackerPath);
                string scopedTrackerPath = ToWorkspaceScopedPath(env, trackerFullPath);
                WallyTaskTrackerDocument tracker = WallyTaskTrackerStore.Load(trackerFullPath);
                WallyTaskTrackerStore.BeginTask(tracker, selectedTaskNumber);

                EnsureHandlerWriteAllowed(env, stepDef, scopedTrackerPath, stepDef.HandlerName);
                WallyTaskTrackerStore.Save(tracker, trackerFullPath);

                WallyTaskTrackerTask task = WallyTaskTrackerStore.GetRequiredTask(tracker, selectedTaskNumber);
                EnsureHandlerWriteAllowed(env, stepDef, selectedTaskPathArgument, stepDef.HandlerName);
                WriteScopedTextDocument(
                    env,
                    stepDef,
                    selectedTaskPathArgument,
                    WallyTaskTrackerStore.BuildSelectedTaskMarkdown(tracker, task, state.TrackerPath));

                return "READY_TO_EXECUTE";
            }
            catch (Exception ex)
            {
                WriteOutcomeForFailure(env, stepDef, outcomePathArgument, string.Empty, ex.Message);
                return "EXECUTION_FAILED";
            }
        }

        private static string ExecuteTaskTrackerPersistHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            string statePathArgument = RequireTaskExecutionArgument(arguments, "statePath", stepDef);
            string verificationPathArgument = RequireTaskExecutionArgument(arguments, "verificationPath", stepDef);
            string selectedTaskPathArgument = RequireTaskExecutionArgument(arguments, "selectedTaskPath", stepDef);
            string outcomePathArgument = RequireTaskExecutionArgument(arguments, "outcomePath", stepDef);

            try
            {
                string stateFullPath = env.ResolveWorkspaceFilePath(statePathArgument);
                WallyTaskExecutionSessionState state = WallyTaskTrackerStore.LoadSessionState(stateFullPath);
                int selectedTaskNumber = state.SelectedTaskNumber
                    ?? throw new InvalidOperationException("Task execution state does not contain a selected task.");

                string trackerFullPath = env.ResolveWorkspaceFilePath(state.TrackerPath);
                string scopedTrackerPath = ToWorkspaceScopedPath(env, trackerFullPath);
                WallyTaskTrackerDocument tracker = WallyTaskTrackerStore.Load(trackerFullPath);
                WallyTaskTrackerTask task = WallyTaskTrackerStore.GetRequiredTask(tracker, selectedTaskNumber);

                string verificationFullPath = env.ResolveWorkspaceFilePath(verificationPathArgument);
                if (!File.Exists(verificationFullPath))
                    throw new FileNotFoundException("Verification result document was not written.", verificationFullPath);

                WallyTaskVerificationResult verification = WallyTaskTrackerStore.ParseVerificationResult(
                    File.ReadAllText(verificationFullPath));

                string finalOutcome;
                string outcomeReason;
                if (string.Equals(verification.Outcome, "TASK_COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    WallyTaskTrackerStore.CompleteTask(tracker, selectedTaskNumber);
                    finalOutcome = tracker.Tasks.TrueForAll(candidate => string.Equals(candidate.Status, "Complete", StringComparison.OrdinalIgnoreCase))
                        ? "ALL_TASKS_COMPLETE"
                        : "TASK_COMPLETED";
                    outcomeReason = !string.IsNullOrWhiteSpace(verification.Evidence)
                        ? verification.Evidence
                        : "Selected task satisfied its done-condition.";
                }
                else
                {
                    WallyTaskTrackerStore.BlockTask(tracker, selectedTaskNumber, verification.Blocker);
                    finalOutcome = "TASK_BLOCKED";
                    outcomeReason = verification.Blocker;
                }

                EnsureHandlerWriteAllowed(env, stepDef, scopedTrackerPath, stepDef.HandlerName);
                WallyTaskTrackerStore.Save(tracker, trackerFullPath);

                task = WallyTaskTrackerStore.GetRequiredTask(tracker, selectedTaskNumber);
                EnsureHandlerWriteAllowed(env, stepDef, selectedTaskPathArgument, stepDef.HandlerName);
                WriteScopedTextDocument(
                    env,
                    stepDef,
                    selectedTaskPathArgument,
                    WallyTaskTrackerStore.BuildSelectedTaskMarkdown(tracker, task, state.TrackerPath));

                state.SelectedTaskNumber = null;
                EnsureHandlerWriteAllowed(env, stepDef, statePathArgument, stepDef.HandlerName);
                WallyTaskTrackerStore.SaveSessionState(state, stateFullPath);

                WriteOutcome(
                    env,
                    stepDef,
                    outcomePathArgument,
                    WallyTaskTrackerStore.BuildOutcomeMarkdown(finalOutcome, state.TrackerPath, task, outcomeReason));

                return "OUTCOME_READY";
            }
            catch (Exception ex)
            {
                WriteOutcomeForFailure(env, stepDef, outcomePathArgument, string.Empty, ex.Message);
                return "EXECUTION_FAILED";
            }
        }

        private static string ExecuteTaskTrackerStopHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            string outcomePathArgument = RequireTaskExecutionArgument(arguments, "outcomePath", stepDef);
            string outcomeFullPath = env.ResolveWorkspaceFilePath(outcomePathArgument);
            if (!File.Exists(outcomeFullPath))
                throw new FileNotFoundException("Task execution outcome document was not written.", outcomeFullPath);

            return WallyTaskTrackerStore.BuildStopResponse(File.ReadAllText(outcomeFullPath));
        }

        private static void WriteOutcomeForFailure(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            string outcomePathArgument,
            string trackerPath,
            string reason)
        {
            WriteOutcome(
                env,
                stepDef,
                outcomePathArgument,
                WallyTaskTrackerStore.BuildOutcomeMarkdown("EXECUTION_FAILED", trackerPath, null, reason));
        }

        private static void WriteOutcome(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            string outcomePathArgument,
            string markdown)
        {
            WriteScopedTextDocument(env, stepDef, outcomePathArgument, markdown);
        }

        private static void WriteScopedTextDocument(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            string pathArgument,
            string content)
        {
            EnsureHandlerWriteAllowed(env, stepDef, pathArgument, stepDef.HandlerName);
            string fullPath = env.ResolveWorkspaceFilePath(pathArgument);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        private static string RequireTaskExecutionArgument(
            IReadOnlyDictionary<string, string> arguments,
            string name,
            WallyStepDefinition stepDef)
        {
            if (arguments.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value))
                return value;

            throw new InvalidOperationException(
                $"Step '{stepDef.Name}' requires argument '{name}' for handler '{stepDef.HandlerName}'.");
        }

        private static string NormalizeInputPath(string path)
        {
            return path.Trim().Trim('"');
        }

        private static void EnsureHandlerWriteAllowed(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            string path,
            string actionName)
        {
            env.EnsureStepWriteAllowed(stepDef, ToWorkspaceScopedPath(env, path), actionName);
        }

        private static string ToWorkspaceScopedPath(WallyEnvironment env, string path)
        {
            string fullPath = env.ResolveWorkspaceFilePath(path);
            string workspaceRoot = Path.GetFullPath(env.WorkspaceFolder ?? throw new InvalidOperationException("A workspace is required."));

            if (fullPath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');
            }

            return path.Replace('\\', '/');
        }
    }
}