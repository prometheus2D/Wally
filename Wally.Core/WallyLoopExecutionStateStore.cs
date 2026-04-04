using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    public sealed class WallyLoopExecutionStateOptions
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("statePath")]
        public string StatePath { get; set; } = string.Empty;

        [JsonPropertyName("resetPathsOnNewRun")]
        public List<string> ResetPathsOnNewRun { get; set; } = new();

        [JsonPropertyName("preservePathsOnReset")]
        public List<string> PreservePathsOnReset { get; set; } = new();
    }

    public sealed class WallyLoopExecutionState
    {
        public string LoopName { get; set; } = string.Empty;

        public string RunId { get; set; } = string.Empty;

        public string Mode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CurrentStepName { get; set; } = string.Empty;

        public string NextStepName { get; set; } = string.Empty;

        public int IterationCount { get; set; }

        public string StopReason { get; set; } = string.Empty;

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset LastUpdatedUtc { get; set; }

        public string OriginalRequest { get; set; } = string.Empty;

        public string CurrentPrompt { get; set; } = string.Empty;

        public string PreviousStepResult { get; set; } = string.Empty;

        public bool CanResume =>
            !string.IsNullOrWhiteSpace(LoopName) &&
            !string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Status, "Failed", StringComparison.OrdinalIgnoreCase) &&
            (!string.IsNullOrWhiteSpace(CurrentPrompt) ||
             !string.IsNullOrWhiteSpace(OriginalRequest) ||
             !string.IsNullOrWhiteSpace(NextStepName));
    }

    public static class WallyLoopExecutionStateStore
    {
        public static bool IsEnabled(WallyLoopDefinition? loopDef)
        {
            return loopDef?.UsesExecutionState == true;
        }

        public static bool TryLoadCurrent(
            WallyEnvironment env,
            WallyLoopDefinition loopDef,
            out WallyLoopExecutionState? state)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(loopDef);

            state = null;
            if (!env.HasWorkspace || !IsEnabled(loopDef))
                return false;

            string filePath = env.ResolveWorkspaceFilePath(ResolveStatePath(loopDef));
            if (!File.Exists(filePath))
                return false;

            state = Parse(File.ReadAllLines(filePath));
            if (state == null)
                return false;

            if (string.IsNullOrWhiteSpace(state.LoopName))
                state.LoopName = loopDef.Name;

            return true;
        }

        public static WallyLoopExecutionState PrepareForRun(
            WallyEnvironment env,
            WallyLoopDefinition loopDef,
            string? prompt,
            string currentPrompt,
            string mode)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(loopDef);

            bool hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            if (hasPrompt)
            {
                ResetForNewRun(env, loopDef);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                var state = new WallyLoopExecutionState
                {
                    LoopName = loopDef.Name,
                    RunId = BuildRunId(loopDef.Name, now),
                    Mode = mode,
                    Status = "Running",
                    CurrentStepName = string.Empty,
                    NextStepName = ResolveInitialNextStep(loopDef),
                    IterationCount = 0,
                    StopReason = string.Empty,
                    StartedAtUtc = now,
                    LastUpdatedUtc = now,
                    OriginalRequest = (prompt ?? string.Empty).Trim(),
                    CurrentPrompt = (currentPrompt ?? string.Empty).Trim(),
                    PreviousStepResult = string.Empty
                };

                Save(env, loopDef, state);
                return state;
            }

            if (TryLoadCurrent(env, loopDef, out WallyLoopExecutionState? existing) && existing?.CanResume == true)
            {
                if (string.IsNullOrWhiteSpace(existing.Mode))
                    existing.Mode = mode;

                return existing;
            }

            throw new InvalidOperationException(
                $"Loop '{loopDef.Name}' has no persisted execution state to resume. Provide an initial request to start a new run.");
        }

        public static void Save(WallyEnvironment env, WallyLoopDefinition loopDef, WallyLoopExecutionState state)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(loopDef);
            ArgumentNullException.ThrowIfNull(state);

            string filePath = env.ResolveWorkspaceFilePath(ResolveStatePath(loopDef));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, BuildMarkdown(state));
        }

        public static void UpdateAndSave(
            WallyEnvironment env,
            WallyLoopDefinition loopDef,
            WallyLoopExecutionState? state,
            string currentStepName,
            string nextStepName,
            int iterationCount,
            string status,
            string? stopReason,
            string currentPrompt,
            string? previousStepResult,
            string? mode = null)
        {
            if (state == null)
                return;

            state.LoopName = loopDef.Name;
            state.CurrentStepName = currentStepName ?? string.Empty;
            state.NextStepName = nextStepName ?? string.Empty;
            state.IterationCount = iterationCount;
            state.Status = status;
            state.StopReason = stopReason ?? string.Empty;
            state.CurrentPrompt = currentPrompt ?? string.Empty;
            state.PreviousStepResult = previousStepResult ?? string.Empty;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(mode))
                state.Mode = mode;

            Save(env, loopDef, state);
        }

        public static string ResolveStatePath(WallyLoopDefinition loopDef)
        {
            ArgumentNullException.ThrowIfNull(loopDef);

            if (!string.IsNullOrWhiteSpace(loopDef.ExecutionState?.StatePath))
                return loopDef.ExecutionState.StatePath.Trim();

            return $"Logs/LoopState/{loopDef.Name}.md";
        }

        public static string ResolveInitialNextStep(WallyLoopDefinition loopDef)
        {
            ArgumentNullException.ThrowIfNull(loopDef);

            if (loopDef.UsesNamedStepRouting)
                return loopDef.StartStepName;

            if (loopDef.HasSteps && loopDef.Steps.Count > 0)
                return GetStableStepName(loopDef.Steps[0], 0);

            return string.Empty;
        }

        public static string GetStableStepName(WallyStepDefinition stepDef, int stepIndex)
        {
            ArgumentNullException.ThrowIfNull(stepDef);
            return string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{stepIndex + 1}" : stepDef.Name;
        }

        private static void ResetForNewRun(WallyEnvironment env, WallyLoopDefinition loopDef)
        {
            foreach (string relativePath in loopDef.ExecutionState.ResetPathsOnNewRun)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    continue;

                ResetPath(env, relativePath.Trim(), loopDef.ExecutionState.PreservePathsOnReset);
            }
        }

        private static void ResetPath(WallyEnvironment env, string relativePath, IReadOnlyCollection<string> preservedPaths)
        {
            string resolvedPath = env.ResolveWorkspaceFilePath(relativePath);
            if (File.Exists(resolvedPath))
            {
                if (!IsPreservedPath(relativePath, preservedPaths))
                    File.Delete(resolvedPath);
                return;
            }

            if (!Directory.Exists(resolvedPath))
                return;

            string normalizedRoot = NormalizeRelativePath(relativePath);
            foreach (string filePath in Directory.GetFiles(resolvedPath, "*", SearchOption.AllDirectories))
            {
                string relativeFilePath = NormalizeRelativePath(Path.GetRelativePath(env.WorkspaceFolder!, filePath));
                if (IsPreservedPath(relativeFilePath, preservedPaths))
                    continue;

                File.Delete(filePath);
            }

            foreach (string directoryPath in Directory.GetDirectories(resolvedPath, "*", SearchOption.AllDirectories)
                         .OrderByDescending(path => path.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    Directory.Delete(directoryPath);
            }

            if (!string.IsNullOrWhiteSpace(normalizedRoot) &&
                Directory.Exists(resolvedPath) &&
                !Directory.EnumerateFileSystemEntries(resolvedPath).Any())
            {
                Directory.Delete(resolvedPath);
            }
        }

        private static bool IsPreservedPath(string relativePath, IReadOnlyCollection<string> preservedPaths)
        {
            string normalized = NormalizeRelativePath(relativePath);
            foreach (string preservedPath in preservedPaths)
            {
                if (string.Equals(normalized, NormalizeRelativePath(preservedPath), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeRelativePath(string path)
        {
            return path.Replace('\\', '/').Trim();
        }

        private static string BuildRunId(string loopName, DateTimeOffset timestamp)
        {
            return $"{loopName}-{timestamp:yyyyMMddHHmmss}";
        }

        private static WallyLoopExecutionState Parse(string[] lines)
        {
            var state = new WallyLoopExecutionState();
            bool inMetadata = false;
            string activeSection = string.Empty;
            var originalRequest = new StringBuilder();
            var currentPrompt = new StringBuilder();
            var previousStepResult = new StringBuilder();

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                if (string.Equals(trimmed, "## Metadata", StringComparison.OrdinalIgnoreCase))
                {
                    inMetadata = true;
                    activeSection = string.Empty;
                    continue;
                }

                if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                {
                    inMetadata = false;
                    activeSection = trimmed;
                    continue;
                }

                if (inMetadata && TryParseBullet(trimmed, out string key, out string value))
                {
                    ApplyMetadata(state, key, value);
                    continue;
                }

                AppendSectionLine(activeSection, line, originalRequest, currentPrompt, previousStepResult);
            }

            state.OriginalRequest = originalRequest.ToString().Trim();
            state.CurrentPrompt = currentPrompt.ToString().Trim();
            state.PreviousStepResult = previousStepResult.ToString().Trim();
            return state;
        }

        private static void AppendSectionLine(
            string activeSection,
            string line,
            StringBuilder originalRequest,
            StringBuilder currentPrompt,
            StringBuilder previousStepResult)
        {
            StringBuilder? target = activeSection switch
            {
                "## Original Request" => originalRequest,
                "## Current Prompt" => currentPrompt,
                "## Previous Step Result" => previousStepResult,
                _ => null
            };

            if (target == null)
                return;

            if (target.Length > 0)
                target.AppendLine();
            target.Append(line);
        }

        private static void ApplyMetadata(WallyLoopExecutionState state, string key, string value)
        {
            switch (key)
            {
                case "LoopName":
                    state.LoopName = value;
                    break;
                case "RunId":
                case "InvestigationId":
                    state.RunId = value;
                    break;
                case "Mode":
                    state.Mode = value;
                    break;
                case "Status":
                    state.Status = value;
                    break;
                case "CurrentStepName":
                    state.CurrentStepName = value;
                    break;
                case "NextStepName":
                    state.NextStepName = value;
                    break;
                case "IterationCount":
                    state.IterationCount = int.TryParse(value, out int iterationCount)
                        ? iterationCount
                        : 0;
                    break;
                case "StopReason":
                    state.StopReason = value;
                    break;
                case "StartedAtUtc":
                    state.StartedAtUtc = DateTimeOffset.TryParse(value, out DateTimeOffset startedAtUtc)
                        ? startedAtUtc
                        : default;
                    break;
                case "LastUpdatedUtc":
                    state.LastUpdatedUtc = DateTimeOffset.TryParse(value, out DateTimeOffset lastUpdatedUtc)
                        ? lastUpdatedUtc
                        : default;
                    break;
            }
        }

        private static bool TryParseBullet(string trimmedLine, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            if (!trimmedLine.StartsWith("- ", StringComparison.Ordinal))
                return false;

            int separatorIndex = trimmedLine.IndexOf(':', 2);
            if (separatorIndex < 0)
                return false;

            key = trimmedLine.Substring(2, separatorIndex - 2).Trim();
            value = trimmedLine[(separatorIndex + 1)..].Trim();
            return !string.IsNullOrWhiteSpace(key);
        }

        private static string BuildMarkdown(WallyLoopExecutionState state)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Loop Execution State");
            builder.AppendLine();
            builder.AppendLine("## Metadata");
            builder.AppendLine($"- LoopName: {state.LoopName}");
            builder.AppendLine($"- RunId: {state.RunId}");
            builder.AppendLine($"- Mode: {state.Mode}");
            builder.AppendLine($"- Status: {state.Status}");
            builder.AppendLine($"- CurrentStepName: {state.CurrentStepName}");
            builder.AppendLine($"- NextStepName: {state.NextStepName}");
            builder.AppendLine($"- IterationCount: {state.IterationCount}");
            builder.AppendLine($"- StopReason: {state.StopReason}");
            builder.AppendLine($"- StartedAtUtc: {FormatTimestamp(state.StartedAtUtc)}");
            builder.AppendLine($"- LastUpdatedUtc: {FormatTimestamp(state.LastUpdatedUtc)}");
            builder.AppendLine();
            builder.AppendLine("## Original Request");
            if (!string.IsNullOrWhiteSpace(state.OriginalRequest))
                builder.AppendLine(state.OriginalRequest.TrimEnd());
            builder.AppendLine();
            builder.AppendLine("## Current Prompt");
            if (!string.IsNullOrWhiteSpace(state.CurrentPrompt))
                builder.AppendLine(state.CurrentPrompt.TrimEnd());
            builder.AppendLine();
            builder.AppendLine("## Previous Step Result");
            if (!string.IsNullOrWhiteSpace(state.PreviousStepResult))
                builder.AppendLine(state.PreviousStepResult.TrimEnd());

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static string FormatTimestamp(DateTimeOffset value)
        {
            return value == default ? string.Empty : value.ToString("O");
        }
    }
}