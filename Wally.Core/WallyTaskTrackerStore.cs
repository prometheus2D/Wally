using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wally.Core
{
    internal sealed class WallyTaskExecutionSessionState
    {
        public string TrackerPath { get; set; } = string.Empty;

        public int? SelectedTaskNumber { get; set; }
    }

    internal sealed class WallyTaskTrackerDocument
    {
        public string Title { get; set; } = string.Empty;

        public string SourceProposal { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public string Created { get; set; } = string.Empty;

        public string LastUpdated { get; set; } = string.Empty;

        public string Owner { get; set; } = string.Empty;

        public List<string> HeaderExtras { get; } = new();

        public List<string> SectionOrder { get; } = new();

        public Dictionary<string, string> PreservedSections { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<WallyTaskTrackerTask> Tasks { get; } = new();

        public List<string> PhaseOrder { get; } = new();

        public List<WallyTaskTrackerNote> Notes { get; } = new();
    }

    internal sealed class WallyTaskTrackerTask
    {
        public int Number { get; set; }

        public string PhaseTitle { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Effort { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Owner { get; set; } = string.Empty;

        public List<int> Dependencies { get; } = new();

        public string DoneCondition { get; set; } = string.Empty;
    }

    internal sealed class WallyTaskTrackerNote
    {
        public int TaskNumber { get; set; }

        public string Note { get; set; } = string.Empty;

        public string Raised { get; set; } = string.Empty;

        public string Resolved { get; set; } = string.Empty;

        public bool IsResolved => !string.IsNullOrWhiteSpace(Resolved) && !string.Equals(Resolved, "-", StringComparison.Ordinal);
    }

    internal sealed class WallyTaskSelectionResult
    {
        public string Outcome { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public WallyTaskTrackerTask? Task { get; set; }
    }

    internal sealed class WallyTaskVerificationResult
    {
        public string Outcome { get; set; } = string.Empty;

        public string Blocker { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }

    internal static class WallyTaskTrackerStore
    {
        private static readonly Regex HeaderMetadataRegex = new(@"^\*\*(?<key>[^*]+)\*\*:\s*(?<value>.*)$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static WallyTaskTrackerDocument Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Path cannot be empty.", nameof(filePath));

            string markdown = File.ReadAllText(filePath);
            WallyTaskTrackerDocument document = Parse(markdown);

            if (document.Tasks.Count == 0)
                throw new InvalidOperationException("Task tracker does not contain any task rows.");

            return document;
        }

        public static void Save(WallyTaskTrackerDocument document, string filePath)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Path cannot be empty.", nameof(filePath));

            RefreshTrackerMetadata(document);

            string markdown = BuildMarkdown(document);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, markdown);
        }

        public static WallyTaskExecutionSessionState LoadSessionState(string filePath)
        {
            if (!File.Exists(filePath))
                return new WallyTaskExecutionSessionState();

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WallyTaskExecutionSessionState>(json, JsonOptions)
                ?? new WallyTaskExecutionSessionState();
        }

        public static void SaveSessionState(WallyTaskExecutionSessionState state, string filePath)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, JsonSerializer.Serialize(state, JsonOptions));
        }

        public static WallyTaskSelectionResult SelectNextTask(WallyTaskTrackerDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            WallyTaskTrackerTask? inProgress = document.Tasks
                .Where(task => IsStatus(task.Status, "In Progress"))
                .OrderBy(task => task.Number)
                .FirstOrDefault();
            if (inProgress != null)
            {
                return new WallyTaskSelectionResult
                {
                    Outcome = "TASK_SELECTED",
                    Task = inProgress,
                    Reason = $"Continuing in-progress task {inProgress.Number}."
                };
            }

            foreach (WallyTaskTrackerTask task in document.Tasks.OrderBy(task => task.Number))
            {
                if (IsStatus(task.Status, "Complete"))
                    continue;

                if (!AreDependenciesComplete(document, task))
                    continue;

                if (IsStatus(task.Status, "Not Started"))
                {
                    return new WallyTaskSelectionResult
                    {
                        Outcome = "TASK_SELECTED",
                        Task = task,
                        Reason = $"Selected first dependency-eligible task {task.Number}."
                    };
                }

                if (IsStatus(task.Status, "Blocked") && !HasActiveBlocker(document, task.Number))
                {
                    return new WallyTaskSelectionResult
                    {
                        Outcome = "TASK_SELECTED",
                        Task = task,
                        Reason = $"Resuming previously blocked task {task.Number} after blocker clearance."
                    };
                }
            }

            if (document.Tasks.All(task => IsStatus(task.Status, "Complete")))
            {
                return new WallyTaskSelectionResult
                {
                    Outcome = "ALL_TASKS_COMPLETE",
                    Reason = "Every task is marked Complete."
                };
            }

            return new WallyTaskSelectionResult
            {
                Outcome = "TASKS_BLOCKED",
                Reason = "No unfinished task is currently eligible from tracker state alone."
            };
        }

        public static WallyTaskTrackerTask GetRequiredTask(WallyTaskTrackerDocument document, int taskNumber)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            WallyTaskTrackerTask? task = document.Tasks.FirstOrDefault(candidate => candidate.Number == taskNumber);
            if (task == null)
                throw new InvalidOperationException($"Task tracker does not contain task {taskNumber}.");

            return task;
        }

        public static void BeginTask(WallyTaskTrackerDocument document, int taskNumber)
        {
            WallyTaskTrackerTask task = GetRequiredTask(document, taskNumber);
            if (!AreDependenciesComplete(document, task))
                throw new InvalidOperationException($"Task {taskNumber} cannot start until all dependencies are Complete.");

            if (IsStatus(task.Status, "Complete"))
                throw new InvalidOperationException($"Task {taskNumber} is already Complete.");

            if (IsStatus(task.Status, "Blocked") && HasActiveBlocker(document, task.Number))
                throw new InvalidOperationException($"Task {taskNumber} still has an unresolved blocker in the tracker.");

            task.Status = "In Progress";
        }

        public static void CompleteTask(WallyTaskTrackerDocument document, int taskNumber)
        {
            WallyTaskTrackerTask task = GetRequiredTask(document, taskNumber);
            task.Status = "Complete";
            ResolveOpenNotes(document, taskNumber, GetTodayStamp());
        }

        public static void BlockTask(WallyTaskTrackerDocument document, int taskNumber, string blockerText)
        {
            if (string.IsNullOrWhiteSpace(blockerText) || string.Equals(blockerText.Trim(), "-", StringComparison.Ordinal))
                throw new InvalidOperationException("Blocked task verification must include a blocker explanation.");

            WallyTaskTrackerTask task = GetRequiredTask(document, taskNumber);
            task.Status = "Blocked";

            string normalizedText = blockerText.Trim();
            WallyTaskTrackerNote? existingOpenNote = document.Notes.FirstOrDefault(note =>
                note.TaskNumber == taskNumber
                && !note.IsResolved
                && string.Equals(note.Note.Trim(), normalizedText, StringComparison.OrdinalIgnoreCase));

            if (existingOpenNote == null)
            {
                document.Notes.Add(new WallyTaskTrackerNote
                {
                    TaskNumber = taskNumber,
                    Note = normalizedText,
                    Raised = GetTodayStamp(),
                    Resolved = "-"
                });
            }
        }

        public static string BuildSelectedTaskMarkdown(WallyTaskTrackerDocument document, WallyTaskTrackerTask task, string trackerPath)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var builder = new StringBuilder();
            builder.AppendLine("# Selected Task");
            builder.AppendLine();
            builder.AppendLine($"- TrackerPath: {trackerPath}");
            builder.AppendLine($"- TaskNumber: {task.Number}");
            builder.AppendLine($"- Phase: {FormatValueOrDash(task.PhaseTitle)}");
            builder.AppendLine($"- Status: {task.Status}");
            builder.AppendLine($"- Priority: {task.Priority}");
            builder.AppendLine($"- Effort: {task.Effort}");
            builder.AppendLine($"- Owner: {task.Owner}");
            builder.AppendLine($"- Dependencies: {BuildDependencySummary(document, task)}");
            builder.AppendLine($"- DoneCondition: {task.DoneCondition}");
            builder.AppendLine();
            builder.AppendLine("## Title");
            builder.AppendLine(task.Title);
            builder.AppendLine();
            builder.AppendLine("## Description");
            builder.AppendLine(task.Description);
            builder.AppendLine();
            builder.AppendLine("## Active Blockers");

            List<WallyTaskTrackerNote> activeNotes = document.Notes
                .Where(note => note.TaskNumber == task.Number && !note.IsResolved)
                .ToList();
            if (activeNotes.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (WallyTaskTrackerNote note in activeNotes)
                    builder.AppendLine($"- {note.Note} (raised {note.Raised})");
            }

            builder.AppendLine();
            builder.AppendLine("## Progress Snapshot");

            int completeCount = document.Tasks.Count(candidate => IsStatus(candidate.Status, "Complete"));
            int activeCount = document.Tasks.Count(candidate => IsStatus(candidate.Status, "In Progress"));
            int blockedCount = document.Tasks.Count(candidate => IsStatus(candidate.Status, "Blocked"));
            int remainingCount = document.Tasks.Count(candidate => IsStatus(candidate.Status, "Not Started"));
            builder.AppendLine($"- Complete: {completeCount}");
            builder.AppendLine($"- Active: {activeCount}");
            builder.AppendLine($"- Blocked: {blockedCount}");
            builder.AppendLine($"- Remaining: {remainingCount}");

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        public static WallyTaskVerificationResult ParseVerificationResult(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                throw new InvalidOperationException("Verification result is empty.");

            var result = new WallyTaskVerificationResult();
            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            string activeHeading = string.Empty;
            var notesBuilder = new StringBuilder();

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                {
                    activeHeading = trimmed;
                    continue;
                }

                if (TryParseBullet(trimmed, out string key, out string value))
                {
                    switch (key)
                    {
                        case "Outcome":
                            result.Outcome = value;
                            break;
                        case "Blocker":
                            result.Blocker = value;
                            break;
                        case "Evidence":
                            result.Evidence = value;
                            break;
                    }

                    continue;
                }

                if (string.Equals(activeHeading, "## Verification Notes", StringComparison.OrdinalIgnoreCase))
                {
                    if (notesBuilder.Length > 0)
                        notesBuilder.AppendLine();
                    notesBuilder.Append(line);
                }
            }

            result.Outcome = NormalizeOutcome(result.Outcome);
            result.Blocker = NormalizeDashValue(result.Blocker);
            result.Evidence = NormalizeDashValue(result.Evidence);
            result.Notes = notesBuilder.ToString().Trim();

            if (!string.Equals(result.Outcome, "TASK_COMPLETED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(result.Outcome, "TASK_BLOCKED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Verification result must declare Outcome as TASK_COMPLETED or TASK_BLOCKED.");
            }

            if (string.Equals(result.Outcome, "TASK_BLOCKED", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(result.Blocker))
            {
                throw new InvalidOperationException(
                    "Verification result must include a blocker explanation when Outcome is TASK_BLOCKED.");
            }

            return result;
        }

        public static string BuildOutcomeMarkdown(string outcome, string trackerPath, WallyTaskTrackerTask? task, string reason)
        {
            string normalizedOutcome = NormalizeOutcome(outcome);
            var builder = new StringBuilder();
            builder.AppendLine("# Task Execution Outcome");
            builder.AppendLine();
            builder.AppendLine($"- Outcome: {normalizedOutcome}");
            builder.AppendLine($"- TrackerPath: {trackerPath}");
            builder.AppendLine($"- TaskNumber: {(task?.Number.ToString() ?? "-")}");
            builder.AppendLine($"- TaskTitle: {FormatValueOrDash(task?.Title)}");
            builder.AppendLine($"- Reason: {FormatValueOrDash(reason)}");
            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        public static string BuildStopResponse(string outcomeMarkdown)
        {
            if (string.IsNullOrWhiteSpace(outcomeMarkdown))
                throw new InvalidOperationException("Outcome document is empty.");

            string outcome = string.Empty;
            string trackerPath = string.Empty;
            string taskNumber = string.Empty;
            string taskTitle = string.Empty;
            string reason = string.Empty;

            foreach (string rawLine in outcomeMarkdown.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = rawLine.Trim();
                if (!TryParseBullet(trimmed, out string key, out string value))
                    continue;

                switch (key)
                {
                    case "Outcome":
                        outcome = NormalizeOutcome(value);
                        break;
                    case "TrackerPath":
                        trackerPath = value;
                        break;
                    case "TaskNumber":
                        taskNumber = NormalizeDashValue(value);
                        break;
                    case "TaskTitle":
                        taskTitle = NormalizeDashValue(value);
                        break;
                    case "Reason":
                        reason = NormalizeDashValue(value);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(outcome))
                throw new InvalidOperationException("Outcome document does not declare an Outcome value.");

            var builder = new StringBuilder();
            builder.AppendLine(outcome);
            builder.AppendLine();
            builder.AppendLine($"Tracker: {trackerPath}");

            if (!string.IsNullOrWhiteSpace(taskNumber) && !string.Equals(taskNumber, "-", StringComparison.Ordinal))
                builder.AppendLine($"Task: {taskNumber} {taskTitle}".TrimEnd());

            if (!string.IsNullOrWhiteSpace(reason) && !string.Equals(reason, "-", StringComparison.Ordinal))
                builder.AppendLine($"Reason: {reason}");

            return builder.ToString().TrimEnd();
        }

        private static WallyTaskTrackerDocument Parse(string markdown)
        {
            var document = new WallyTaskTrackerDocument();
            string normalized = markdown.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
            int firstSectionIndex = FindFirstSectionIndex(lines);
            if (firstSectionIndex < 0)
                throw new InvalidOperationException("Task tracker is missing required top-level sections.");

            ParseHeader(document, lines, firstSectionIndex);

            int index = firstSectionIndex;
            while (index < lines.Length)
            {
                if (!lines[index].TrimStart().StartsWith("## ", StringComparison.Ordinal))
                {
                    index++;
                    continue;
                }

                string heading = lines[index].Trim()[3..].Trim();
                int sectionStart = index + 1;
                int sectionEnd = sectionStart;
                while (sectionEnd < lines.Length && !lines[sectionEnd].TrimStart().StartsWith("## ", StringComparison.Ordinal))
                    sectionEnd++;

                string content = string.Join("\n", lines[sectionStart..sectionEnd]).TrimEnd();
                document.SectionOrder.Add(heading);

                if (string.Equals(heading, "Task List", StringComparison.OrdinalIgnoreCase))
                {
                    ParseTaskList(document, content);
                }
                else if (string.Equals(heading, "Blockers & Notes", StringComparison.OrdinalIgnoreCase))
                {
                    ParseBlockers(document, content);
                }
                else if (!string.Equals(heading, "Progress Summary", StringComparison.OrdinalIgnoreCase))
                {
                    document.PreservedSections[heading] = content;
                }

                index = sectionEnd;
            }

            if (!document.SectionOrder.Any(heading => string.Equals(heading, "Task List", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Task tracker is missing the Task List section.");

            return document;
        }

        private static void ParseHeader(WallyTaskTrackerDocument document, string[] lines, int firstSectionIndex)
        {
            for (int i = 0; i < firstSectionIndex; i++)
            {
                string line = lines[i].TrimEnd();
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                {
                    document.Title = trimmed;
                    continue;
                }

                Match match = HeaderMetadataRegex.Match(trimmed);
                if (match.Success)
                {
                    string key = match.Groups["key"].Value.Trim();
                    string value = match.Groups["value"].Value.Trim();

                    switch (key)
                    {
                        case "Source Proposal":
                            document.SourceProposal = value;
                            break;
                        case "Status":
                            document.Status = value;
                            break;
                        case "Created":
                            document.Created = value;
                            break;
                        case "Last Updated":
                            document.LastUpdated = value;
                            break;
                        case "Owner":
                            document.Owner = value;
                            break;
                        default:
                            document.HeaderExtras.Add(trimmed);
                            break;
                    }

                    continue;
                }

                document.HeaderExtras.Add(trimmed);
            }
        }

        private static void ParseTaskList(WallyTaskTrackerDocument document, string content)
        {
            string[] lines = content.Replace("\r\n", "\n").Split('\n');
            string currentPhase = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.StartsWith("#### ", StringComparison.Ordinal))
                {
                    currentPhase = trimmed[5..].Trim();
                    AddPhaseIfMissing(document, currentPhase);
                    continue;
                }

                if (!trimmed.StartsWith("| # | Task |", StringComparison.OrdinalIgnoreCase))
                    continue;

                i += 2;
                for (; i < lines.Length; i++)
                {
                    string row = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(row))
                        continue;

                    if (!row.StartsWith("|", StringComparison.Ordinal) || IsSeparatorRow(row))
                    {
                        i--;
                        break;
                    }

                    List<string> cells = ParseTableCells(row);
                    if (cells.Count < 9)
                    {
                        i--;
                        break;
                    }

                    var task = new WallyTaskTrackerTask
                    {
                        Number = ParseRequiredInt(cells[0], "task number"),
                        PhaseTitle = currentPhase,
                        Title = cells[1],
                        Description = cells[2],
                        Priority = cells[3],
                        Effort = cells[4],
                        Status = cells[5],
                        Owner = cells[6],
                        DoneCondition = cells[8]
                    };

                    foreach (int dependency in ParseDependencies(cells[7]))
                        task.Dependencies.Add(dependency);

                    document.Tasks.Add(task);
                    AddPhaseIfMissing(document, currentPhase);
                }
            }
        }

        private static void ParseBlockers(WallyTaskTrackerDocument document, string content)
        {
            string[] lines = content.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (!trimmed.StartsWith("| Task # |", StringComparison.OrdinalIgnoreCase)
                    && !trimmed.StartsWith("| Task #|", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                i += 2;
                for (; i < lines.Length; i++)
                {
                    string row = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(row))
                        continue;

                    if (!row.StartsWith("|", StringComparison.Ordinal) || IsSeparatorRow(row))
                    {
                        i--;
                        break;
                    }

                    List<string> cells = ParseTableCells(row);
                    if (cells.Count < 4)
                    {
                        i--;
                        break;
                    }

                    document.Notes.Add(new WallyTaskTrackerNote
                    {
                        TaskNumber = ParseRequiredInt(cells[0], "blocker task number"),
                        Note = cells[1],
                        Raised = cells[2],
                        Resolved = cells[3]
                    });
                }
            }
        }

        private static string BuildMarkdown(WallyTaskTrackerDocument document)
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.IsNullOrWhiteSpace(document.Title) ? "# Task Tracker" : document.Title);
            builder.AppendLine();
            builder.AppendLine($"**Source Proposal**: {document.SourceProposal}");
            builder.AppendLine($"**Status**: {document.Status}");
            builder.AppendLine($"**Created**: {document.Created}");
            builder.AppendLine($"**Last Updated**: {document.LastUpdated}");
            builder.AppendLine($"**Owner**: {document.Owner}");

            if (document.HeaderExtras.Count > 0)
            {
                builder.AppendLine();
                foreach (string extra in document.HeaderExtras)
                    builder.AppendLine(extra);
            }

            builder.AppendLine();

            bool blockersWritten = false;
            bool progressWritten = false;
            foreach (string heading in document.SectionOrder)
            {
                if (string.Equals(heading, "Task List", StringComparison.OrdinalIgnoreCase))
                {
                    AppendTaskList(builder, document);
                    continue;
                }

                if (string.Equals(heading, "Blockers & Notes", StringComparison.OrdinalIgnoreCase))
                {
                    if (document.Notes.Count > 0)
                    {
                        AppendBlockers(builder, document);
                        blockersWritten = true;
                    }

                    continue;
                }

                if (string.Equals(heading, "Progress Summary", StringComparison.OrdinalIgnoreCase))
                {
                    AppendProgressSummary(builder, document);
                    progressWritten = true;
                    continue;
                }

                AppendPreservedSection(builder, heading, document.PreservedSections.TryGetValue(heading, out string? content) ? content : string.Empty);
            }

            if (!blockersWritten && document.Notes.Count > 0)
                AppendBlockers(builder, document);

            if (!progressWritten)
                AppendProgressSummary(builder, document);

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static void AppendPreservedSection(StringBuilder builder, string heading, string content)
        {
            builder.AppendLine($"## {heading}");
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(content))
                builder.AppendLine(content.TrimEnd());
            builder.AppendLine();
        }

        private static void AppendTaskList(StringBuilder builder, WallyTaskTrackerDocument document)
        {
            builder.AppendLine("## Task List");
            builder.AppendLine();

            bool hasPhases = document.PhaseOrder.Any(phase => !string.IsNullOrWhiteSpace(phase));
            if (!hasPhases)
            {
                AppendTaskTable(builder, document.Tasks.OrderBy(task => task.Number).ToList());
                builder.AppendLine();
                return;
            }

            foreach (string phase in document.PhaseOrder.Where(phase => !string.IsNullOrWhiteSpace(phase)))
            {
                List<WallyTaskTrackerTask> phaseTasks = document.Tasks
                    .Where(task => string.Equals(task.PhaseTitle, phase, StringComparison.Ordinal))
                    .OrderBy(task => task.Number)
                    .ToList();
                if (phaseTasks.Count == 0)
                    continue;

                builder.AppendLine($"#### {phase}");
                builder.AppendLine();
                AppendTaskTable(builder, phaseTasks);
                builder.AppendLine();
            }

            List<WallyTaskTrackerTask> unphasedTasks = document.Tasks
                .Where(task => string.IsNullOrWhiteSpace(task.PhaseTitle))
                .OrderBy(task => task.Number)
                .ToList();
            if (unphasedTasks.Count > 0)
            {
                AppendTaskTable(builder, unphasedTasks);
                builder.AppendLine();
            }
        }

        private static void AppendTaskTable(StringBuilder builder, IReadOnlyList<WallyTaskTrackerTask> tasks)
        {
            builder.AppendLine("| # | Task | Description | Priority | Effort | Status | Owner | Dependencies | Done-Condition |");
            builder.AppendLine("|---|------|-------------|----------|--------|--------|-------|--------------|----------------|");

            foreach (WallyTaskTrackerTask task in tasks)
            {
                builder.AppendLine(
                    $"| {task.Number} | {EscapeTableCell(task.Title)} | {EscapeTableCell(task.Description)} | {EscapeTableCell(task.Priority)} | {EscapeTableCell(task.Effort)} | {EscapeTableCell(task.Status)} | {EscapeTableCell(task.Owner)} | {EscapeTableCell(FormatDependencies(task.Dependencies))} | {EscapeTableCell(task.DoneCondition)} |");
            }
        }

        private static void AppendBlockers(StringBuilder builder, WallyTaskTrackerDocument document)
        {
            builder.AppendLine("## Blockers & Notes");
            builder.AppendLine();
            builder.AppendLine("| Task # | Blocker / Note | Raised | Resolved |");
            builder.AppendLine("|--------|----------------|--------|----------|");

            foreach (WallyTaskTrackerNote note in document.Notes.OrderBy(note => note.TaskNumber).ThenBy(note => note.Raised, StringComparer.Ordinal))
            {
                builder.AppendLine(
                    $"| {note.TaskNumber} | {EscapeTableCell(note.Note)} | {EscapeTableCell(note.Raised)} | {EscapeTableCell(string.IsNullOrWhiteSpace(note.Resolved) ? "-" : note.Resolved)} |");
            }

            builder.AppendLine();
        }

        private static void AppendProgressSummary(StringBuilder builder, WallyTaskTrackerDocument document)
        {
            builder.AppendLine("## Progress Summary");
            builder.AppendLine();
            builder.AppendLine("| Phase | Total | Done | Active | Blocked | Remaining |");
            builder.AppendLine("|-------|-------|------|--------|---------|-----------|");

            foreach (string phase in document.PhaseOrder.Where(phase => !string.IsNullOrWhiteSpace(phase)))
            {
                List<WallyTaskTrackerTask> phaseTasks = document.Tasks
                    .Where(task => string.Equals(task.PhaseTitle, phase, StringComparison.Ordinal))
                    .ToList();
                if (phaseTasks.Count == 0)
                    continue;

                AppendProgressRow(builder, phase, phaseTasks, bold: false);
            }

            AppendProgressRow(builder, "Total", document.Tasks, bold: true);
            builder.AppendLine();
        }

        private static void AppendProgressRow(StringBuilder builder, string label, IReadOnlyCollection<WallyTaskTrackerTask> tasks, bool bold)
        {
            int total = tasks.Count;
            int done = tasks.Count(task => IsStatus(task.Status, "Complete"));
            int active = tasks.Count(task => IsStatus(task.Status, "In Progress"));
            int blocked = tasks.Count(task => IsStatus(task.Status, "Blocked"));
            int remaining = tasks.Count(task => IsStatus(task.Status, "Not Started"));

            if (bold)
            {
                builder.AppendLine($"| **{label}** | **{total}** | **{done}** | **{active}** | **{blocked}** | **{remaining}** |");
                return;
            }

            builder.AppendLine($"| {label} | {total} | {done} | {active} | {blocked} | {remaining} |");
        }

        private static void RefreshTrackerMetadata(WallyTaskTrackerDocument document)
        {
            string today = GetTodayStamp();
            if (string.IsNullOrWhiteSpace(document.Created))
                document.Created = today;

            document.LastUpdated = today;
            document.Status = ComputeTrackerStatus(document);
        }

        private static string ComputeTrackerStatus(WallyTaskTrackerDocument document)
        {
            if (document.Tasks.Count == 0)
                return "Active";

            if (document.Tasks.All(task => IsStatus(task.Status, "Complete")))
                return "Complete";

            if (document.Tasks.Any(task => IsStatus(task.Status, "In Progress")))
                return "Active";

            return HasEligibleWork(document) ? "Active" : "Blocked";
        }

        private static bool HasEligibleWork(WallyTaskTrackerDocument document)
        {
            return document.Tasks.Any(task =>
                !IsStatus(task.Status, "Complete")
                && AreDependenciesComplete(document, task)
                && (IsStatus(task.Status, "Not Started")
                    || (IsStatus(task.Status, "Blocked") && !HasActiveBlocker(document, task.Number))));
        }

        private static bool AreDependenciesComplete(WallyTaskTrackerDocument document, WallyTaskTrackerTask task)
        {
            foreach (int dependency in task.Dependencies)
            {
                WallyTaskTrackerTask dependencyTask = GetRequiredTask(document, dependency);
                if (!IsStatus(dependencyTask.Status, "Complete"))
                    return false;
            }

            return true;
        }

        private static bool HasActiveBlocker(WallyTaskTrackerDocument document, int taskNumber)
        {
            return document.Notes.Any(note => note.TaskNumber == taskNumber && !note.IsResolved);
        }

        private static void ResolveOpenNotes(WallyTaskTrackerDocument document, int taskNumber, string resolvedStamp)
        {
            foreach (WallyTaskTrackerNote note in document.Notes.Where(note => note.TaskNumber == taskNumber && !note.IsResolved))
                note.Resolved = resolvedStamp;
        }

        private static string BuildDependencySummary(WallyTaskTrackerDocument document, WallyTaskTrackerTask task)
        {
            if (task.Dependencies.Count == 0)
                return "-";

            return string.Join(", ", task.Dependencies.Select(dependency =>
            {
                WallyTaskTrackerTask dependencyTask = GetRequiredTask(document, dependency);
                return $"{dependency} ({dependencyTask.Status})";
            }));
        }

        private static void AddPhaseIfMissing(WallyTaskTrackerDocument document, string phaseTitle)
        {
            if (document.PhaseOrder.Contains(phaseTitle, StringComparer.Ordinal))
                return;

            document.PhaseOrder.Add(phaseTitle);
        }

        private static List<string> ParseTableCells(string row)
        {
            string trimmed = row.Trim();
            if (trimmed.StartsWith("|", StringComparison.Ordinal))
                trimmed = trimmed[1..];
            if (trimmed.EndsWith("|", StringComparison.Ordinal))
                trimmed = trimmed[..^1];

            return trimmed
                .Split('|')
                .Select(cell => cell.Trim())
                .ToList();
        }

        private static bool IsSeparatorRow(string row)
        {
            foreach (char c in row)
            {
                if (c != '|' && c != '-' && c != ' ' && c != ':')
                    return false;
            }

            return true;
        }

        private static IEnumerable<int> ParseDependencies(string value)
        {
            string normalized = NormalizeDashValue(value);
            if (string.IsNullOrWhiteSpace(normalized))
                yield break;

            foreach (string part in normalized.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                Match match = Regex.Match(part, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int dependency))
                    yield return dependency;
            }
        }

        private static int ParseRequiredInt(string value, string label)
        {
            if (!int.TryParse(value.Trim(), out int parsed))
                throw new InvalidOperationException($"Unable to parse {label} '{value}'.");

            return parsed;
        }

        private static int FindFirstSectionIndex(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("## ", StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static bool TryParseBullet(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            if (!line.StartsWith("- ", StringComparison.Ordinal))
                return false;

            int separatorIndex = line.IndexOf(':', 2);
            if (separatorIndex < 0)
                return false;

            key = line[2..separatorIndex].Trim();
            value = line[(separatorIndex + 1)..].Trim();
            return !string.IsNullOrWhiteSpace(key);
        }

        private static string FormatDependencies(IReadOnlyCollection<int> dependencies)
        {
            return dependencies.Count == 0 ? "-" : string.Join(", ", dependencies);
        }

        private static string EscapeTableCell(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Trim();
        }

        private static bool IsStatus(string value, string expected)
        {
            return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeOutcome(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeDashValue(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            return string.Equals(normalized, "-", StringComparison.Ordinal) ? string.Empty : normalized;
        }

        private static string FormatValueOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static string GetTodayStamp() => DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}