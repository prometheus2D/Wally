using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Wally.Core
{
    /// <summary>
    /// A serializable definition for a <see cref="WallyLoop"/>, loaded from a JSON file.
    /// <para>
    /// Each <c>.json</c> file in the workspace's <c>Loops/</c> folder defines one loop.
    /// The definition specifies the actor to use, the initial prompt, how to build
    /// continuation prompts, and the stop conditions.
    /// </para>
    /// <code>
    /// {
    ///   "name": "CodeReview",
    ///   "description": "Iterative code review with the Engineer actor",
    ///   "actorName": "Engineer",
    ///   "startPrompt": "Review the codebase for bugs, security issues, and code quality problems.",
    ///   "continuePromptTemplate": "You are continuing a code review task. Here is your previous response:\n\n---\n{previousResult}\n---\n\nContinue reviewing. If you are finished, respond with: {completedKeyword}\nIf something went wrong, respond with: {errorKeyword}",
    ///   "completedKeyword": "[LOOP COMPLETED]",
    ///   "errorKeyword": "[LOOP ERROR]",
    ///   "maxIterations": 5
    /// }
    /// </code>
    /// </summary>
    public class WallyLoopDefinition
    {
        /// <summary>
        /// A short, unique name for this loop (e.g. <c>"CodeReview"</c>, <c>"Refactor"</c>).
        /// Used as the identifier when running the loop from the CLI or UI.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this loop does.
        /// Displayed in <c>list-loops</c> and the UI.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The name of the actor to run on each iteration.
        /// Must match one of the loaded actors (case-insensitive).
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// The prompt used for the first iteration of the loop.
        /// Supports the placeholder <c>{userPrompt}</c> which is replaced with
        /// the user's input at runtime.
        /// </summary>
        public string StartPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Template for building the continuation prompt on iterations after the first.
        /// <para>
        /// Supports the following placeholders:
        /// <list type="bullet">
        ///   <item><c>{previousResult}</c> — the full text of the previous iteration's response.</item>
        ///   <item><c>{completedKeyword}</c> — resolves to <see cref="CompletedKeyword"/>.</item>
        ///   <item><c>{errorKeyword}</c> — resolves to <see cref="ErrorKeyword"/>.</item>
        ///   <item><c>{userPrompt}</c> — the original user prompt.</item>
        /// </list>
        /// </para>
        /// When <see langword="null"/> or empty, a sensible default template is used.
        /// </summary>
        public string? ContinuePromptTemplate { get; set; }

        /// <summary>
        /// Keyword that signals the loop has completed successfully.
        /// Detected case-insensitively in the iteration result.
        /// Defaults to <see cref="WallyLoop.CompletedKeyword"/> when null or empty.
        /// </summary>
        public string? CompletedKeyword { get; set; }

        /// <summary>
        /// Keyword that signals the actor detected an error.
        /// Detected case-insensitively in the iteration result.
        /// Defaults to <see cref="WallyLoop.ErrorKeyword"/> when null or empty.
        /// </summary>
        public string? ErrorKeyword { get; set; }

        /// <summary>
        /// Maximum number of iterations for this loop.
        /// When <c>0</c> or negative, the workspace's <see cref="WallyConfig.MaxIterations"/> is used.
        /// </summary>
        public int MaxIterations { get; set; }

        // — Resolved keywords ————————————————————————————————————————————————

        /// <summary>Returns the effective completed keyword, falling back to the default.</summary>
        public string ResolvedCompletedKeyword =>
            string.IsNullOrWhiteSpace(CompletedKeyword) ? WallyLoop.CompletedKeyword : CompletedKeyword!;

        /// <summary>Returns the effective error keyword, falling back to the default.</summary>
        public string ResolvedErrorKeyword =>
            string.IsNullOrWhiteSpace(ErrorKeyword) ? WallyLoop.ErrorKeyword : ErrorKeyword!;

        // — Continue prompt builder ——————————————————————————————————————————

        private static readonly string DefaultContinueTemplate =
            "You are continuing a task. Here is your previous response:\n\n" +
            "---\n{previousResult}\n---\n\n" +
            "Continue where you left off. " +
            "If you are finished, respond with: {completedKeyword}\n" +
            "If something went wrong, respond with: {errorKeyword}";

        /// <summary>
        /// Builds the continuation prompt for the given previous result and original user prompt.
        /// </summary>
        public string BuildContinuePrompt(string previousResult, string userPrompt)
        {
            string template = string.IsNullOrWhiteSpace(ContinuePromptTemplate)
                ? DefaultContinueTemplate
                : ContinuePromptTemplate!;

            return template
                .Replace("{previousResult}", previousResult)
                .Replace("{completedKeyword}", ResolvedCompletedKeyword)
                .Replace("{errorKeyword}", ResolvedErrorKeyword)
                .Replace("{userPrompt}", userPrompt);
        }

        // — Serialization ————————————————————————————————————————————————————

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Deserializes a <see cref="WallyLoopDefinition"/> from a JSON file.</summary>
        public static WallyLoopDefinition LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WallyLoopDefinition>(json, _jsonOptions)
                   ?? new WallyLoopDefinition { Name = Path.GetFileNameWithoutExtension(filePath) };
        }

        /// <summary>Serializes this definition to a JSON file.</summary>
        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this, _jsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads all <c>*.json</c> files from <paramref name="loopsFolder"/>.
        /// Skips files that fail to parse (logs a warning to stderr).
        /// </summary>
        public static List<WallyLoopDefinition> LoadFromFolder(string loopsFolder)
        {
            var loops = new List<WallyLoopDefinition>();
            if (!Directory.Exists(loopsFolder)) return loops;

            foreach (string file in Directory.GetFiles(loopsFolder, "*.json"))
            {
                try
                {
                    var def = LoadFromFile(file);
                    if (string.IsNullOrWhiteSpace(def.Name))
                        def.Name = Path.GetFileNameWithoutExtension(file);
                    loops.Add(def);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to load loop definition '{file}': {ex.Message}");
                }
            }

            return loops;
        }
    }
}
