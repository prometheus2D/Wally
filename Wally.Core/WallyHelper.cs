using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Wally.Core.Actors;
using Wally.Core.RBA;

namespace Wally.Core
{
    public static class WallyHelper
    {
        // ?? Well-known file names ?????????????????????????????????????????????

        /// <summary>File name for the canonical workspace configuration file.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>File name for each actor's definition file inside its actor folder.</summary>
        public const string ActorFileName = "actor.json";

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
        ///           Actors/
        ///               Developer/
        ///                   actor.json
        ///               Tester/
        ///                   actor.json
        /// </code>
        /// Default actor folders are copied from the exe directory when present.
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

            // Actors/ — copy the default actor tree from the exe directory
            CopyDefaultDirectory(config.ActorsFolderName, workspaceFolder);
        }

        // ?? Actor loading from folders ????????????????????????????????????????

        /// <summary>
        /// Reads every actor subfolder under <c>&lt;workspaceFolder&gt;/Actors/</c> and
        /// returns one <see cref="CopilotActor"/> per folder.
        ///
        /// Each subfolder must contain an <c>actor.json</c> file. Missing files produce
        /// empty-prompt RBA items rather than errors so partially configured actors still load.
        /// </summary>
        public static List<Actor> LoadActors(
            string workspaceFolder, WallyConfig config, WallyWorkspace workspace = null)
        {
            var actors    = new List<Actor>();
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (!Directory.Exists(actorsDir)) return actors;

            foreach (string actorDir in Directory.GetDirectories(actorsDir))
            {
                string folderName = Path.GetFileName(actorDir);
                string jsonPath   = Path.Combine(actorDir, ActorFileName);

                string name           = folderName;
                string rolePrompt     = string.Empty;
                string? roleTier      = null;
                string criteriaPrompt = string.Empty;
                string? criteriaTier  = null;
                string intentPrompt   = string.Empty;
                string? intentTier    = null;

                if (File.Exists(jsonPath))
                {
                    var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                    var root = doc.RootElement;

                    name           = TryGetString(root, "name") ?? folderName;
                    rolePrompt     = TryGetString(root, "rolePrompt")     ?? string.Empty;
                    roleTier       = TryGetString(root, "roleTier");
                    criteriaPrompt = TryGetString(root, "criteriaPrompt") ?? string.Empty;
                    criteriaTier   = TryGetString(root, "criteriaTier");
                    intentPrompt   = TryGetString(root, "intentPrompt")   ?? string.Empty;
                    intentTier     = TryGetString(root, "intentTier");
                }

                actors.Add(new CopilotActor(
                    name,
                    actorDir,
                    new Role(name, rolePrompt, roleTier),
                    new AcceptanceCriteria(name, criteriaPrompt, criteriaTier),
                    new Intent(name, intentPrompt, intentTier),
                    workspace));
            }
            return actors;
        }

        /// <summary>
        /// Writes an actor's definition back to its <c>actor.json</c>, creating the folder
        /// if needed. Overwrites any existing file.
        /// </summary>
        public static void SaveActor(string workspaceFolder, WallyConfig config, Actor actor)
        {
            string actorDir = Path.Combine(workspaceFolder, config.ActorsFolderName, actor.Name);
            Directory.CreateDirectory(actorDir);

            var obj = new
            {
                name           = actor.Name,
                rolePrompt     = actor.Role.Prompt,
                roleTier       = actor.Role.Tier,
                criteriaPrompt = actor.AcceptanceCriteria.Prompt,
                criteriaTier   = actor.AcceptanceCriteria.Tier,
                intentPrompt   = actor.Intent.Prompt,
                intentTier     = actor.Intent.Tier
            };

            string json = JsonSerializer.Serialize(obj,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(actorDir, ActorFileName), json);
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
                string actorDest = Path.Combine(destDir, Path.GetFileName(srcSubDir));
                Directory.CreateDirectory(actorDest);
                foreach (string srcFile in Directory.GetFiles(srcSubDir))
                {
                    string destFile = Path.Combine(actorDest, Path.GetFileName(srcFile));
                    if (!File.Exists(destFile))
                        File.Copy(srcFile, destFile);
                }
            }
        }

        private static string? TryGetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
    }
}
