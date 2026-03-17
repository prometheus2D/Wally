using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// A serializable definition for a <see cref="WallyLoop"/>, loaded from a JSON file.
    /// <para>
    /// Each <c>.json</c> file in the workspace's <c>Loops/</c> folder defines one loop.
    /// The definition specifies the actor to use, the initial prompt, how to build
    /// continuation prompts, and the stop conditions.
    /// </para>
    /// <para>
    /// When the <see cref="Steps"/> list is non-empty the loop executes each step in
    /// order on every pass. Steps may each use a different actor, prompt template, and
    /// stop keywords. Legacy single-actor loops (no steps) continue to work unchanged.
    /// </para>
    /// <code>
    /// // Legacy single-actor loop (steps omitted):
    /// {
    ///   "name": "CodeReview",
    ///   "actorName": "Engineer",
    ///   "startPrompt": "Review the codebase for bugs.",
    ///   "maxIterations": 5
    /// }
    ///
    /// // Multi-step loop:
    /// {
    ///   "name": "AnalyseAndReview",
    ///   "description": "BA analyses, then Engineer validates",
    ///   "maxIterations": 3,
    ///   "steps": [
    ///     {
    ///       "name": "Analyse",
    ///       "actorName": "BusinessAnalyst",
    ///       "promptTemplate": "Analyse this area: {userPrompt}",
    ///       "continuePromptTemplate": "Continue analysis. Previous:\n---\n{previousResult}\n---\nIf done: {completedKeyword}"
    ///     },
    ///     {
    ///       "name": "Review",
    ///       "actorName": "Engineer",
    ///       "promptTemplate": "Review this analysis and validate it against the code:\n{previousStepResult}\n\nOriginal request: {userPrompt}",
    ///       "continuePromptTemplate": "Continue the review. Previous pass:\n---\n{previousResult}\n---\nIf done: {completedKeyword}"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public class WallyLoopDefinition
    {
        // ??? Identity ?????????????????????????????????????????????????????????

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

        // ??? Single-actor (legacy) properties ????????????????????????????????

        /// <summary>
        /// The name of the actor to run on each iteration.
        /// Used when <see cref="Steps"/> is empty (legacy single-actor mode).
        /// In multi-step mode, individual steps override this; it acts as a
        /// fallback actor name for steps that do not specify one.
        /// Must match one of the loaded actors (case-insensitive).
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// The prompt used for the first iteration of the loop.
        /// Used when <see cref="Steps"/> is empty (legacy single-actor mode).
        /// Supports the placeholder <c>{userPrompt}</c>.
        /// </summary>
        public string StartPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Template for building the continuation prompt on iterations after the first.
        /// Used when <see cref="Steps"/> is empty (legacy single-actor mode).
        /// <para>Supports: <c>{previousResult}</c>, <c>{completedKeyword}</c>,
        /// <c>{errorKeyword}</c>, <c>{userPrompt}</c>.</para>
        /// When <see langword="null"/> or empty, a sensible default template is used.
        /// </summary>
        public string? ContinuePromptTemplate { get; set; }

        // ??? Steps ????????????????????????????????????????????????????????????

        /// <summary>
        /// The ordered sequence of steps that make up this loop.
        /// <para>
        /// When non-empty, the loop iterates over these steps in order on each pass.
        /// Each step may use a different actor and has its own prompt templates and
        /// stop conditions.
        /// </para>
        /// <para>
        /// When empty, the loop falls back to legacy single-actor behaviour using
        /// <see cref="ActorName"/>, <see cref="StartPrompt"/>, and
        /// <see cref="ContinuePromptTemplate"/>.
        /// </para>
        /// </summary>
        public List<WallyStepDefinition> Steps { get; set; } = new();

        /// <summary>
        /// Returns <see langword="true"/> when this loop defines explicit steps
        /// rather than using the legacy single-actor model.
        /// </summary>
        [JsonIgnore]
        public bool HasSteps => Steps != null && Steps.Count > 0;

        // ??? Stop conditions ??????????????????????????????????????????????????

        /// <summary>
        /// Keyword that signals the loop (or a step) has completed successfully.
        /// Detected case-insensitively in the iteration result.
        /// Defaults to <see cref="WallyLoop.CompletedKeyword"/> when null or empty.
        /// Individual steps may override this via <see cref="WallyStepDefinition.CompletedKeyword"/>.
        /// </summary>
        public string? CompletedKeyword { get; set; }

        /// <summary>
        /// Keyword that signals the actor detected an error.
        /// Detected case-insensitively in the iteration result.
        /// Defaults to <see cref="WallyLoop.ErrorKeyword"/> when null or empty.
        /// Individual steps may override this via <see cref="WallyStepDefinition.ErrorKeyword"/>.
        /// </summary>
        public string? ErrorKeyword { get; set; }

        /// <summary>
        /// Maximum number of iterations for this loop.
        /// In single-actor mode: total loop iterations.
        /// In multi-step mode: the maximum number of full step-sequence passes.
        /// When <c>0</c> or negative, the workspace's <see cref="WallyConfig.MaxIterations"/> is used.
        /// </summary>
        public int MaxIterations { get; set; }

        // ??? Resolved keywords ????????????????????????????????????????????????

        /// <summary>Returns the effective completed keyword, falling back to the default.</summary>
        [JsonIgnore]
        public string ResolvedCompletedKeyword =>
            string.IsNullOrWhiteSpace(CompletedKeyword) ? WallyLoop.CompletedKeyword : CompletedKeyword!;

        /// <summary>Returns the effective error keyword, falling back to the default.</summary>
        [JsonIgnore]
        public string ResolvedErrorKeyword =>
            string.IsNullOrWhiteSpace(ErrorKeyword) ? WallyLoop.ErrorKeyword : ErrorKeyword!;

        // ??? Legacy continue-prompt builder ??????????????????????????????????

        private static readonly string DefaultContinueTemplate =
            "You are continuing a task. Here is your previous response:\n\n" +
            "---\n{previousResult}\n---\n\n" +
            "Continue where you left off. " +
            "If you are finished, respond with: {completedKeyword}\n" +
            "If something went wrong, respond with: {errorKeyword}";

        /// <summary>
        /// Builds the continuation prompt for the legacy single-actor mode.
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

        // ??? Step builder ?????????????????????????????????????????????????????

        /// <summary>
        /// Builds a runtime <see cref="WallyStep"/> from a <see cref="WallyStepDefinition"/>,
        /// binding the provided <paramref name="stepAction"/> as the work lambda and
        /// resolving all prompt templates and keywords.
        /// </summary>
        /// <param name="stepDef">The step definition to materialise.</param>
        /// <param name="userPrompt">The original runtime user prompt.</param>
        /// <param name="stepAction">The action lambda to execute on each step iteration.</param>
        /// <param name="fallbackMaxIterations">
        /// Used when neither the step nor the loop defines a <c>MaxIterations</c>.
        /// </param>
        public WallyStep BuildStep(
            WallyStepDefinition stepDef,
            string userPrompt,
            Func<string, string> stepAction,
            int fallbackMaxIterations = 1)
        {
            string completedKw = stepDef.ResolveCompletedKeyword(ResolvedCompletedKeyword);
            string errorKw     = stepDef.ResolveErrorKeyword(ResolvedErrorKeyword);

            int maxIter = stepDef.MaxIterations > 0  ? stepDef.MaxIterations  :
                          MaxIterations          > 0  ? MaxIterations          :
                          fallbackMaxIterations;

            Func<string, string?, string> startFactory = (up, prev) =>
                stepDef.BuildStartPrompt(up, prev, completedKw, errorKw);

            Func<string, string, string?, string> continueFactory = (prevResult, up, prevStep) =>
                stepDef.BuildContinuePrompt(prevResult, up, prevStep, completedKw, errorKw);

            string stepName = string.IsNullOrWhiteSpace(stepDef.Name) ? "(unnamed step)" : stepDef.Name;

            return new WallyStep(
                name:                  stepName,
                description:           stepDef.Description,
                action:                stepAction,
                startPromptFactory:    startFactory,
                continuePromptFactory: continueFactory,
                maxIterations:         maxIter,
                completedKeyword:      completedKw,
                errorKeyword:          errorKw);
        }

        // ??? Serialization ????????????????????????????????????????????????????

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
