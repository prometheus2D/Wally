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
    /// RBA definitions are no longer stored here — they are loaded at runtime from each
    /// agent's subfolder under <see cref="AgentsFolderName"/>:
    /// <code>
    ///   .wally/
    ///       Agents/
    ///           Developer/
    ///               role.txt
    ///               criteria.txt
    ///               intent.txt
    ///           Tester/
    ///               role.txt
    ///               criteria.txt
    ///               intent.txt
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
        /// Subfolder inside the workspace folder that holds one directory per agent.
        /// Each agent directory contains <c>role.txt</c>, <c>criteria.txt</c>, and
        /// <c>intent.txt</c>. Default: <c>Agents</c>.
        /// </summary>
        public string AgentsFolderName { get; set; } = "Agents";

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
