using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// A serializable definition for a Wally loop, loaded from a JSON file.
    /// <para>
    /// Each <c>.json</c> file in the workspace's <c>Loops/</c> folder defines one loop.
    /// A loop is either a single-actor call, an ordered pipeline of steps, or an
    /// agent loop that iterates until a stop condition is met.
    /// </para>
    /// <para>
    /// Single-actor shorthand: omit <see cref="Steps"/> and set <see cref="ActorName"/> and
    /// <see cref="StartPrompt"/> directly.
    /// </para>
    /// <code>
    /// // Multi-step pipeline:
    /// {
    ///   "name": "CodeReview",
    ///   "steps": [
    ///     { "name": "TechnicalReview", "actorName": "Engineer",        "promptTemplate": "Review:\n\n{userPrompt}" },
    ///     { "name": "BusinessTriage",  "actorName": "BusinessAnalyst", "promptTemplate": "Triage:\n\n{previousStepResult}\n\nRequest: {userPrompt}" }
    ///   ]
    /// }
    ///
    /// // Single-actor shorthand:
    /// { "name": "QuickReview", "actorName": "Engineer", "startPrompt": "{userPrompt}" }
    /// </code>
    /// </summary>
    public class WallyLoopDefinition
    {
        // ?? Identity ??????????????????????????????????????????????????????????

        /// <summary>Unique name used to select this pipeline from the CLI or UI.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable description shown in <c>list-loops</c>.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="false"/> this loop definition is skipped during
        /// workspace load and will not appear in any dropdown or be selectable
        /// by name. Defaults to <see langword="true"/> so existing JSON files
        /// without the field continue to load normally.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        // ?? Agent loop configuration ??????????????????????????????????????????

        /// <summary>
        /// Maximum number of iterations for agent loop mode.
        /// When set to a value &gt; 0 (and <see cref="HasSteps"/> is false),
        /// the loop runs as a self-driving agent loop instead of a single-shot call.
        /// Default is 0, meaning single-shot / pipeline mode (no iteration).
        /// </summary>
        [JsonPropertyName("maxIterations")]
        public int MaxIterations { get; set; }

        /// <summary>
        /// When the LLM response contains this keyword (case-insensitive),
        /// the agent loop stops immediately. Checked before action dispatch
        /// and iteration-count limits.
        /// </summary>
        [JsonPropertyName("stopKeyword")]
        public string StopKeyword { get; set; } = string.Empty;

        /// <summary>
        /// Controls how the previous response is fed back into the next iteration's prompt.
        /// <list type="bullet">
        ///   <item><c>"AppendResponse"</c> (default) � appends the response to the original prompt.</item>
        ///   <item><c>"ReplacePrompt"</c> � uses the response as the next iteration's prompt, discarding the original.</item>
        /// </list>
        /// </summary>
        [JsonPropertyName("feedbackMode")]
        public string FeedbackMode { get; set; } = "AppendResponse";

        /// <summary>
        /// Returns <see langword="true"/> when this definition is configured as an
        /// agent loop (has <see cref="MaxIterations"/> &gt; 0 and no explicit steps).
        /// </summary>
        [JsonIgnore]
        public bool IsAgentLoop => MaxIterations > 0 && !HasSteps;

        // ?? Single-actor shorthand ????????????????????????????????????????????

        /// <summary>
        /// Actor for single-actor runs (no <see cref="Steps"/>).
        /// Also used as fallback for any step that omits its own <c>ActorName</c>.
        /// </summary>
        [JsonPropertyName("actorName")]
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// Prompt for single-actor runs. Supports <c>{userPrompt}</c>.
        /// Ignored when <see cref="Steps"/> is non-empty.
        /// </summary>
        [JsonPropertyName("startPrompt")]
        public string StartPrompt { get; set; } = string.Empty;

        /// <summary>
        /// The first named step to execute for dynamic step-routing workflows.
        /// When empty, loops with explicit steps retain legacy ordered pipeline
        /// semantics.
        /// </summary>
        [JsonPropertyName("startStepName")]
        public string StartStepName { get; set; } = string.Empty;

        // ?? Steps ????????????????????????????????????????????????????????????

        /// <summary>
        /// Ordered steps. When non-empty the pipeline executes each in sequence,
        /// threading the previous step's output into the next prompt.
        /// </summary>
        [JsonPropertyName("steps")]
        public List<WallyStepDefinition> Steps { get; set; } = new();

        /// <summary>Returns <see langword="true"/> when this definition uses explicit steps.</summary>
        [JsonIgnore]
        public bool HasSteps => Steps != null && Steps.Count > 0;

        /// <summary>
        /// Returns <see langword="true"/> when this definition uses named-step
        /// routing metadata instead of pure ordered pipeline semantics.
        /// </summary>
        [JsonIgnore]
        public bool UsesNamedStepRouting => HasSteps && !string.IsNullOrWhiteSpace(StartStepName);

        /// <summary>
        /// Returns <see langword="true"/> when the loop omits actor fallback and is
        /// therefore configured for actor-agnostic direct prompt execution.
        /// </summary>
        [JsonIgnore]
        public bool IsActorAgnostic => string.IsNullOrWhiteSpace(ActorName);

        /// <summary>Finds a step by name using case-insensitive matching.</summary>
        public WallyStepDefinition? FindStep(string? stepName)
        {
            if (string.IsNullOrWhiteSpace(stepName) || !HasSteps)
                return null;

            return Steps.Find(step =>
                string.Equals(step.Name, stepName, StringComparison.OrdinalIgnoreCase));
        }

        // ?? Serialization ?????????????????????????????????????????????????????

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
                    if (!def.Enabled) continue;
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
