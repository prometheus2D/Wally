using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Wally.Core
{
    public sealed class InvestigationInteractionQuestion
    {
        public string QuestionId { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string ExpectedAnswerShape { get; set; } = string.Empty;
    }

    public sealed class InvestigationInteractionState
    {
        public string QuestionBatchId { get; set; } = string.Empty;

        public DateTimeOffset? AskedAtUtc { get; set; }

        public List<InvestigationInteractionQuestion> Questions { get; } = new();
    }

    public static class InvestigationInteractionStore
    {
        public const string InvestigationLoopName = "InvestigationLoop";
        public const string CurrentInvestigationFolder = "Actors/Investigator/Active/CurrentInvestigation";
        public const string DefaultInteractionStatePath = CurrentInvestigationFolder + "/InteractionState.md";
        public const string DefaultUserResponsesPath = CurrentInvestigationFolder + "/UserResponses.md";
        public const string DefaultLatestUserResponsePath = CurrentInvestigationFolder + "/LatestUserResponse.md";

        private static readonly Regex QuestionHeadingRegex = new(
            @"^###\s+(?<id>\S+)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex InlineAnswerRegex = new(
            @"^(?<id>[A-Za-z][A-Za-z0-9_-]*)\s*:\s*(?<answer>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsInvestigationLoop(string? loopName)
        {
            return string.Equals(loopName, InvestigationLoopName, StringComparison.OrdinalIgnoreCase);
        }

        public static InvestigationInteractionState? TryLoadCurrent(WallyEnvironment env)
        {
            ArgumentNullException.ThrowIfNull(env);

            if (!env.HasWorkspace)
                return null;

            string filePath = env.ResolveWorkspaceFilePath(DefaultInteractionStatePath);
            if (!File.Exists(filePath))
                return null;

            return ParseInteractionState(File.ReadAllLines(filePath));
        }

        public static bool TryLoadWaiting(WallyEnvironment env, out InvestigationInteractionState? state)
        {
            state = TryLoadCurrent(env);
            if (state == null || state.Questions.Count == 0)
                return false;

            if (TryLoadExecutionState(env, out WallyLoopExecutionState? executionState) && executionState != null)
            {
                return string.Equals(executionState.Status, "WaitingForUser", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        public static bool TryLoadCurrentRunId(WallyEnvironment env, out string runId)
        {
            ArgumentNullException.ThrowIfNull(env);

            runId = string.Empty;
            if (!TryLoadExecutionState(env, out WallyLoopExecutionState? executionState) ||
                executionState == null ||
                string.IsNullOrWhiteSpace(executionState.RunId))
            {
                return false;
            }

            runId = executionState.RunId;
            return true;
        }

        public static bool TryRecordResponse(
            WallyEnvironment env,
            string responseMarkdown,
            string source,
            out InvestigationInteractionState? updatedState)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentException.ThrowIfNullOrWhiteSpace(responseMarkdown);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);

            updatedState = null;
            if (!TryLoadWaiting(env, out InvestigationInteractionState? state) || state == null)
                return false;

            DateTimeOffset recordedAtUtc = DateTimeOffset.UtcNow;
            var answers = ParseAnswerBatch(state.Questions, responseMarkdown.Trim());
            string runId = TryLoadCurrentRunId(env, out string currentRunId)
                ? currentRunId
                : InvestigationLoopName;

            string responsesPath = env.ResolveWorkspaceFilePath(DefaultUserResponsesPath);
            Directory.CreateDirectory(Path.GetDirectoryName(responsesPath)!);
            string existingResponses = File.Exists(responsesPath)
                ? File.ReadAllText(responsesPath)
                : string.Empty;
            File.WriteAllText(
                responsesPath,
                BuildUpdatedUserResponses(existingResponses, runId, state, answers, source, recordedAtUtc));

            string latestResponsePath = env.ResolveWorkspaceFilePath(DefaultLatestUserResponsePath);
            Directory.CreateDirectory(Path.GetDirectoryName(latestResponsePath)!);
            File.WriteAllText(
                latestResponsePath,
                BuildLatestUserResponse(runId, state, answers, source, recordedAtUtc));

            string interactionStatePath = env.ResolveWorkspaceFilePath(DefaultInteractionStatePath);
            if (File.Exists(interactionStatePath))
                File.Delete(interactionStatePath);

            updatedState = state;
            return true;
        }

        public static InvestigationInteractionState PersistWaitingQuestions(
            WallyEnvironment env,
            IReadOnlyList<InvestigationInteractionQuestion> questions)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(questions);

            List<InvestigationInteractionQuestion> normalizedQuestions = NormalizeQuestions(questions);
            if (normalizedQuestions.Count == 0)
                throw new InvalidOperationException("At least one question is required to persist a waiting interaction.");

            DateTimeOffset recordedAtUtc = DateTimeOffset.UtcNow;

            var state = new InvestigationInteractionState
            {
                QuestionBatchId = BuildQuestionBatchId(recordedAtUtc),
                AskedAtUtc = recordedAtUtc
            };

            foreach (InvestigationInteractionQuestion question in normalizedQuestions)
                state.Questions.Add(question);

            string interactionStatePath = env.ResolveWorkspaceFilePath(DefaultInteractionStatePath);
            Directory.CreateDirectory(Path.GetDirectoryName(interactionStatePath)!);
            File.WriteAllText(interactionStatePath, BuildInteractionStateMarkdown(state));
            return state;
        }

        public static List<InvestigationInteractionQuestion> ParseQuestionBatchMarkdown(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return new List<InvestigationInteractionQuestion>();

            List<InvestigationInteractionQuestion> structuredQuestions = ParseStructuredQuestionBatch(markdown);
            if (structuredQuestions.Count > 0)
                return structuredQuestions;

            List<InvestigationInteractionQuestion> listQuestions = ParseListQuestionBatch(markdown);
            if (listQuestions.Count > 0)
                return listQuestions;

            return NormalizeQuestions(new[]
            {
                new InvestigationInteractionQuestion
                {
                    Text = markdown.Trim(),
                    Reason = "Clarification is required before the investigation can continue.",
                    ExpectedAnswerShape = "free_text"
                }
            });
        }

        public static string BuildWaitingDisplayText(WallyEnvironment env, InvestigationInteractionState state)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(state);

            string runLabel = TryLoadCurrentRunId(env, out string runId)
                ? runId
                : InvestigationLoopName;

            var builder = new StringBuilder();
            builder.AppendLine($"Investigation {runLabel} is waiting for user input.");
            builder.AppendLine($"Question batch: {state.QuestionBatchId}");
            builder.AppendLine();

            for (int i = 0; i < state.Questions.Count; i++)
            {
                InvestigationInteractionQuestion question = state.Questions[i];
                builder.AppendLine($"{i + 1}. [{question.QuestionId}] {question.Text}");
                if (!string.IsNullOrWhiteSpace(question.Reason))
                    builder.AppendLine($"   Reason: {question.Reason}");
                if (!string.IsNullOrWhiteSpace(question.ExpectedAnswerShape))
                    builder.AppendLine($"   Expected answer: {question.ExpectedAnswerShape}");
                builder.AppendLine();
            }

            if (state.Questions.Count > 1)
            {
                builder.AppendLine("When answering multiple questions, use question-id headings, for example:");
                builder.AppendLine("### Q-001");
                builder.AppendLine("Your answer");
                builder.AppendLine();
            }

            builder.AppendLine($"Resume command: {BuildResumeCommand(InvestigationLoopName)}");

            return builder.ToString().TrimEnd();
        }

        private static InvestigationInteractionState ParseInteractionState(string[] lines)
        {
            var state = new InvestigationInteractionState();
            InvestigationInteractionQuestion? currentQuestion = null;
            bool inMetadata = false;
            bool inQuestions = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                if (string.Equals(trimmed, "## Metadata", StringComparison.OrdinalIgnoreCase))
                {
                    inMetadata = true;
                    inQuestions = false;
                    currentQuestion = null;
                    continue;
                }

                if (string.Equals(trimmed, "## Questions", StringComparison.OrdinalIgnoreCase))
                {
                    inMetadata = false;
                    inQuestions = true;
                    currentQuestion = null;
                    continue;
                }

                if (inMetadata && TryParseBullet(trimmed, out string key, out string value))
                {
                    ApplyMetadata(state, key, value);
                    continue;
                }

                if (!inQuestions)
                    continue;

                Match headingMatch = QuestionHeadingRegex.Match(trimmed);
                if (headingMatch.Success)
                {
                    currentQuestion = new InvestigationInteractionQuestion
                    {
                        QuestionId = headingMatch.Groups["id"].Value.Trim()
                    };
                    state.Questions.Add(currentQuestion);
                    continue;
                }

                if (currentQuestion != null && TryParseBullet(trimmed, out key, out value))
                {
                    ApplyQuestionMetadata(currentQuestion, key, value);
                }
            }

            return state;
        }

        private static void ApplyMetadata(InvestigationInteractionState state, string key, string value)
        {
            switch (key)
            {
                case "QuestionBatchId":
                    state.QuestionBatchId = value;
                    break;
                case "AskedAtUtc":
                    state.AskedAtUtc = ParseTimestamp(value);
                    break;
            }
        }

        private static void ApplyQuestionMetadata(InvestigationInteractionQuestion question, string key, string value)
        {
            switch (key)
            {
                case "Text":
                    question.Text = value;
                    break;
                case "Reason":
                    question.Reason = value;
                    break;
                case "ExpectedAnswerShape":
                    question.ExpectedAnswerShape = value;
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

        private static DateTimeOffset? ParseTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
                ? parsed
                : null;
        }

        private static Dictionary<string, string> ParseAnswerBatch(
            IReadOnlyList<InvestigationInteractionQuestion> questions,
            string responseMarkdown)
        {
            var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (questions.Count == 0)
                return answers;

            if (questions.Count == 1)
            {
                answers[questions[0].QuestionId] = responseMarkdown;
                return answers;
            }

            var structuredAnswers = ParseStructuredAnswers(responseMarkdown);
            if (structuredAnswers.Count == 0)
            {
                answers[questions[0].QuestionId] = responseMarkdown;
                return answers;
            }

            foreach (InvestigationInteractionQuestion question in questions)
            {
                if (structuredAnswers.TryGetValue(question.QuestionId, out string? answer) &&
                    !string.IsNullOrWhiteSpace(answer))
                {
                    answers[question.QuestionId] = answer.Trim();
                }
            }

            return answers;
        }

        private static Dictionary<string, string> ParseStructuredAnswers(string responseMarkdown)
        {
            var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string normalized = responseMarkdown.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
            string? currentQuestionId = null;
            StringBuilder? currentBody = null;

            void CommitCurrent()
            {
                if (string.IsNullOrWhiteSpace(currentQuestionId) || currentBody == null)
                    return;

                string body = currentBody.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(body))
                    answers[currentQuestionId] = body;
            }

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                Match headingMatch = QuestionHeadingRegex.Match(trimmed);
                if (headingMatch.Success)
                {
                    CommitCurrent();
                    currentQuestionId = headingMatch.Groups["id"].Value.Trim();
                    currentBody = new StringBuilder();
                    continue;
                }

                if (currentQuestionId == null)
                {
                    Match inlineMatch = InlineAnswerRegex.Match(trimmed);
                    if (inlineMatch.Success)
                    {
                        answers[inlineMatch.Groups["id"].Value.Trim()] =
                            inlineMatch.Groups["answer"].Value.Trim();
                    }
                    continue;
                }

                if (currentBody!.Length > 0)
                    currentBody.AppendLine();
                currentBody.Append(line);
            }

            CommitCurrent();
            return answers;
        }

        private static List<InvestigationInteractionQuestion> ParseStructuredQuestionBatch(string markdown)
        {
            var questions = new List<InvestigationInteractionQuestion>();
            string normalized = markdown.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
            InvestigationInteractionQuestion? currentQuestion = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                Match headingMatch = QuestionHeadingRegex.Match(trimmed);
                if (headingMatch.Success)
                {
                    currentQuestion = new InvestigationInteractionQuestion
                    {
                        QuestionId = headingMatch.Groups["id"].Value.Trim()
                    };
                    questions.Add(currentQuestion);
                    continue;
                }

                if (currentQuestion == null)
                    continue;

                if (TryParseBullet(trimmed, out string key, out string value))
                {
                    ApplyQuestionMetadata(currentQuestion, key, value);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentQuestion.Text) && !string.IsNullOrWhiteSpace(trimmed))
                    currentQuestion.Text = trimmed;
            }

            return NormalizeQuestions(questions);
        }

        private static List<InvestigationInteractionQuestion> ParseListQuestionBatch(string markdown)
        {
            var questions = new List<InvestigationInteractionQuestion>();
            string normalized = markdown.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');

            foreach (string rawLine in lines)
            {
                string trimmed = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                string questionText = ExtractListQuestionText(trimmed);
                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                questions.Add(new InvestigationInteractionQuestion
                {
                    Text = questionText,
                    Reason = "Clarification is required before the investigation can continue.",
                    ExpectedAnswerShape = "free_text"
                });
            }

            return NormalizeQuestions(questions);
        }

        private static string ExtractListQuestionText(string trimmedLine)
        {
            if (trimmedLine.Length < 3)
                return string.Empty;

            if (trimmedLine.StartsWith("- ", StringComparison.Ordinal) ||
                trimmedLine.StartsWith("* ", StringComparison.Ordinal))
            {
                return trimmedLine[2..].Trim();
            }

            int dotIndex = trimmedLine.IndexOf('.');
            if (dotIndex > 0 && int.TryParse(trimmedLine[..dotIndex], out _))
                return trimmedLine[(dotIndex + 1)..].Trim();

            return string.Empty;
        }

        private static List<InvestigationInteractionQuestion> NormalizeQuestions(
            IEnumerable<InvestigationInteractionQuestion> questions)
        {
            var normalized = new List<InvestigationInteractionQuestion>();
            int questionNumber = 1;

            foreach (InvestigationInteractionQuestion question in questions)
            {
                if (question == null)
                    continue;

                string text = question.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                normalized.Add(new InvestigationInteractionQuestion
                {
                    QuestionId = !string.IsNullOrWhiteSpace(question.QuestionId)
                        ? question.QuestionId.Trim()
                        : $"Q-{questionNumber:000}",
                    Text = text,
                    Reason = string.IsNullOrWhiteSpace(question.Reason)
                        ? "Clarification is required before the investigation can continue."
                        : question.Reason.Trim(),
                    ExpectedAnswerShape = string.IsNullOrWhiteSpace(question.ExpectedAnswerShape)
                        ? "free_text"
                        : question.ExpectedAnswerShape.Trim()
                });

                questionNumber++;
            }

            return normalized;
        }

        private static string BuildUpdatedUserResponses(
            string existingResponses,
            string runId,
            InvestigationInteractionState state,
            IReadOnlyDictionary<string, string> answers,
            string source,
            DateTimeOffset recordedAtUtc)
        {
            var builder = new StringBuilder();

            if (string.IsNullOrWhiteSpace(existingResponses))
            {
                builder.AppendLine("# User Responses");
                builder.AppendLine();
            }
            else
            {
                builder.Append(existingResponses.TrimEnd());
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"## Batch {state.QuestionBatchId}");
            builder.AppendLine($"- InvestigationId: {runId}");
            builder.AppendLine($"- RecordedAtUtc: {recordedAtUtc:O}");
            builder.AppendLine($"- Source: {source}");
            builder.AppendLine();

            foreach (InvestigationInteractionQuestion question in state.Questions)
            {
                builder.AppendLine($"### {question.QuestionId}");
                builder.AppendLine(
                    answers.TryGetValue(question.QuestionId, out string? answer) && !string.IsNullOrWhiteSpace(answer)
                        ? answer.Trim()
                        : "No answer provided.");
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static string BuildLatestUserResponse(
            string runId,
            InvestigationInteractionState state,
            IReadOnlyDictionary<string, string> answers,
            string source,
            DateTimeOffset recordedAtUtc)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Latest User Response");
            builder.AppendLine();
            builder.AppendLine("## Metadata");
            builder.AppendLine($"- InvestigationId: {runId}");
            builder.AppendLine($"- QuestionBatchId: {state.QuestionBatchId}");
            builder.AppendLine($"- RecordedAtUtc: {recordedAtUtc:O}");
            builder.AppendLine($"- Source: {source}");
            builder.AppendLine();
            builder.AppendLine("## Answers");
            builder.AppendLine();

            foreach (InvestigationInteractionQuestion question in state.Questions)
            {
                builder.AppendLine($"### {question.QuestionId}");
                builder.AppendLine($"- Question: {question.Text}");
                builder.AppendLine(
                    answers.TryGetValue(question.QuestionId, out string? answer) && !string.IsNullOrWhiteSpace(answer)
                        ? $"- Answer: {answer.Trim()}"
                        : "- Answer: No answer provided.");
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static string BuildInteractionStateMarkdown(InvestigationInteractionState state)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Interaction State");
            builder.AppendLine();
            builder.AppendLine("## Metadata");
            builder.AppendLine($"- QuestionBatchId: {state.QuestionBatchId}");
            builder.AppendLine($"- AskedAtUtc: {FormatTimestamp(state.AskedAtUtc)}");
            builder.AppendLine();
            builder.AppendLine("## Questions");
            builder.AppendLine();

            foreach (InvestigationInteractionQuestion question in state.Questions)
            {
                builder.AppendLine($"### {question.QuestionId}");
                builder.AppendLine($"- Text: {question.Text}");
                builder.AppendLine($"- Reason: {question.Reason}");
                builder.AppendLine($"- ExpectedAnswerShape: {question.ExpectedAnswerShape}");
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static string FormatTimestamp(DateTimeOffset? value)
        {
            return value.HasValue ? value.Value.ToString("O") : string.Empty;
        }

        private static bool TryLoadExecutionState(WallyEnvironment env, out WallyLoopExecutionState? executionState)
        {
            executionState = null;

            if (!env.HasWorkspace)
                return false;

            WallyLoopDefinition? loopDef = env.GetLoop(InvestigationLoopName);
            if (loopDef == null)
                return false;

            return WallyLoopExecutionStateStore.TryLoadCurrent(env, loopDef, out executionState);
        }

        private static string BuildResumeCommand(string loopName)
        {
            return $"wally run \"<answer batch>\" -l {loopName}";
        }

        private static string BuildQuestionBatchId(DateTimeOffset timestamp)
        {
            return $"QB-{timestamp:yyyyMMddHHmmssfff}";
        }
    }
}