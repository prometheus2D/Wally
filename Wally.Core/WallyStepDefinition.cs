using System;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// Defines a single step within a <see cref="WallyLoopDefinition"/>.
    /// <para>
    /// A step is the atomic unit of a <see cref="WallyLoop"/>. Each iteration of the
    /// loop executes the step sequence in order — each step may use a different actor,
    /// a different prompt strategy, and its own stop keywords.
    /// </para>
    /// <para>
    /// Steps are defined in the loop's JSON file under the <c>steps</c> array:
    /// </para>
    /// <code>
    /// {
    ///   "name": "MultiActorReview",
    ///   "steps": [
    ///     {
    ///       "name": "Analyse",
    ///       "actorName": "BusinessAnalyst",
    ///       "promptTemplate": "Analyse the following area of the codebase: {userPrompt}",
    ///       "continuePromptTemplate": "Continue your analysis. Previous pass:\n---\n{previousResult}\n---\nIf done: {completedKeyword}"
    ///     },
    ///     {
    ///       "name": "Review",
    ///       "actorName": "Engineer",
    ///       "promptTemplate": "Review the following analysis and validate it against the code:\n{previousStepResult}",
    ///       "continuePromptTemplate": "Continue the review. Previous pass:\n---\n{previousResult}\n---\nIf done: {completedKeyword}"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// <para>
    /// Prompt template placeholders:
    /// <list type="bullet">
    ///   <item><c>{userPrompt}</c> — the original runtime user prompt.</item>
    ///   <item><c>{previousResult}</c> — the result of the previous iteration of <em>this step</em>.</item>
    ///   <item><c>{previousStepResult}</c> — the result of the immediately preceding step in this pass.</item>
    ///   <item><c>{completedKeyword}</c> — the effective completed keyword for this step.</item>
    ///   <item><c>{errorKeyword}</c> — the effective error keyword for this step.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WallyStepDefinition
    {
        // ??? Identity ??????????????????????????????????????????????????????????

        /// <summary>
        /// Short identifier for this step (e.g. <c>"Analyse"</c>, <c>"Review"</c>).
        /// Used in log output and loop result metadata. Falls back to the step index
        /// when omitted.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional human-readable description shown in <c>list-loops</c>.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // ??? Actor ????????????????????????????????????????????????????????????

        /// <summary>
        /// The name of the actor to use for this step (case-insensitive).
        /// When empty, the step runs in direct mode (no actor enrichment) unless the
        /// parent loop's <see cref="WallyLoopDefinition.ActorName"/> provides a fallback.
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        // ??? Prompts ??????????????????????????????????????????????????????????

        /// <summary>
        /// The prompt template used for the <em>first</em> iteration of this step.
        /// When empty, the loop's <see cref="WallyLoopDefinition.StartPrompt"/> is
        /// used for the first step, and <c>{previousStepResult}</c> is used for
        /// subsequent steps in the same pass.
        /// <para>Supports: <c>{userPrompt}</c>, <c>{previousStepResult}</c>,
        /// <c>{completedKeyword}</c>, <c>{errorKeyword}</c>.</para>
        /// </summary>
        public string PromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Template for building the prompt on subsequent iterations of <em>this step</em>
        /// (i.e. when the step loops back before advancing to the next step).
        /// When empty, a sensible default is used.
        /// <para>Supports: <c>{userPrompt}</c>, <c>{previousResult}</c>,
        /// <c>{previousStepResult}</c>, <c>{completedKeyword}</c>, <c>{errorKeyword}</c>.</para>
        /// </summary>
        public string? ContinuePromptTemplate { get; set; }

        // ??? Stop conditions ??????????????????????????????????????????????????

        /// <summary>
        /// Keyword that signals this step completed successfully.
        /// When null or empty, inherits from the parent loop's
        /// <see cref="WallyLoopDefinition.CompletedKeyword"/> (or the global default).
        /// </summary>
        public string? CompletedKeyword { get; set; }

        /// <summary>
        /// Keyword that signals this step encountered an error.
        /// When null or empty, inherits from the parent loop's
        /// <see cref="WallyLoopDefinition.ErrorKeyword"/> (or the global default).
        /// </summary>
        public string? ErrorKeyword { get; set; }

        /// <summary>
        /// Maximum number of iterations for this individual step before it is
        /// considered complete and the loop advances to the next step.
        /// When <c>0</c>, the parent loop's <see cref="WallyLoopDefinition.MaxIterations"/>
        /// is used.
        /// </summary>
        public int MaxIterations { get; set; }

        // ??? Resolved keywords (runtime) ?????????????????????????????????????

        /// <summary>
        /// Resolves the effective completed keyword, falling back to
        /// <paramref name="loopFallback"/> and then the global default.
        /// </summary>
        public string ResolveCompletedKeyword(string? loopFallback) =>
            !string.IsNullOrWhiteSpace(CompletedKeyword)  ? CompletedKeyword! :
            !string.IsNullOrWhiteSpace(loopFallback)      ? loopFallback!     :
            WallyLoop.CompletedKeyword;

        /// <summary>
        /// Resolves the effective error keyword, falling back to
        /// <paramref name="loopFallback"/> and then the global default.
        /// </summary>
        public string ResolveErrorKeyword(string? loopFallback) =>
            !string.IsNullOrWhiteSpace(ErrorKeyword)  ? ErrorKeyword! :
            !string.IsNullOrWhiteSpace(loopFallback)  ? loopFallback! :
            WallyLoop.ErrorKeyword;

        // ??? Prompt builders ??????????????????????????????????????????????????

        private static readonly string DefaultStepContinueTemplate =
            "You are continuing a task as part of an iterative loop. " +
            "Here is your previous response for this step:\n\n" +
            "---\n{previousResult}\n---\n\n" +
            "Continue where you left off. " +
            "If you are finished with this step, respond with: {completedKeyword}\n" +
            "If something went wrong, respond with: {errorKeyword}";

        /// <summary>
        /// Builds the initial prompt for this step.
        /// </summary>
        /// <param name="userPrompt">The original runtime user prompt.</param>
        /// <param name="previousStepResult">
        /// The result produced by the immediately preceding step in this pass,
        /// or <see langword="null"/> when this is the first step.
        /// </param>
        /// <param name="resolvedCompletedKeyword">Effective completed keyword.</param>
        /// <param name="resolvedErrorKeyword">Effective error keyword.</param>
        public string BuildStartPrompt(
            string userPrompt,
            string? previousStepResult,
            string resolvedCompletedKeyword,
            string resolvedErrorKeyword)
        {
            if (string.IsNullOrWhiteSpace(PromptTemplate))
            {
                // Default: pass the previous step result, or the user prompt for the first step.
                return previousStepResult != null
                    ? $"Here is the output from the previous step:\n\n---\n{previousStepResult}\n---\n\nUser request: {userPrompt}"
                    : userPrompt;
            }

            return PromptTemplate
                .Replace("{userPrompt}", userPrompt)
                .Replace("{previousStepResult}", previousStepResult ?? string.Empty)
                .Replace("{completedKeyword}", resolvedCompletedKeyword)
                .Replace("{errorKeyword}", resolvedErrorKeyword);
        }

        /// <summary>
        /// Builds the continuation prompt for a subsequent iteration of this step.
        /// </summary>
        /// <param name="previousResult">The result from the previous iteration of this step.</param>
        /// <param name="userPrompt">The original runtime user prompt.</param>
        /// <param name="previousStepResult">The result from the preceding step in this pass.</param>
        /// <param name="resolvedCompletedKeyword">Effective completed keyword.</param>
        /// <param name="resolvedErrorKeyword">Effective error keyword.</param>
        public string BuildContinuePrompt(
            string previousResult,
            string userPrompt,
            string? previousStepResult,
            string resolvedCompletedKeyword,
            string resolvedErrorKeyword)
        {
            string template = !string.IsNullOrWhiteSpace(ContinuePromptTemplate)
                ? ContinuePromptTemplate!
                : DefaultStepContinueTemplate;

            return template
                .Replace("{previousResult}", previousResult)
                .Replace("{userPrompt}", userPrompt)
                .Replace("{previousStepResult}", previousStepResult ?? string.Empty)
                .Replace("{completedKeyword}", resolvedCompletedKeyword)
                .Replace("{errorKeyword}", resolvedErrorKeyword);
        }
    }
}
