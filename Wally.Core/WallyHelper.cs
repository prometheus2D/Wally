using System;
using System.Collections.Generic;
using System.IO;
using Wally.Core.Actors;
using Wally.Core.RBA;

namespace Wally.Core
{
    public static class WallyHelper
    {
        // ?? Well-known file names ?????????????????????????????????????????????

        /// <summary>File name for the canonical workspace configuration file.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>Name of the role prompt file inside each agent folder.</summary>
        public const string RoleFileName     = "role.txt";
        /// <summary>Name of the acceptance criteria prompt file inside each agent folder.</summary>
        public const string CriteriaFileName = "criteria.txt";
        /// <summary>Name of the intent prompt file inside each agent folder.</summary>
        public const string IntentFileName   = "intent.txt";

        // ?? Default parent folder ?????????????????????????????????????????????

        /// <summary>
        /// The directory containing the executing assembly — the natural home for a
        /// "drop Wally next to your code" setup.
        /// </summary>
        public static string GetDefaultParentFolder() =>
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();

        // ?? Workspace scaffolding ?????????????????????????????????????????????

        /// <summary>
        /// Scaffolds a complete workspace under <paramref name="parentFolder"/>:
        /// <code>
        ///   &lt;parentFolder&gt;/
        ///       Project/
        ///       .wally/
        ///           wally-config.json
        ///           Agents/
        ///               Developer/
        ///                   role.txt
        ///                   criteria.txt
        ///                   intent.txt
        ///               Tester/
        ///                   role.txt
        ///                   criteria.txt
        ///                   intent.txt
        /// </code>
        /// Default agent folders are copied from the exe directory when present.
        /// Existing files are never overwritten.
        /// </summary>
        public static void CreateDefaultWorkspace(string parentFolder, WallyConfig config = null)
        {
            config ??= ResolveConfig();

            string workspaceFolder = Path.Combine(parentFolder, config.WorkspaceFolderName);
            string projectFolder   = Path.Combine(parentFolder, config.ProjectFolderName);

            Directory.CreateDirectory(workspaceFolder);
            Directory.CreateDirectory(projectFolder);

            // wally-config.json
            string destConfig = Path.Combine(workspaceFolder, ConfigFileName);
            if (!File.Exists(destConfig))
                config.SaveToFile(destConfig);

            // Agents/ — copy the default agent tree from the exe directory
            CopyDefaultDirectory(config.AgentsFolderName, workspaceFolder);
        }

        // ?? Actor loading from folders ????????????????????????????????????????

        /// <summary>
        /// Reads every agent subfolder under <c>&lt;workspaceFolder&gt;/Agents/</c> and
        /// returns one <see cref="CopilotActor"/> per folder.
        ///
        /// Each subfolder must contain at least one of <c>role.txt</c>,
        /// <c>criteria.txt</c>, or <c>intent.txt</c>. Missing files produce empty-prompt
        /// RBA items rather than errors so partially configured agents still load.
        /// </summary>
        public static List<Actor> LoadActors(
            string workspaceFolder, WallyConfig config, WallyWorkspace workspace = null)
        {
            var actors = new List<Actor>();
            string agentsDir = Path.Combine(workspaceFolder, config.AgentsFolderName);
            if (!Directory.Exists(agentsDir)) return actors;

            foreach (string agentDir in Directory.GetDirectories(agentsDir))
            {
                string name = Path.GetFileName(agentDir);

                var (rolePrompt, roleTier)        = ReadPromptFile(agentDir, RoleFileName);
                var (criteriaPrompt, criteriaTier) = ReadPromptFile(agentDir, CriteriaFileName);
                var (intentPrompt, intentTier)     = ReadPromptFile(agentDir, IntentFileName);

                actors.Add(new CopilotActor(
                    name,
                    agentDir,
                    new Role(name, rolePrompt, roleTier),
                    new AcceptanceCriteria(name, criteriaPrompt, criteriaTier),
                    new Intent(name, intentPrompt, intentTier),
                    workspace));
            }
            return actors;
        }

        /// <summary>
        /// Writes an actor's RBA prompt files back to its folder, creating the folder if needed.
        /// Overwrites existing files.
        /// </summary>
        public static void SaveActor(string workspaceFolder, WallyConfig config, Actor actor)
        {
            string agentDir = Path.Combine(workspaceFolder, config.AgentsFolderName, actor.Name);
            Directory.CreateDirectory(agentDir);

            WritePromptFile(agentDir, RoleFileName,     actor.Role.Prompt,             actor.Role.Tier);
            WritePromptFile(agentDir, CriteriaFileName, actor.AcceptanceCriteria.Prompt, actor.AcceptanceCriteria.Tier);
            WritePromptFile(agentDir, IntentFileName,   actor.Intent.Prompt,           actor.Intent.Tier);
        }

        // ?? Config resolution ?????????????????????????????????????????????????

        public static WallyConfig ResolveConfig()
        {
            string parentFolder               = GetDefaultParentFolder();
            string defaultWorkspaceFolderName = new WallyConfig().WorkspaceFolderName;

            string subFolderConfig = Path.Combine(
                parentFolder, defaultWorkspaceFolderName, ConfigFileName);

            return File.Exists(subFolderConfig)
                ? WallyConfig.LoadFromFile(subFolderConfig)
                : new WallyConfig();
        }

        public static string? ResolveDefaultAgentsPath(string workspaceFolder)
        {
            // Kept for backward compatibility; always returns null now that
            // agents are defined by folders rather than a JSON file.
            return null;
        }

        // ?? Default environment loading ???????????????????????????????????????

        public static WallyEnvironment LoadDefault()
        {
            var env = new WallyEnvironment();
            env.SetupLocal();
            return env;
        }

        // ?? Directory utilities ???????????????????????????????????????????????

        /// <summary>Recursively copies <paramref name="sourceDir"/> into <paramref name="destDir"/>.</summary>
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        /// <summary>
        /// Copies a subdirectory tree from the exe directory into <paramref name="destFolder"/>.
        /// Existing destination files are not overwritten.
        /// </summary>
        private static void CopyDefaultDirectory(string subDirName, string destFolder)
        {
            string srcDir  = Path.Combine(GetDefaultParentFolder(), subDirName);
            string destDir = Path.Combine(destFolder, subDirName);
            if (!Directory.Exists(srcDir)) return;

            Directory.CreateDirectory(destDir);
            foreach (string srcSubDir in Directory.GetDirectories(srcDir))
            {
                string agentDest = Path.Combine(destDir, Path.GetFileName(srcSubDir));
                Directory.CreateDirectory(agentDest);
                foreach (string srcFile in Directory.GetFiles(srcSubDir))
                {
                    string destFile = Path.Combine(agentDest, Path.GetFileName(srcFile));
                    if (!File.Exists(destFile))
                        File.Copy(srcFile, destFile);
                }
            }
        }

        /// <summary>
        /// Reads a prompt file, parsing an optional first-line metadata header
        /// <c># Tier: value</c>. Returns the prompt body and the tier (or null).
        /// Returns empty strings when the file does not exist.
        /// </summary>
        private static (string Prompt, string? Tier) ReadPromptFile(
            string agentDir, string fileName)
        {
            string path = Path.Combine(agentDir, fileName);
            if (!File.Exists(path)) return (string.Empty, null);

            string[] lines = File.ReadAllLines(path);
            string? tier   = null;
            int bodyStart  = 0;

            // Parse leading "# Key: Value" metadata lines
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith('#')) break;

                int colon = line.IndexOf(':');
                if (colon > 0)
                {
                    string key = line[1..colon].Trim().ToLowerInvariant();
                    string val = line[(colon + 1)..].Trim();
                    if (key == "tier") tier = val;
                }
                bodyStart = i + 1;
            }

            string prompt = string.Join(Environment.NewLine,
                lines[bodyStart..]).Trim();

            return (prompt, tier);
        }

        /// <summary>
        /// Writes a prompt file, prepending a <c># Tier: value</c> header line when
        /// <paramref name="tier"/> is non-null.
        /// </summary>
        private static void WritePromptFile(
            string agentDir, string fileName, string prompt, string? tier)
        {
            string path    = Path.Combine(agentDir, fileName);
            string content = string.IsNullOrWhiteSpace(tier)
                ? prompt ?? string.Empty
                : $"# Tier: {tier}{Environment.NewLine}{prompt ?? string.Empty}";
            File.WriteAllText(path, content);
        }
    }
}
