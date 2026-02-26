using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wally.Core.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Holds all configuration for a Wally environment.
    /// Defines folder names and runtime settings.
    ///
    /// Actor definitions are loaded at runtime from each actor's subfolder under
    /// <see cref="ActorsFolderName"/>. Each subfolder contains a single
    /// <c>actor.json</c> file with the full RBA definition:
    /// <code>
    ///   .wally/
    ///       Actors/
    ///           Developer/
    ///               actor.json
    ///           Tester/
    ///               actor.json
    /// </code>
    /// </summary>
    public class WallyConfig
    {
        // ?? Folder names ??????????????????????????????????????????????????????

        /// <summary>Workspace subfolder name. Default: <c>.wally</c>.</summary>
        public string WorkspaceFolderName { get; set; } = ".wally";

        /// <summary>Project (codebase) subfolder name. Default: <c>Project</c>.</summary>
        public string ProjectFolderName { get; set; } = "Project";

        /// <summary>
        /// Subfolder inside the workspace folder that holds one directory per actor.
        /// Each actor directory contains a single <c>actor.json</c> file with the
        /// full RBA (Role / AcceptanceCriteria / Intent) definition.
        /// Default: <c>Actors</c>.
        /// </summary>
        public string ActorsFolderName { get; set; } = "Actors";

        // ?? Runtime settings ??????????????????????????????????????????????????

        /// <summary>Maximum number of iterations in iterative actor runs.</summary>
        public int MaxIterations { get; set; } = 10;

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
