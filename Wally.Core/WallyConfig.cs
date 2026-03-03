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
    ///           Docs/                   workspace-level documentation
    ///           Templates/              document templates
    ///           Actors/
    ///               &lt;ActorName&gt;/
    ///                   actor.json
    ///                   Docs/           actor-private documentation
    /// </code>
    /// All files under WorkSource (including <c>.wally/</c>) are accessible to
    /// <c>gh copilot</c> via <c>--add-dir</c>. Documentation files are never
    /// injected into prompts — Copilot reads them from disk natively.
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
        /// <para>
        /// These files are accessible to <c>gh copilot</c> via <c>--add-dir</c>
        /// (the entire WorkSource tree is granted). They are <b>not</b> injected
        /// into prompts — Copilot reads them from disk when relevant, or the
        /// user can reference specific files by path in the prompt.
        /// </para>
        /// Default: <c>Docs</c>.
        /// </summary>
        public string DocsFolderName { get; set; } = "Docs";

        /// <summary>
        /// Subfolder inside the workspace folder that holds document templates
        /// (e.g. <c>ProposalTemplate.md</c>, <c>RequirementsTemplate.md</c>).
        /// Actors reference these templates when producing structured documents.
        /// Default: <c>Templates</c>.
        /// </summary>
        public string TemplatesFolderName { get; set; } = "Templates";

        /// <summary>
        /// Subfolder inside the workspace folder that holds loop definition
        /// JSON files. Each <c>.json</c> file defines a reusable
        /// <see cref="WallyLoop"/> with its actor, prompts, and stop conditions.
        /// Default: <c>Loops</c>.
        /// </summary>
        public string LoopsFolderName { get; set; } = "Loops";

        /// <summary>
        /// Subfolder inside the workspace folder that holds LLM provider
        /// definition JSON files. Each <c>.json</c> file maps a logical
        /// provider name to a concrete provider type.
        /// Default: <c>Providers</c>.
        /// </summary>
        public string ProvidersFolderName { get; set; } = "Providers";

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

        /// <summary>
        /// The name of the LLM provider to use when running actors.
        /// Must match a known provider name (case-insensitive):
        /// <list type="bullet">
        ///   <item><c>"Copilot"</c> — read-only, runs <c>gh copilot -p</c> (default).</item>
        ///   <item><c>"AutoCopilot"</c> — agentic, runs <c>gh copilot -i --yolo</c> (can edit files).</item>
        /// </list>
        /// </summary>
        public string DefaultProvider { get; set; } = "Copilot";

        // — Factory ———————————————————————————————————————————————————————————

        /// <summary>Deserializes a <see cref="WallyConfig"/> from a JSON file.</summary>
        public static WallyConfig LoadFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<WallyConfig>(json) ?? new WallyConfig();
            }
            catch (JsonException ex)
            {
                System.Console.Error.WriteLine(
                    $"Warning: Failed to parse '{filePath}': {ex.Message}. Using default config.");
                return new WallyConfig();
            }
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
