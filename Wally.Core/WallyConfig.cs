using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Wally.Core
{
    /// <summary>
    /// Holds configuration for a Wally workspace folder.
    ///
    /// The workspace folder IS the root — <c>wally-config.json</c> sits at its top level
    /// alongside the <see cref="ActorsFolderName"/> subfolder:
    /// <code>
    ///   &lt;WorkspaceFolder&gt;/
    ///       wally-config.json
    ///       Actors/
    ///           Developer/
    ///               actor.json
    ///           Tester/
    ///               actor.json
    /// </code>
    /// </summary>
    public class WallyConfig
    {
        // — Folder names ——————————————————————————————————————————————————————

        /// <summary>
        /// Subfolder inside the workspace folder that holds one directory per actor.
        /// Each actor directory contains a single <c>actor.json</c> file.
        /// Default: <c>Actors</c>.
        /// </summary>
        public string ActorsFolderName { get; set; } = "Actors";

        // — Source path ———————————————————————————————————————————————————————

        /// <summary>
        /// The directory whose files and subdirectories provide context to the
        /// <c>gh copilot</c> command.  When set, <see cref="Actors.CopilotActor"/>
        /// uses this as the <c>WorkingDirectory</c> of the spawned process so that
        /// Copilot CLI can see the target codebase regardless of where Wally was
        /// launched from.
        /// <para>
        /// When <see langword="null"/> or empty the current working directory of the
        /// Wally process is used (previous default behaviour).
        /// </para>
        /// </summary>
        public string? SourcePath { get; set; }

        // — Model selection ———————————————————————————————————————————————————

        /// <summary>
        /// The model identifier passed to <c>gh copilot --model</c> when running actors.
        /// When <see langword="null"/> or empty, the Copilot CLI default model is used.
        /// Must be one of the values in <see cref="Models"/> (when that list is non-empty).
        /// </summary>
        public string? DefaultModel { get; set; }

        /// <summary>
        /// The set of model identifiers that this workspace is allowed to use.
        /// Serves as a reference list — edit it to track which models are available
        /// or permitted in your environment.
        /// <para>
        /// Common values: <c>"gpt-4o"</c>, <c>"gpt-4.1"</c>, <c>"claude-3.5-sonnet"</c>,
        /// <c>"o4-mini"</c>, <c>"gemini-2.0-flash-001"</c>.
        /// Run <c>gh copilot model list</c> to see the full set available to your account.
        /// </para>
        /// </summary>
        public List<string> Models { get; set; } = new();

        // — Runtime settings ——————————————————————————————————————————————————

        /// <summary>Maximum number of iterations in iterative actor runs.</summary>
        public int MaxIterations { get; set; } = 10;

        // — Factory ———————————————————————————————————————————————————————————

        /// <summary>Deserializes a <see cref="WallyConfig"/> from a JSON file.</summary>
        public static WallyConfig LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WallyConfig>(json) ?? new WallyConfig();
        }

        /// <summary>Serializes this config to a JSON file.</summary>
        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
