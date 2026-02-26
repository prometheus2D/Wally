using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wally.Core.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Holds all configuration for a Wally environment.
    /// Defines folder names, actor RBA definitions, and runtime settings.
    /// </summary>
    public class WallyConfig
    {
        // ?? Folder names ??????????????????????????????????????????????????????

        /// <summary>
        /// Name of the workspace subfolder (sibling of <see cref="ProjectFolderName"/>)
        /// where Wally's config file lives. Defaults to <c>.wally</c>.
        /// </summary>
        public string WorkspaceFolderName { get; set; } = ".wally";

        /// <summary>
        /// Name of the project subfolder (sibling of <see cref="WorkspaceFolderName"/>)
        /// that contains the codebase Wally operates on. Defaults to <c>Project</c>.
        /// </summary>
        public string ProjectFolderName   { get; set; } = "Project";

        /// <summary>Subfolder inside the workspace folder that holds per-role prompt files.</summary>
        public string RolesFolderName    { get; set; } = "Roles";

        /// <summary>Subfolder inside the workspace folder that holds per-criteria prompt files.</summary>
        public string CriteriaFolderName { get; set; } = "Criteria";

        /// <summary>Subfolder inside the workspace folder that holds per-intent prompt files.</summary>
        public string IntentsFolderName  { get; set; } = "Intents";

        // ?? Runtime settings ??????????????????????????????????????????????????

        /// <summary>Maximum number of iterations when running actors in iterative mode.</summary>
        public int MaxIterations { get; set; } = 10;

        // ?? Actor RBA definitions (runtime-only — loaded from .txt files) ?????

        /// <summary>
        /// Populated at runtime from the <see cref="RolesFolderName"/> prompt files.
        /// Not serialised to <c>wally-config.json</c> — the <c>.txt</c> files are the
        /// source of truth.
        /// </summary>
        [JsonIgnore]
        public List<Role> Roles { get; set; } = new List<Role>();

        /// <summary>
        /// Populated at runtime from the <see cref="CriteriaFolderName"/> prompt files.
        /// Not serialised to <c>wally-config.json</c>.
        /// </summary>
        [JsonIgnore]
        public List<AcceptanceCriteria> AcceptanceCriterias { get; set; } = new List<AcceptanceCriteria>();

        /// <summary>
        /// Populated at runtime from the <see cref="IntentsFolderName"/> prompt files.
        /// Not serialised to <c>wally-config.json</c>.
        /// </summary>
        [JsonIgnore]
        public List<Intent> Intents { get; set; } = new List<Intent>();

        // ?? Factory ???????????????????????????????????????????????????????????

        /// <summary>Deserializes a <see cref="WallyConfig"/> from a JSON file.</summary>
        public static WallyConfig LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WallyConfig>(json) ?? new WallyConfig();
        }

        /// <summary>Serializes this config to a JSON file.</summary>
        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
