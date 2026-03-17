using System;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// Defines a single step within a <see cref="WallyLoopDefinition"/>.
    /// <para>
    /// Each step names an actor and supplies a <see cref="PromptTemplate"/> that is
    /// resolved at runtime. The prompt is sent to the actor once; the response is
    /// threaded to the next step via <c>{previousStepResult}</c>.
    /// </para>
    /// <para>Prompt template placeholders:</para>
    /// <list type="bullet">
    ///   <item><c>{userPrompt}</c> — the original user prompt.</item>
    ///   <item><c>{previousStepResult}</c> — the output of the preceding step (empty for the first step).</item>
    /// </list>
    /// </summary>
    public class WallyStepDefinition
    {
        /// <summary>Short display name for this step (e.g. <c>"TechnicalReview"</c>).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional human-readable description shown in <c>list-loops</c>.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The actor to use for this step. When empty the step runs in direct mode
        /// (no actor enrichment), unless the parent loop's <see cref="WallyLoopDefinition.ActorName"/>
        /// provides a fallback.
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// The prompt sent to the AI. When empty, the user prompt is used for the
        /// first step; subsequent steps receive the previous step's output.
        /// <para>Supports: <c>{userPrompt}</c>, <c>{previousStepResult}</c>.</para>
        /// </summary>
        public string PromptTemplate { get; set; } = string.Empty;

        /// <summary>Builds the resolved prompt for this step.</summary>
        public string BuildPrompt(string userPrompt, string? previousStepResult)
        {
            if (string.IsNullOrWhiteSpace(PromptTemplate))
                return previousStepResult != null
                    ? $"Here is the output from the previous step:\n\n---\n{previousStepResult}\n---\n\nUser request: {userPrompt}"
                    : userPrompt;

            return PromptTemplate
                .Replace("{userPrompt}",         userPrompt)
                .Replace("{previousStepResult}", previousStepResult ?? string.Empty);
        }
    }
}
