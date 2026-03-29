using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Wally.Core
{
    public enum MermaidDiagramTargetKind
    {
        Workspace,
        Loop,
        LoopStep,
        Runbook
    }

    public sealed class MermaidDiagramDefinition
    {
        public MermaidDiagramTargetKind TargetKind { get; init; }
        public string Title { get; init; } = string.Empty;
        public string FileStem { get; init; } = string.Empty;
        public string RelativeOutputFolder { get; init; } = string.Empty;
        public string MermaidSource { get; init; } = string.Empty;
    }

    public sealed class MermaidDiagramRenderResult
    {
        public MermaidDiagramDefinition Definition { get; init; } = new();
        public string OutputPath { get; init; } = string.Empty;
        public string MermaidFilePath { get; init; } = string.Empty;
        public string OutputFormat { get; init; } = string.Empty;
        public string CommandDisplay { get; init; } = string.Empty;
    }

    public static class MermaidDiagramService
    {
        private const int MaxLabelLength = 72;

        public static MermaidDiagramDefinition BuildWorkspaceDefinition(WallyWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var sb = new StringBuilder();
            sb.AppendLine("flowchart TD");
            AppendNode(sb, "WS", $"Workspace<br/>{TrimLabel(Path.GetFileName(workspace.WorkSource))}");
            AppendNode(sb, "ACTORS", $"Actors<br/>{workspace.Actors.Count}");
            AppendNode(sb, "LOOPS", $"Loops<br/>{workspace.Loops.Count}");
            AppendNode(sb, "RUNBOOKS", $"Runbooks<br/>{workspace.Runbooks.Count}");
            AppendNode(sb, "WRAPPERS", $"Wrappers<br/>{workspace.LlmWrappers.Count}");
            AppendEdge(sb, "WS", "ACTORS");
            AppendEdge(sb, "WS", "LOOPS");
            AppendEdge(sb, "WS", "RUNBOOKS");
            AppendEdge(sb, "WS", "WRAPPERS");

            for (int i = 0; i < workspace.Actors.Count; i++)
            {
                string nodeId = $"A{i + 1}";
                AppendNode(sb, nodeId, TrimLabel(workspace.Actors[i].Name));
                AppendEdge(sb, "ACTORS", nodeId);
            }

            for (int i = 0; i < workspace.Loops.Count; i++)
            {
                var loop = workspace.Loops[i];
                string nodeId = $"L{i + 1}";
                string mode = loop.HasSteps
                    ? $"pipeline ({loop.Steps.Count} step(s))"
                    : loop.IsAgentLoop
                        ? $"agent loop ({loop.MaxIterations} max)"
                        : "single actor";
                AppendNode(sb, nodeId, $"{TrimLabel(loop.Name)}<br/>{mode}");
                AppendEdge(sb, "LOOPS", nodeId);
            }

            for (int i = 0; i < workspace.Runbooks.Count; i++)
            {
                var runbook = workspace.Runbooks[i];
                string nodeId = $"R{i + 1}";
                AppendNode(sb, nodeId, $"{TrimLabel(runbook.Name)}<br/>{runbook.Commands.Count} command(s)");
                AppendEdge(sb, "RUNBOOKS", nodeId);
            }

            for (int i = 0; i < workspace.LlmWrappers.Count; i++)
            {
                var wrapper = workspace.LlmWrappers[i];
                string nodeId = $"W{i + 1}";
                AppendNode(sb, nodeId, TrimLabel(wrapper.Name));
                AppendEdge(sb, "WRAPPERS", nodeId);
            }

            return new MermaidDiagramDefinition
            {
                TargetKind = MermaidDiagramTargetKind.Workspace,
                Title = "Workspace Diagram",
                FileStem = "workspace-overview",
                RelativeOutputFolder = "Workspace",
                MermaidSource = sb.ToString()
            };
        }

        public static MermaidDiagramDefinition BuildLoopDefinition(WallyLoopDefinition loop)
        {
            ArgumentNullException.ThrowIfNull(loop);

            var sb = new StringBuilder();
            sb.AppendLine(loop.IsAgentLoop ? "flowchart TD" : "flowchart LR");

            if (loop.HasSteps)
            {
                AppendNode(sb, "START", "User Prompt");
                for (int i = 0; i < loop.Steps.Count; i++)
                {
                    var step = loop.Steps[i];
                    string nodeId = $"STEP{i + 1}";
                    string label = $"{i + 1}. {ResolveStepName(step, i)}<br/>Actor: {ResolveStepActorName(loop, step)}";
                    AppendNode(sb, nodeId, label);
                    AppendEdge(sb, i == 0 ? "START" : $"STEP{i}", nodeId);
                }

                AppendNode(sb, "END", "Loop Result");
                AppendEdge(sb, $"STEP{loop.Steps.Count}", "END");
            }
            else if (loop.IsAgentLoop)
            {
                AppendNode(sb, "START", "User Prompt");
                AppendNode(sb, "AGENT", $"{TrimLabel(loop.Name)}<br/>Actor: {ResolveLoopActorName(loop)}");
                AppendDecisionNode(sb, "CHECK", $"Stop keyword reached?<br/>Max iterations: {loop.MaxIterations}");
                AppendNode(sb, "FEEDBACK", $"Feedback Mode<br/>{TrimLabel(loop.FeedbackMode)}");
                AppendNode(sb, "END", "Loop Result");

                AppendEdge(sb, "START", "AGENT");
                AppendEdge(sb, "AGENT", "CHECK");
                AppendLabeledEdge(sb, "CHECK", "END", "Yes");
                AppendLabeledEdge(sb, "CHECK", "FEEDBACK", "No");
                AppendEdge(sb, "FEEDBACK", "AGENT");
            }
            else
            {
                AppendNode(sb, "START", "User Prompt");
                AppendNode(sb, "RUN", $"{TrimLabel(loop.Name)}<br/>Actor: {ResolveLoopActorName(loop)}");
                AppendNode(sb, "END", "Loop Result");
                AppendEdge(sb, "START", "RUN");
                AppendEdge(sb, "RUN", "END");
            }

            return new MermaidDiagramDefinition
            {
                TargetKind = MermaidDiagramTargetKind.Loop,
                Title = $"Loop Diagram - {loop.Name}",
                FileStem = SanitizeFileStem(loop.Name),
                RelativeOutputFolder = "Loops",
                MermaidSource = sb.ToString()
            };
        }

        public static MermaidDiagramDefinition BuildLoopStepDefinition(WallyLoopDefinition loop, int stepIndex)
        {
            ArgumentNullException.ThrowIfNull(loop);
            if (!loop.HasSteps)
                throw new InvalidOperationException($"Loop '{loop.Name}' does not define pipeline steps.");
            if (stepIndex < 0 || stepIndex >= loop.Steps.Count)
                throw new ArgumentOutOfRangeException(nameof(stepIndex));

            var step = loop.Steps[stepIndex];
            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");

            AppendNode(sb, "REQ", "User Prompt");
            if (stepIndex > 0)
                AppendNode(sb, "PREV", $"Previous Step<br/>{ResolveStepName(loop.Steps[stepIndex - 1], stepIndex - 1)}");

            string promptSummary = string.IsNullOrWhiteSpace(step.PromptTemplate)
                ? "Implicit prompt wiring"
                : $"Prompt Template<br/>{TrimLabel(step.PromptTemplate)}";
            AppendNode(sb, "PROMPT", promptSummary);
            AppendNode(sb, "STEP", $"{stepIndex + 1}. {ResolveStepName(step, stepIndex)}<br/>Actor: {ResolveStepActorName(loop, step)}");
            AppendNode(sb, "OUT", "Step Output");

            AppendEdge(sb, "REQ", "PROMPT");
            if (stepIndex > 0)
                AppendEdge(sb, "PREV", "PROMPT");
            AppendEdge(sb, "PROMPT", "STEP");
            AppendEdge(sb, "STEP", "OUT");

            if (stepIndex < loop.Steps.Count - 1)
            {
                AppendNode(sb, "NEXT", $"Next Step<br/>{ResolveStepName(loop.Steps[stepIndex + 1], stepIndex + 1)}");
                AppendEdge(sb, "OUT", "NEXT");
            }
            else
            {
                AppendNode(sb, "END", "Loop Result");
                AppendEdge(sb, "OUT", "END");
            }

            return new MermaidDiagramDefinition
            {
                TargetKind = MermaidDiagramTargetKind.LoopStep,
                Title = $"Step Diagram - {loop.Name} / {ResolveStepName(step, stepIndex)}",
                FileStem = $"{SanitizeFileStem(loop.Name)}-step-{stepIndex + 1:D2}-{SanitizeFileStem(ResolveStepName(step, stepIndex))}",
                RelativeOutputFolder = Path.Combine("Loops", SanitizeFileStem(loop.Name)),
                MermaidSource = sb.ToString()
            };
        }

        public static MermaidDiagramDefinition BuildRunbookDefinition(WallyRunbook runbook)
        {
            ArgumentNullException.ThrowIfNull(runbook);

            var sb = new StringBuilder();
            sb.AppendLine("flowchart TD");
            AppendNode(sb, "START", $"Runbook<br/>{TrimLabel(runbook.Name)}");

            if (runbook.Commands.Count == 0)
            {
                AppendNode(sb, "END", "No Commands");
                AppendEdge(sb, "START", "END");
            }
            else
            {
                for (int i = 0; i < runbook.Commands.Count; i++)
                {
                    string nodeId = $"CMD{i + 1}";
                    AppendNode(sb, nodeId, $"{i + 1}. {TrimLabel(runbook.Commands[i])}");
                    AppendEdge(sb, i == 0 ? "START" : $"CMD{i}", nodeId);
                }

                AppendNode(sb, "END", "Complete");
                AppendEdge(sb, $"CMD{runbook.Commands.Count}", "END");
            }

            return new MermaidDiagramDefinition
            {
                TargetKind = MermaidDiagramTargetKind.Runbook,
                Title = $"Runbook Diagram - {runbook.Name}",
                FileStem = SanitizeFileStem(runbook.Name),
                RelativeOutputFolder = "Runbooks",
                MermaidSource = sb.ToString()
            };
        }

        public static MermaidDiagramRenderResult Render(
            WallyEnvironment env,
            MermaidDiagramDefinition definition,
            string outputFormat = "png",
            string? outputPath = null)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(definition);

            if (!env.HasWorkspace)
                throw new InvalidOperationException("A workspace must be loaded before diagrams can be generated.");

            string normalizedFormat = NormalizeFormat(outputFormat);
            string resolvedOutputPath = string.IsNullOrWhiteSpace(outputPath)
                ? ResolveDefaultOutputPath(env.Workspace!, definition, normalizedFormat)
                : Path.GetFullPath(outputPath);
            string mermaidPath = Path.ChangeExtension(resolvedOutputPath, ".mmd");

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
            File.WriteAllText(mermaidPath, definition.MermaidSource, Encoding.UTF8);

            string configuredCommand = string.IsNullOrWhiteSpace(env.Workspace!.Config.MermaidCliCommand)
                ? "npx"
                : env.Workspace.Config.MermaidCliCommand.Trim();
            string argumentTemplate = string.IsNullOrWhiteSpace(env.Workspace.Config.MermaidCliArgumentsTemplate)
                ? "-y @mermaid-js/mermaid-cli -i {input} -o {output} -b transparent"
                : env.Workspace.Config.MermaidCliArgumentsTemplate;
            string arguments = argumentTemplate
                .Replace("{input}", QuoteArgument(mermaidPath), StringComparison.Ordinal)
                .Replace("{output}", QuoteArgument(resolvedOutputPath), StringComparison.Ordinal)
                .Replace("{format}", normalizedFormat, StringComparison.Ordinal);

            MermaidCliExecutionResult execution = ExecuteMermaidCli(
                configuredCommand,
                arguments,
                env.WorkSource ?? env.WorkspaceFolder ?? Environment.CurrentDirectory);

            if (execution.ExitCode != 0 || !File.Exists(resolvedOutputPath))
            {
                throw new InvalidOperationException(
                    $"Mermaid CLI failed with exit code {execution.ExitCode}.\n" +
                    $"Command: {execution.CommandDisplay}\n" +
                    (!string.IsNullOrWhiteSpace(execution.StandardError)
                        ? execution.StandardError.Trim()
                        : execution.StandardOutput.Trim()));
            }

            env.Logger.LogInfo($"Generated {definition.TargetKind} diagram at {resolvedOutputPath}");

            return new MermaidDiagramRenderResult
            {
                Definition = definition,
                OutputPath = resolvedOutputPath,
                MermaidFilePath = mermaidPath,
                OutputFormat = normalizedFormat,
                CommandDisplay = execution.CommandDisplay
            };
        }

        public static string ResolveDefaultOutputPath(
            WallyWorkspace workspace,
            MermaidDiagramDefinition definition,
            string outputFormat)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(definition);

            string normalizedFormat = NormalizeFormat(outputFormat);
            string folder = Path.Combine(
                workspace.WorkspaceFolder,
                workspace.Config.DocsFolderName,
                "Diagrams",
                definition.RelativeOutputFolder);
            return Path.Combine(folder, $"{definition.FileStem}.{normalizedFormat}");
        }

        private static string NormalizeFormat(string outputFormat)
        {
            string normalized = (outputFormat ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "png" or "svg" or "pdf" => normalized,
                _ => throw new InvalidOperationException("Diagram format must be png, svg, or pdf.")
            };
        }

        private static MermaidCliExecutionResult ExecuteMermaidCli(string configuredCommand, string arguments, string workingDirectory)
        {
            List<string> candidates = BuildCommandCandidates(configuredCommand);
            List<string> failures = new();

            foreach (string candidate in candidates)
            {
                MermaidCliProcessInvocation invocation = CreateProcessInvocation(candidate, arguments, workingDirectory);

                try
                {
                    using var process = Process.Start(invocation.ProcessStartInfo);
                    if (process == null)
                    {
                        failures.Add($"{invocation.CommandDisplay}: process did not start");
                        continue;
                    }

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return new MermaidCliExecutionResult(invocation.CommandDisplay, process.ExitCode, stdout, stderr);

                    failures.Add(
                        $"{invocation.CommandDisplay}: exit {process.ExitCode} - " +
                        (!string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim()));
                }
                catch (Exception ex)
                {
                    failures.Add($"{invocation.CommandDisplay}: {ex.Message}");
                }
            }

            throw new InvalidOperationException(
                "Unable to start Mermaid CLI. Tried: " + string.Join(", ", candidates) +
                ". Configure WallyConfig.MermaidCliCommand if Mermaid is installed somewhere else.\n" +
                string.Join(Environment.NewLine, failures));
        }

        private static MermaidCliProcessInvocation CreateProcessInvocation(string candidate, string arguments, string workingDirectory)
        {
            string resolvedCandidate = ResolveExecutablePath(candidate) ?? candidate;
            bool isCmdScript = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && resolvedCandidate.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);

            if (isCmdScript)
            {
                string cmdArguments = $"/c call \"{resolvedCandidate}\" {arguments}";
                return new MermaidCliProcessInvocation(
                    new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = cmdArguments,
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    $"cmd.exe {cmdArguments}");
            }

            return new MermaidCliProcessInvocation(
                new ProcessStartInfo
                {
                    FileName = resolvedCandidate,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                $"{resolvedCandidate} {arguments}");
        }

        private static string? ResolveExecutablePath(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return null;

            if (Path.IsPathRooted(command))
                return File.Exists(command) ? command : null;

            if (File.Exists(command))
                return Path.GetFullPath(command);

            string[] searchDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            bool hasExtension = Path.HasExtension(command);
            string[] extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !hasExtension
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : new[] { string.Empty };

            foreach (string directory in searchDirectories)
            {
                foreach (string extension in extensions)
                {
                    string candidatePath = Path.Combine(directory, hasExtension ? command : command + extension);
                    if (File.Exists(candidatePath))
                        return candidatePath;
                }
            }

            return null;
        }

        private static List<string> BuildCommandCandidates(string configuredCommand)
        {
            var candidates = new List<string>();

            void Add(string command)
            {
                if (!string.IsNullOrWhiteSpace(command) &&
                    !candidates.Contains(command, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(command);
                }
            }

            Add(configuredCommand);

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows && !Path.HasExtension(configuredCommand))
                Add(configuredCommand + ".cmd");

            if (string.Equals(configuredCommand, "npx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(configuredCommand, "npx.cmd", StringComparison.OrdinalIgnoreCase))
            {
                Add("mmdc");
                if (isWindows)
                    Add("mmdc.cmd");
            }

            return candidates;
        }

        private static void AppendNode(StringBuilder sb, string nodeId, string label)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeLabel(label)}\"]");
        }

        private static void AppendDecisionNode(StringBuilder sb, string nodeId, string label)
        {
            sb.AppendLine($"    {nodeId}{{\"{EscapeLabel(label)}\"}}");
        }

        private static void AppendEdge(StringBuilder sb, string fromNodeId, string toNodeId)
        {
            sb.AppendLine($"    {fromNodeId} --> {toNodeId}");
        }

        private static void AppendLabeledEdge(StringBuilder sb, string fromNodeId, string toNodeId, string label)
        {
            sb.AppendLine($"    {fromNodeId} -- \"{EscapeLabel(label)}\" --> {toNodeId}");
        }

        private static string EscapeLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            return label
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal)
                .Replace("\r", "<br/>", StringComparison.Ordinal)
                .Replace("\"", "'", StringComparison.Ordinal);
        }

        private static string QuoteArgument(string value)
        {
            return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }

        private static string ResolveLoopActorName(WallyLoopDefinition loop)
        {
            return string.IsNullOrWhiteSpace(loop.ActorName) ? "direct / caller-supplied" : TrimLabel(loop.ActorName);
        }

        private static string ResolveStepActorName(WallyLoopDefinition loop, WallyStepDefinition step)
        {
            if (!string.IsNullOrWhiteSpace(step.ActorName))
                return TrimLabel(step.ActorName);
            if (!string.IsNullOrWhiteSpace(loop.ActorName))
                return $"{TrimLabel(loop.ActorName)} (fallback)";
            return "direct";
        }

        private static string ResolveStepName(WallyStepDefinition step, int index)
        {
            return string.IsNullOrWhiteSpace(step.Name) ? $"step-{index + 1}" : step.Name.Trim();
        }

        private static string TrimLabel(string? value)
        {
            string text = (value ?? string.Empty).Trim();
            if (text.Length == 0)
                return "(none)";

            string singleLine = text.Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
            return singleLine.Length <= MaxLabelLength
                ? singleLine
                : singleLine[..(MaxLabelLength - 3)] + "...";
        }

        private static string SanitizeFileStem(string value)
        {
            string sanitized = new string(value
                .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch)
                .ToArray())
                .Trim();

            if (string.IsNullOrWhiteSpace(sanitized))
                return "diagram";

            return sanitized.Replace(' ', '-');
        }

        private sealed record MermaidCliExecutionResult(
            string CommandDisplay,
            int ExitCode,
            string StandardOutput,
            string StandardError);

        private sealed record MermaidCliProcessInvocation(ProcessStartInfo ProcessStartInfo, string CommandDisplay);
    }
}