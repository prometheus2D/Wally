using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Wally.Core
{
    /// <summary>
    /// Holds configuration for a Wally workspace folder.
    ///
    /// The workspace lives inside the <b>WorkSource</b> directory — the root of
    /// the user's codebase.  The <c>.wally/</c> folder sits at the top level of
    /// the WorkSource alongside the <see cref="ActorsFolderName"/> subfolder:
    /// <code>
    ///   &lt;WorkSource&gt;/               e.g. C:\repos\MyApp
    ///       .wally/                     workspace folder
    ///           wally-config.json
    ///           Actors/
    ///               Developer/
    ///                   actor.json
    ///               Tester/
    ///                   actor.json
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

        /// <summary>
        /// Subfolder inside the workspace folder that holds session log directories.
        /// Each session creates a timestamped subfolder (e.g. <c>2025-07-13_143022_a1b2c3d4</c>).
        /// Default: <c>Logs</c>.
        /// </summary>
        public string LogsFolderName { get; set; } = "Logs";

        /// <summary>
        /// Subfolder inside the workspace folder that holds workspace-level
        /// documentation files (e.g. <c>.md</c>, <c>.txt</c>).
        /// These documents are injected into <em>every</em> actor's prompt as
        /// shared context. Default: <c>Docs</c>.
        /// </summary>
        public string DocsFolderName { get; set; } = "Docs";

        /// <summary>
        /// How often (in minutes) the session logger rotates to a new log file.
        /// Each file covers roughly this time window. Set to <c>0</c> to disable
        /// rotation (single file per session). Default: <c>2</c>.
        /// </summary>
        public int LogRotationMinutes { get; set; } = 2;

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
        /// Run <c>gh copilot -- --help</c> and check the <c>--model</c> choices
        /// to see the full set available to your account.
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
