using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// A serializable definition for a <see cref="WallyPipeline"/>, loaded from a JSON file.
    /// <para>
    /// Each <c>.json</c> file in the workspace's <c>Loops/</c> folder defines one pipeline.
    /// A pipeline is an ordered list of steps; each step names an actor and a prompt template.
    /// The output of each step is passed to the next step as <c>{previousStepResult}</c>.
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
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable description shown in <c>list-loops</c>.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="false"/> this loop definition is skipped during
        /// workspace load and will not appear in any dropdown or be selectable
        /// by name. Defaults to <see langword="true"/> so existing JSON files
        /// without the field continue to load normally.
        /// </summary>
        public bool Enabled { get; set; } = true;

        // ?? Single-actor shorthand ????????????????????????????????????????????

        /// <summary>
        /// Actor for single-actor runs (no <see cref="Steps"/>).
        /// Also used as fallback for any step that omits its own <c>ActorName</c>.
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// Prompt for single-actor runs. Supports <c>{userPrompt}</c>.
        /// Ignored when <see cref="Steps"/> is non-empty.
        /// </summary>
        public string StartPrompt { get; set; } = string.Empty;

        // ?? Steps ????????????????????????????????????????????????????????????

        /// <summary>
        /// Ordered steps. When non-empty the pipeline executes each in sequence,
        /// threading the previous step's output into the next prompt.
        /// </summary>
        public List<WallyStepDefinition> Steps { get; set; } = new();

        /// <summary>Returns <see langword="true"/> when this definition uses explicit steps.</summary>
        [JsonIgnore]
        public bool HasSteps => Steps != null && Steps.Count > 0;

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
