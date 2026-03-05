using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// Holds configuration for a Wally workspace folder.
    ///
    /// The workspace lives inside the <b>WorkSource</b> directory — the root of
    /// the user's codebase.  The <c>.wally/</c> folder sits at the top level of
    /// the WorkSource:
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
    ///           Loops/                  loop definitions (JSON)
    ///           Wrappers/              LLM wrapper definitions (JSON)
    ///           Logs/                   session logs
    /// </code>
    /// All files under WorkSource (including <c>.wally/</c>) are accessible to
    /// the LLM wrapper. Documentation files are listed in the enriched prompt
    /// so the LLM knows they exist.
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
        /// These files are accessible to the LLM wrapper (e.g. via
        /// <c>--add-dir</c> for Copilot). Doc file names are listed in the
        /// enriched prompt so the LLM knows they exist and can consult them
        /// when relevant to the task.
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
        /// Subfolder inside the workspace folder that holds LLM wrapper
        /// definition JSON files. Each <c>.json</c> file defines a complete
        /// CLI recipe for calling an LLM backend (executable, argument template,
        /// placeholders, and behavioural flags).
        /// Default: <c>Wrappers</c>.
        /// </summary>
        public string WrappersFolderName { get; set; } = "Wrappers";

        /// <summary>
        /// Subfolder inside the workspace folder that holds runbook files (<c>.wrb</c>).
        /// Each file defines a reusable sequence of Wally commands.
        /// Default: <c>Runbooks</c>.
        /// </summary>
        public string RunbooksFolderName { get; set; } = "Runbooks";

        /// <summary>
        /// How often (in minutes) the session logger rotates to a new log file.
        /// Each file covers roughly this time window. Set to <c>0</c> to disable
        /// rotation (single file per session). Default: <c>2</c>.
        /// </summary>
        public int LogRotationMinutes { get; set; } = 2;

        // — Default options (available) ———————————————————————————————————

        /// <summary>
        /// Curated list of model identifiers available in this workspace.
        /// Used to populate UI dropdowns and validate <c>-m</c> arguments.
        /// If a name doesn't match anything the LLM backend recognises, it is
        /// silently skipped. Edit this list to control which models appear
        /// in the model selector.
        /// </summary>
        public List<string> DefaultModels { get; set; } = new();

        /// <summary>
        /// Curated list of LLM wrapper names available in this workspace.
        /// Used to populate UI dropdowns and validate <c>-w</c> arguments.
        /// Each name must match a <c>.json</c> file in <c>Wrappers/</c>
        /// (case-insensitive). Names that don't resolve to a loaded wrapper
        /// are silently skipped.
        /// </summary>
        public List<string> DefaultWrappers { get; set; } = new();

        /// <summary>
        /// Curated list of loop names available in this workspace.
        /// Used to populate UI dropdowns and validate <c>-l</c> arguments.
        /// Each name must match a <c>.json</c> file in <c>Loops/</c>
        /// (case-insensitive). Names that don't resolve to a loaded loop
        /// are silently skipped.
        /// </summary>
        public List<string> DefaultLoops { get; set; } = new();

        /// <summary>
        /// Curated list of runbook names available in this workspace.
        /// Used to populate UI dropdowns and validate <c>runbook</c> arguments.
        /// Each name must match a <c>.wrb</c> file in <c>Runbooks/</c>
        /// (case-insensitive, without extension).
        /// </summary>
        public List<string> DefaultRunbooks { get; set; } = new();

        // — Default selected (priority order) —————————————————————————————

        /// <summary>
        /// Priority-ordered list of preferred model identifiers.
        /// At load time the first entry that appears in <see cref="DefaultModels"/>
        /// becomes <see cref="DefaultModel"/>. This separates "what's in the
        /// dropdown" from "what's pre-selected."
        /// <para>
        /// Models are string identifiers — there is no disk-load check.
        /// The match is performed against <see cref="DefaultModels"/> so that
        /// removing a model from the available list also removes it as a
        /// candidate for selection.
        /// </para>
        /// </summary>
        public List<string> SelectedModels { get; set; } = new();

        /// <summary>
        /// Priority-ordered list of preferred wrapper names.
        /// At load time the first entry that is actually loaded from disk
        /// becomes <see cref="DefaultWrapper"/>.
        /// </summary>
        public List<string> SelectedWrappers { get; set; } = new();

        /// <summary>
        /// Priority-ordered list of preferred loop names.
        /// At load time the first entry that is actually loaded from disk
        /// is used as the default loop selection in the UI.
        /// </summary>
        public List<string> SelectedLoops { get; set; } = new();

        /// <summary>
        /// Priority-ordered list of preferred runbook names.
        /// At load time the first entry that is actually loaded from disk
        /// is used as the default runbook selection in the UI.
        /// </summary>
        public List<string> SelectedRunbooks { get; set; } = new();

        // — Runtime settings ——————————————————————————————————————————————————

        /// <summary>Maximum number of iterations in iterative actor runs.</summary>
        public int MaxIterations { get; set; } = 10;

        // — Factory ———————————————————————————————————————————————————————

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

        // — Resolved defaults (runtime only — not persisted) ——————————————

        /// <summary>Resolved model — first <see cref="SelectedModels"/> entry found in <see cref="DefaultModels"/>.</summary>
        [JsonIgnore]
        public string? DefaultModel { get; private set; }

        /// <summary>Resolved wrapper — first <see cref="SelectedWrappers"/> entry that is loaded from disk.</summary>
        [JsonIgnore]
        public string? DefaultWrapper { get; private set; }

        /// <summary>Resolved loop — first <see cref="SelectedLoops"/> entry that is loaded from disk.</summary>
        [JsonIgnore]
        public string? ResolvedDefaultLoop { get; private set; }

        /// <summary>Resolved runbook — first <see cref="SelectedRunbooks"/> entry that is loaded from disk.</summary>
        [JsonIgnore]
        public string? ResolvedDefaultRunbook { get; private set; }

        // — Resolve selected defaults ————————————————————————————————————

        /// <summary>
        /// Resolves the active default for each category by picking the first
        /// <c>Selected*</c> entry that actually exists (in the available list
        /// for models, or loaded from disk for wrappers/loops/runbooks).
        /// Call after loading all workspace entities.
        /// </summary>
        public void ResolveSelectedDefaults(
            IEnumerable<string> loadedWrapperNames,
            IEnumerable<string> loadedLoopNames,
            IEnumerable<string> loadedRunbookNames)
        {
            DefaultModel = FirstMatch(SelectedModels, DefaultModels);
            DefaultWrapper = FirstMatch(SelectedWrappers, loadedWrapperNames);
            ResolvedDefaultLoop = FirstMatch(SelectedLoops, loadedLoopNames);
            ResolvedDefaultRunbook = FirstMatch(SelectedRunbooks, loadedRunbookNames);
        }

        /// <summary>
        /// Returns the first entry in <paramref name="preferred"/> that exists
        /// in <paramref name="available"/>, or <see langword="null"/> if none match.
        /// </summary>
        private static string? FirstMatch(List<string> preferred, IEnumerable<string> available)
        {
            if (preferred.Count == 0) return null;
            var set = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
            return preferred.FirstOrDefault(p => set.Contains(p));
        }
    }
}
