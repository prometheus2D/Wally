using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// Declares one named document or state input that a step expects the runtime
    /// to load before prompt expansion or executable-step handling.
    /// </summary>
    public class WallyDocumentInputDefinition
    {
        /// <summary>The placeholder key referenced by the step template.</summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>Workspace-relative path to the backing document or state file.</summary>
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="true"/>, a missing file should fail step execution
        /// instead of injecting empty content.
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }
    }

    /// <summary>
    /// Defines a single step within a <see cref="WallyLoopDefinition"/>.
    /// <para>
    /// A step may be a prompt step, shell step, command step, code-backed step,
    /// or user-input step. The step owns its prompt template, document inputs,
    /// handler metadata, and keyword routing fields.
    /// </para>
    /// <para>Prompt template placeholders:</para>
    /// <list type="bullet">
    ///   <item><c>{userPrompt}</c> � the original user prompt.</item>
    ///   <item><c>{previousStepResult}</c> � the output of the preceding step (empty for the first step).</item>
    /// </list>
    /// </summary>
    public class WallyStepDefinition
    {
        /// <summary>Short display name for this step (e.g. <c>"TechnicalReview"</c>).</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional human-readable description shown in <c>list-loops</c>.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Declares how the runtime executes this step.
        /// Supported v1 kinds are <c>prompt</c>, <c>shell</c>, <c>command</c>,
        /// <c>code</c>, and <c>user_input</c>.
        /// </summary>
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "prompt";

        /// <summary>
        /// The actor to use for this step. When empty the step runs in direct mode
        /// (no actor enrichment), unless the parent loop's <see cref="WallyLoopDefinition.ActorName"/>
        /// provides a fallback.
        /// </summary>
        [JsonPropertyName("actorName")]
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// Optional ordered list of reusable ability names that augment this step's
        /// local configuration without replacing it.
        /// </summary>
        [JsonPropertyName("abilityRefs")]
        public List<string> AbilityRefs { get; set; } = new();

        /// <summary>
        /// The prompt sent to the AI. When empty, the user prompt is used for the
        /// first step; subsequent steps receive the previous step's output.
        /// <para>Supports: <c>{userPrompt}</c>, <c>{previousStepResult}</c>.</para>
        /// </summary>
        [JsonPropertyName("promptTemplate")]
        public string PromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Named document or state inputs required by the step prompt or handler.
        /// The runtime should only load the inputs declared here for step-owned
        /// prompt assembly and executable-step state reconstruction.
        /// </summary>
        [JsonPropertyName("documentInputs")]
        public List<WallyDocumentInputDefinition> DocumentInputs { get; set; } = new();

        /// <summary>
        /// Template used by <c>shell</c> or <c>command</c> steps to describe what
        /// should be executed.
        /// </summary>
        [JsonPropertyName("commandTemplate")]
        public string CommandTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Built-in handler name for <c>code</c> steps.
        /// </summary>
        [JsonPropertyName("handlerName")]
        public string HandlerName { get; set; } = string.Empty;

        /// <summary>
        /// Named arguments supplied to command or code-backed step handlers.
        /// </summary>
        [JsonPropertyName("arguments")]
        public Dictionary<string, string> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// When <see langword="true"/>, the loop may continue after a failure from
        /// this step according to the owning workflow's policy.
        /// </summary>
        [JsonPropertyName("continueOnFailure")]
        public bool ContinueOnFailure { get; set; }

        /// <summary>
        /// Declares which documents or state files this step is expected to update.
        /// Values are workspace-relative output paths or glob-like patterns used
        /// to constrain mutating step behavior to declared workflow artifacts.
        /// </summary>
        [JsonPropertyName("writesToDocs")]
        public List<string> WritesToDocs { get; set; } = new();

        /// <summary>
        /// Maps routing keywords returned by this step to the next named step.
        /// </summary>
        [JsonPropertyName("keywordRoutes")]
        public Dictionary<string, string> KeywordRoutes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fallback step to use when no routing keyword matches.
        /// </summary>
        [JsonPropertyName("defaultNextStep")]
        public string DefaultNextStep { get; set; } = string.Empty;

        /// <summary>Returns the effective execution kind for this step.</summary>
        [JsonIgnore]
        public string EffectiveKind => string.IsNullOrWhiteSpace(Kind) ? "prompt" : Kind;

        /// <summary>Returns <see langword="true"/> when the step declares routing metadata.</summary>
        [JsonIgnore]
        public bool HasRouting => KeywordRoutes.Count > 0 || !string.IsNullOrWhiteSpace(DefaultNextStep);

        /// <summary>Returns <see langword="true"/> when the step declares any write targets.</summary>
        [JsonIgnore]
        public bool HasDeclaredWriteScope => WritesToDocs.Count > 0;

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
