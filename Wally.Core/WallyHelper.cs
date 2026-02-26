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

        /// <summary>Name of the config file that lives at the workspace folder root.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>Name of each actor's definition file inside its actor folder.</summary>
        public const string ActorFileName = "actor.json";

        // ?? Default workspace folder name ?????????????????????????????????????

        /// <summary>
        /// The conventional workspace folder name dropped into a project root.
        /// Callers that want to locate a workspace relative to a project use this.
        /// </summary>
        public const string DefaultWorkspaceFolderName = ".wally";

        // ?? Workspace root resolution ?????????????????????????????????????????

        /// <summary>
        /// Returns the default workspace folder path: <c>&lt;exeDir&gt;/.wally</c>.
        /// </summary>
        public static string GetDefaultWorkspaceFolder() =>
            Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    ?? Directory.GetCurrentDirectory(),
                DefaultWorkspaceFolderName);

        // ?? Workspace scaffolding ?????????????????????????????????????????????

        /// <summary>
        /// Scaffolds a complete workspace at <paramref name="workspaceFolder"/>:
        /// <code>
        ///   &lt;workspaceFolder&gt;/
        ///       wally-config.json
        ///       Actors/
        ///           Developer/
        ///               actor.json
        ///           Tester/
        ///               actor.json
        /// </code>
        /// Default actor folders are copied from the exe's own <c>.wally/</c> when present.
        /// Existing files are never overwritten.
        /// </summary>
        public static void CreateDefaultWorkspace(string workspaceFolder, WallyConfig config = null)
        {
            config ??= ResolveConfig(workspaceFolder);
            Directory.CreateDirectory(workspaceFolder);

            string destConfig = Path.Combine(workspaceFolder, ConfigFileName);
            if (!File.Exists(destConfig))
                config.SaveToFile(destConfig);

            CopyDefaultActors(workspaceFolder, config);
        }

        // ?? Actor loading ?????????????????????????????????????????????????????

        /// <summary>
        /// Reads every actor subfolder under <c>&lt;workspaceFolder&gt;/Actors/</c> and
        /// returns one <see cref="CopilotActor"/> per folder.
        /// Missing <c>actor.json</c> produces an empty-prompt actor rather than an error.
        /// </summary>
        public static List<Actor> LoadActors(
            string workspaceFolder, WallyConfig config, WallyWorkspace workspace = null)
        {
            var actors   = new List<Actor>();
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (!Directory.Exists(actorsDir)) return actors;

            foreach (string actorDir in Directory.GetDirectories(actorsDir))
            {
                string folderName = Path.GetFileName(actorDir);
                string jsonPath   = Path.Combine(actorDir, ActorFileName);

                string  name           = folderName;
                string  rolePrompt     = string.Empty;
                string? roleTier       = null;
                string  criteriaPrompt = string.Empty;
                string? criteriaTier   = null;
                string  intentPrompt   = string.Empty;
                string? intentTier     = null;

                if (File.Exists(jsonPath))
                {
                    var doc  = JsonDocument.Parse(File.ReadAllText(jsonPath));
                    var root = doc.RootElement;

                    name           = TryGetString(root, "name")           ?? folderName;
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
        /// Writes an actor's definition back to its <c>actor.json</c>, creating the
        /// folder if needed. Overwrites any existing file.
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

            File.WriteAllText(
                Path.Combine(actorDir, ActorFileName),
                JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ?? Config resolution ?????????????????????????????????????????????????

        /// <summary>
        /// Loads <c>wally-config.json</c> from <paramref name="workspaceFolder"/> when it
        /// exists, otherwise returns defaults.
        /// </summary>
        public static WallyConfig ResolveConfig(string workspaceFolder = null)
        {
            workspaceFolder ??= GetDefaultWorkspaceFolder();
            string configPath = Path.Combine(workspaceFolder, ConfigFileName);
            return File.Exists(configPath)
                ? WallyConfig.LoadFromFile(configPath)
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
        /// Copies the Actors subfolder from the exe's own <c>.wally/</c> directory into
        /// <paramref name="destWorkspaceFolder"/>. Existing files are not overwritten.
        /// </summary>
        private static void CopyDefaultActors(string destWorkspaceFolder, WallyConfig config)
        {
            string srcActors  = Path.Combine(GetDefaultWorkspaceFolder(), config.ActorsFolderName);
            string destActors = Path.Combine(destWorkspaceFolder, config.ActorsFolderName);
            if (!Directory.Exists(srcActors)) return;

            Directory.CreateDirectory(destActors);
            foreach (string srcActorDir in Directory.GetDirectories(srcActors))
            {
                string actorDest = Path.Combine(destActors, Path.GetFileName(srcActorDir));
                Directory.CreateDirectory(actorDest);
                foreach (string srcFile in Directory.GetFiles(srcActorDir))
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
