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
        // — Well-known file names ————————————————————————————————————————————

        /// <summary>Name of the config file that lives at the workspace folder root.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>Name of each actor's definition file inside its actor folder.</summary>
        public const string ActorFileName = "actor.json";

        // — Default workspace folder name ———————————————————————————————————

        /// <summary>
        /// The conventional workspace folder name dropped into a project root.
        /// Callers that want to locate a workspace relative to a project use this.
        /// </summary>
        public const string DefaultWorkspaceFolderName = ".wally";

        /// <summary>
        /// The folder that ships alongside the executable and contains the default
        /// workspace template (<c>wally-config.json</c>, <c>Actors/</c>, etc.).
        /// The Console project copies <c>Default/**</c> to output via
        /// <c>CopyToOutputDirectory</c>.
        /// </summary>
        public const string DefaultTemplateFolderName = "Default";

        // — Workspace root resolution —————————————————————————————————————

        /// <summary>
        /// Returns the default workspace folder path: <c>&lt;exeDir&gt;/.wally</c>.
        /// </summary>
        public static string GetDefaultWorkspaceFolder() =>
            Path.Combine(GetExeDirectory(), DefaultWorkspaceFolderName);

        /// <summary>
        /// Returns the path to the default template folder that ships with the exe:
        /// <c>&lt;exeDir&gt;/Default</c>.
        /// </summary>
        public static string GetDefaultTemplateFolder() =>
            Path.Combine(GetExeDirectory(), DefaultTemplateFolderName);

        // — Workspace scaffolding ————————————————————————————————————————————

        /// <summary>
        /// Scaffolds a complete workspace at <paramref name="workspaceFolder"/>.
        /// <para>
        /// If the exe's <c>Default/</c> template folder exists, its entire contents
        /// are copied recursively into <paramref name="workspaceFolder"/> without
        /// overwriting any existing files.  This gives the new workspace the exact
        /// same folder structure and JSON files that ship with the application.
        /// </para>
        /// <para>
        /// When no template folder is available (e.g. running from a unit-test host),
        /// a minimal <c>wally-config.json</c> is serialised from
        /// <paramref name="config"/> (or <see cref="WallyConfig"/> defaults).
        /// The workspace-level <c>Docs/</c> folder is always created.
        /// </para>
        /// </summary>
        public static void CreateDefaultWorkspace(string workspaceFolder, WallyConfig config = null)
        {
            Directory.CreateDirectory(workspaceFolder);

            string templateFolder = GetDefaultTemplateFolder();
            if (Directory.Exists(templateFolder))
            {
                // Mirror the entire template into the workspace (no-overwrite).
                CopyDirectoryNoOverwrite(templateFolder, workspaceFolder);
            }
            else
            {
                // No template available — write a minimal config so the workspace is valid.
                config ??= new WallyConfig();
                string destConfig = Path.Combine(workspaceFolder, ConfigFileName);
                if (!File.Exists(destConfig))
                    config.SaveToFile(destConfig);
            }

            // Ensure the workspace-level Docs folder exists.
            config ??= new WallyConfig();
            string docsFolder = Path.Combine(workspaceFolder, config.DocsFolderName);
            Directory.CreateDirectory(docsFolder);
        }

        // — Actor loading ———————————————————————————————————————————————————

        /// <summary>
        /// Reads every actor subfolder under <c>&lt;workspaceFolder&gt;/Actors/</c> and
        /// returns one <see cref="CopilotActor"/> per folder.
        /// Missing <c>actor.json</c> produces an empty-prompt actor rather than an error.
        /// If no actors are found on disk, loads default actors from the template folder.
        /// </summary>
        public static List<Actor> LoadActors(
            string workspaceFolder, WallyConfig config, WallyWorkspace workspace = null)
        {
            var actors   = new List<Actor>();
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                {
                    var actor = LoadActorFromDirectory(actorDir, workspace);
                    actors.Add(actor);
                }
            }

            // If no actors loaded from disk, load defaults from the template folder
            if (actors.Count == 0)
            {
                actors.AddRange(LoadDefaultActors(config, workspace));
            }

            return actors;
        }

        /// <summary>
        /// Writes an actor's definition back to its <c>actor.json</c>, creating the
        /// folder if needed. Overwrites any existing file. Also creates the
        /// actor's <c>Docs/</c> subfolder if it does not exist.
        /// </summary>
        public static void SaveActor(string workspaceFolder, WallyConfig config, Actor actor)
        {
            string actorDir = Path.Combine(workspaceFolder, config.ActorsFolderName, actor.Name);
            Directory.CreateDirectory(actorDir);

            // Ensure the actor's Docs folder exists on disk.
            string actorDocsDir = Path.Combine(actorDir, actor.DocsFolderName);
            Directory.CreateDirectory(actorDocsDir);

            var obj = new
            {
                name           = actor.Name,
                rolePrompt     = actor.Role.Prompt,
                criteriaPrompt = actor.AcceptanceCriteria.Prompt,
                intentPrompt   = actor.Intent.Prompt,
                docsFolderName = actor.DocsFolderName
            };

            File.WriteAllText(
                Path.Combine(actorDir, ActorFileName),
                JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }

        // — Config resolution ——————————————————————————————————————————————

        /// <summary>
        /// Loads <c>wally-config.json</c> from <paramref name="workspaceFolder"/> when it
        /// exists, then tries the shipped template folder, otherwise returns defaults.
        /// </summary>
        public static WallyConfig ResolveConfig(string workspaceFolder = null)
        {
            workspaceFolder ??= GetDefaultWorkspaceFolder();

            // 1. Workspace-local config
            string configPath = Path.Combine(workspaceFolder, ConfigFileName);
            if (File.Exists(configPath))
                return WallyConfig.LoadFromFile(configPath);

            // 2. Template config shipped with the exe
            string templateConfig = Path.Combine(GetDefaultTemplateFolder(), ConfigFileName);
            if (File.Exists(templateConfig))
                return WallyConfig.LoadFromFile(templateConfig);

            // 3. Hard-coded defaults
            return new WallyConfig();
        }

        // — Default environment loading ———————————————————————————————————————

        public static WallyEnvironment LoadDefault()
        {
            var env = new WallyEnvironment();
            env.SetupLocal();
            return env;
        }

        // — Workspace verification ——————————————————————————————————————————

        /// <summary>
        /// Verifies the structural integrity of a workspace at <paramref name="workspaceFolder"/>.
        /// Returns a list of issues found. An empty list means the workspace is valid.
        /// When <paramref name="repair"/> is true, missing directories are created
        /// and a missing config file is written from defaults.
        /// </summary>
        public static List<string> VerifyWorkspace(string workspaceFolder, bool repair = false)
        {
            var issues = new List<string>();
            workspaceFolder = Path.GetFullPath(workspaceFolder);

            // 1. Workspace folder exists
            if (!Directory.Exists(workspaceFolder))
            {
                issues.Add($"Workspace folder does not exist: {workspaceFolder}");
                if (repair)
                {
                    Directory.CreateDirectory(workspaceFolder);
                    issues.Add("  ? Created workspace folder.");
                }
                else
                {
                    return issues; // Can't check further if folder doesn't exist
                }
            }

            // 2. Config file
            string configPath = Path.Combine(workspaceFolder, ConfigFileName);
            WallyConfig config;
            if (!File.Exists(configPath))
            {
                issues.Add($"Config file missing: {configPath}");
                if (repair)
                {
                    config = new WallyConfig();
                    config.SaveToFile(configPath);
                    issues.Add("  ? Created default config file.");
                }
                else
                {
                    config = new WallyConfig();
                }
            }
            else
            {
                config = WallyConfig.LoadFromFile(configPath);
            }

            // 3. Actors folder
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (!Directory.Exists(actorsDir))
            {
                issues.Add($"Actors folder missing: {actorsDir}");
                if (repair)
                {
                    Directory.CreateDirectory(actorsDir);
                    issues.Add("  ? Created Actors folder.");
                }
            }

            // 4. Docs folder
            string docsDir = Path.Combine(workspaceFolder, config.DocsFolderName);
            if (!Directory.Exists(docsDir))
            {
                issues.Add($"Docs folder missing: {docsDir}");
                if (repair)
                {
                    Directory.CreateDirectory(docsDir);
                    issues.Add("  ? Created Docs folder.");
                }
            }

            // 5. Templates folder
            string templatesDir = Path.Combine(workspaceFolder, config.TemplatesFolderName);
            if (!Directory.Exists(templatesDir))
            {
                issues.Add($"Templates folder missing: {templatesDir}");
                if (repair)
                {
                    Directory.CreateDirectory(templatesDir);
                    issues.Add("  ? Created Templates folder.");

                    // Copy templates from the shipped Default folder if available
                    string defaultTemplates = Path.Combine(GetDefaultTemplateFolder(), config.TemplatesFolderName);
                    if (Directory.Exists(defaultTemplates))
                    {
                        CopyDirectoryNoOverwrite(defaultTemplates, templatesDir);
                        issues.Add("  ? Copied default templates.");
                    }
                }
            }

            // 6. Logs folder
            string logsDir = Path.Combine(workspaceFolder, config.LogsFolderName);
            if (!Directory.Exists(logsDir))
            {
                issues.Add($"Logs folder missing: {logsDir}");
                if (repair)
                {
                    Directory.CreateDirectory(logsDir);
                    issues.Add("  ? Created Logs folder.");
                }
            }

            // 7. Actor subfolders — check each has an actor.json and Docs/
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                {
                    string actorJson = Path.Combine(actorDir, ActorFileName);
                    string actorName = Path.GetFileName(actorDir);
                    if (!File.Exists(actorJson))
                    {
                        issues.Add($"Actor '{actorName}' is missing {ActorFileName}: {actorJson}");
                    }

                    string actorDocsDir = Path.Combine(actorDir, "Docs");
                    if (!Directory.Exists(actorDocsDir))
                    {
                        issues.Add($"Actor '{actorName}' is missing Docs folder: {actorDocsDir}");
                        if (repair)
                        {
                            Directory.CreateDirectory(actorDocsDir);
                            issues.Add($"  ? Created Docs folder for actor '{actorName}'.");
                        }
                    }
                }
            }

            return issues;
        }

        // — Directory utilities —————————————————————————————————————————————

        /// <summary>Recursively copies <paramref name="sourceDir"/> into <paramref name="destDir"/>.</summary>
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        // — Private helpers —————————————————————————————————————————————————

        /// <summary>
        /// Returns the directory that contains the entry-point executable.
        /// Falls back to <see cref="Directory.GetCurrentDirectory"/> when the
        /// entry assembly location cannot be determined.
        /// </summary>
        public static string GetExeDirectory() =>
            Path.GetDirectoryName(
                System.Reflection.Assembly.GetEntryAssembly()?.Location
                ?? System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();

        private static void CopyDirectoryNoOverwrite(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Copy(file, destFile);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectoryNoOverwrite(
                    subDir,
                    Path.Combine(destDir, Path.GetFileName(subDir)));
            }
        }

        /// <summary>
        /// Loads default actors from the <c>Default/Actors/</c> template folder that
        /// ships alongside the executable. Returns an empty list when the template
        /// folder is not present (e.g. running from a unit-test host).
        /// </summary>
        private static List<Actor> LoadDefaultActors(WallyConfig config, WallyWorkspace workspace)
        {
            var actors = new List<Actor>();
            string defaultActorsDir = Path.Combine(GetDefaultTemplateFolder(), config.ActorsFolderName);

            if (!Directory.Exists(defaultActorsDir)) return actors;

            foreach (string actorDir in Directory.GetDirectories(defaultActorsDir))
            {
                var actor = LoadActorFromDirectory(actorDir, workspace, isFallback: true);
                actors.Add(actor);
            }

            return actors;
        }

        /// <summary>
        /// Reads a single actor from <paramref name="actorDir"/> (which must contain
        /// an <c>actor.json</c>).  When <paramref name="isFallback"/> is true the actor's
        /// <see cref="Actor.FolderPath"/> is set to <see cref="string.Empty"/> because
        /// it does not represent a workspace-local folder.
        /// </summary>
        private static Actor LoadActorFromDirectory(
            string actorDir, WallyWorkspace? workspace, bool isFallback = false)
        {
            string folderName = Path.GetFileName(actorDir);
            string jsonPath   = Path.Combine(actorDir, ActorFileName);

            string name           = folderName;
            string rolePrompt     = string.Empty;
            string criteriaPrompt = string.Empty;
            string intentPrompt   = string.Empty;
            string docsFolderName = "Docs";

            if (File.Exists(jsonPath))
            {
                try
                {
                    var doc  = JsonDocument.Parse(File.ReadAllText(jsonPath));
                    var root = doc.RootElement;

                    name           = TryGetString(root, "name")           ?? folderName;
                    rolePrompt     = TryGetString(root, "rolePrompt")     ?? string.Empty;
                    criteriaPrompt = TryGetString(root, "criteriaPrompt") ?? string.Empty;
                    intentPrompt   = TryGetString(root, "intentPrompt")   ?? string.Empty;
                    docsFolderName = TryGetString(root, "docsFolderName") ?? "Docs";
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to parse '{jsonPath}': {ex.Message}. " +
                        $"Actor '{folderName}' will load with empty prompts.");
                }
            }

            var actor = new CopilotActor(
                name,
                isFallback ? string.Empty : actorDir,
                new Role(name, rolePrompt),
                new AcceptanceCriteria(name, criteriaPrompt),
                new Intent(name, intentPrompt),
                workspace);

            actor.DocsFolderName = docsFolderName;

            return actor;
        }

        private static string? TryGetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
    }
}
