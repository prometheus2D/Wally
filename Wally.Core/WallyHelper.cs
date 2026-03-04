using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wally.Core.Actors;
using Wally.Core.Providers;
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

            config ??= new WallyConfig();
            // Ensure the workspace-level Docs folder exists.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.DocsFolderName));

            // Ensure the Loops folder exists.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.LoopsFolderName));

            // Ensure the Providers folder exists.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.WrappersFolderName));

            // Ensure the Runbooks folder exists.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.RunbooksFolderName));
        }

        // — Actor loading ———————————————————————————————————————————————————

        /// <summary>
        /// Reads every actor subfolder under <c>&lt;workspaceFolder&gt;/Actors/</c> and
        /// returns one <see cref="Actor"/> per folder.
        /// Missing <c>actor.json</c> produces an empty-prompt actor rather than an error.
        /// If no actors are found on disk, loads default actors from the template folder.
        /// </summary>
        public static List<Actor> LoadActors(
            string workspaceFolder, WallyConfig config, WallyWorkspace workspace = null)
        {
            var actors = new List<Actor>();
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                    actors.Add(LoadActorFromDirectory(actorDir, workspace));
            }

            // If no actors loaded from disk, load defaults from the template folder.
            if (actors.Count == 0)
                actors.AddRange(LoadDefaultActors(config, workspace));

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
        /// Checks the structural integrity of a workspace at <paramref name="workspaceFolder"/>
        /// without making any changes. Returns a list of issues found.
        /// An empty list means the workspace is valid.
        /// </summary>
        public static List<string> CheckWorkspace(string workspaceFolder)
        {
            var issues = new List<string>();
            workspaceFolder = Path.GetFullPath(workspaceFolder);

            if (!Directory.Exists(workspaceFolder))
            {
                issues.Add($"Workspace folder does not exist: {workspaceFolder}");
                return issues;
            }

            string configPath = Path.Combine(workspaceFolder, ConfigFileName);
            WallyConfig config = File.Exists(configPath)
                ? WallyConfig.LoadFromFile(configPath)
                : new WallyConfig();

            if (!File.Exists(configPath))
                issues.Add($"Config file missing: {configPath}");

            CheckDir(issues, workspaceFolder, config.ActorsFolderName);
            CheckDir(issues, workspaceFolder, config.DocsFolderName);
            CheckDir(issues, workspaceFolder, config.TemplatesFolderName);
            CheckDir(issues, workspaceFolder, config.LoopsFolderName);
            CheckDir(issues, workspaceFolder, config.WrappersFolderName);
            CheckDir(issues, workspaceFolder, config.LogsFolderName);
            CheckDir(issues, workspaceFolder, config.RunbooksFolderName);

            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                {
                    string actorName = Path.GetFileName(actorDir);
                    if (!File.Exists(Path.Combine(actorDir, ActorFileName)))
                        issues.Add($"Actor '{actorName}' missing {ActorFileName}");
                    if (!Directory.Exists(Path.Combine(actorDir, "Docs")))
                        issues.Add($"Actor '{actorName}' missing Docs folder");
                }
            }

            // Check for wally exe in the WorkSource (parent of .wally/).
            string workSource = Path.GetDirectoryName(workspaceFolder)!;
            string exeName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows) ? "wally.exe" : "wally";
            if (!File.Exists(Path.Combine(workSource, exeName)))
                issues.Add($"wally executable not found in {workSource} (run 'setup' to copy it)");

            return issues;
        }

        private static void CheckDir(List<string> issues, string root, string subFolder)
        {
            if (!Directory.Exists(Path.Combine(root, subFolder)))
                issues.Add($"{subFolder} folder missing");
        }

        // — Exe deployment ——————————————————————————————————————————————————

        /// <summary>
        /// Copies the Wally executable and its runtime dependencies from the
        /// current exe directory into <paramref name="workSourcePath"/> so the
        /// user can run <c>.\wally</c> directly from their codebase root.
        /// <para>
        /// Skips the <c>Default/</c> template folder (already expanded into
        /// <c>.wally/</c> during scaffolding) and the <c>.wally/</c> folder itself.
        /// Files that already exist in the destination are not overwritten.
        /// </para>
        /// </summary>
        /// <returns>
        /// The number of files copied, or <c>0</c> when the exe directory
        /// and <paramref name="workSourcePath"/> are the same location.
        /// </returns>
        public static int CopyExeToWorkSource(string workSourcePath)
        {
            string exeDir = GetExeDirectory();
            workSourcePath = Path.GetFullPath(workSourcePath);

            // Nothing to do if we're already running from the WorkSource.
            if (string.Equals(exeDir, workSourcePath, StringComparison.OrdinalIgnoreCase))
                return 0;

            // Skip these directories — they're workspace-specific, not runtime files.
            var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                DefaultTemplateFolderName,  // "Default" — already expanded into .wally/
                DefaultWorkspaceFolderName  // ".wally"  — workspace data, not runtime
            };

            return CopyRuntimeFiles(exeDir, workSourcePath, skipDirs);
        }

        private static int CopyRuntimeFiles(string sourceDir, string destDir, HashSet<string> skipDirs)
        {
            Directory.CreateDirectory(destDir);
            int count = 0;

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (!File.Exists(destFile))
                {
                    File.Copy(file, destFile);
                    count++;
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                if (skipDirs.Contains(dirName))
                    continue;
                count += CopyRuntimeFiles(subDir, Path.Combine(destDir, dirName), skipDirs);
            }

            return count;
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

        // — LLM wrapper loading ——————————————————————————————————————————

        /// <summary>
        /// Loads all LLM wrapper JSON files from
        /// <c>&lt;workspaceFolder&gt;/Wrappers/</c>.
        /// Falls back to the Default template folder when none are found.
        /// </summary>
        public static List<LLMWrapper> LoadLlmWrappers(
            string workspaceFolder, WallyConfig config)
        {
            string wrappersDir = Path.Combine(workspaceFolder, config.WrappersFolderName);
            var wrappers = LLMWrapper.LoadFromFolder(wrappersDir);

            // Fallback: load from shipped Default template if workspace has none.
            if (wrappers.Count == 0)
            {
                string defaultDir = Path.Combine(GetDefaultTemplateFolder(), config.WrappersFolderName);
                wrappers = LLMWrapper.LoadFromFolder(defaultDir);
            }

            return wrappers;
        }

        /// <summary>
        /// Finds the <see cref="LLMWrapper"/> whose <see cref="LLMWrapper.Name"/>
        /// matches <paramref name="wrapperName"/> (case-insensitive).
        /// Returns <see langword="null"/> when not found.
        /// </summary>
        public static LLMWrapper? ResolveWrapper(
            string wrapperName, List<LLMWrapper> wrappers)
        {
            return wrappers.FirstOrDefault(w =>
                string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase));
        }

        // — Loop loading ————————————————————————————————————————————————————

        /// <summary>
        /// Loads all loops from <c>&lt;workspaceFolder&gt;/Loops/</c>.
        /// Falls back to the Default template folder when no loops are found on disk.
        /// </summary>
        public static List<WallyLoopDefinition> LoadLoops(
            string workspaceFolder, WallyConfig config)
        {
            string loopsDir = Path.Combine(workspaceFolder, config.LoopsFolderName);
            var loops = WallyLoopDefinition.LoadFromFolder(loopsDir);

            // Fallback: load from shipped Default template if workspace has none.
            if (loops.Count == 0)
            {
                string defaultLoopsDir = Path.Combine(GetDefaultTemplateFolder(), config.LoopsFolderName);
                loops = WallyLoopDefinition.LoadFromFolder(defaultLoopsDir);
            }

            return loops;
        }

        // — Runbook loading ————————————————————————————————————————

        /// <summary>
        /// Loads all <c>.wrb</c> runbook files from
        /// <c>&lt;workspaceFolder&gt;/Runbooks/</c>.
        /// Falls back to the Default template folder when none are found.
        /// </summary>
        public static List<WallyRunbook> LoadRunbooks(
            string workspaceFolder, WallyConfig config)
        {
            string runbooksDir = Path.Combine(workspaceFolder, config.RunbooksFolderName);
            var runbooks = WallyRunbook.LoadFromFolder(runbooksDir);

            // Fallback: load from shipped Default template if workspace has none.
            if (runbooks.Count == 0)
            {
                string defaultDir = Path.Combine(GetDefaultTemplateFolder(), config.RunbooksFolderName);
                runbooks = WallyRunbook.LoadFromFolder(defaultDir);
            }

            return runbooks;
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

        private static List<Actor> LoadDefaultActors(WallyConfig config, WallyWorkspace workspace)
        {
            var actors = new List<Actor>();
            string defaultActorsDir = Path.Combine(GetDefaultTemplateFolder(), config.ActorsFolderName);

            if (!Directory.Exists(defaultActorsDir)) return actors;

            foreach (string actorDir in Directory.GetDirectories(defaultActorsDir))
                actors.Add(LoadActorFromDirectory(actorDir, workspace, isFallback: true));

            return actors;
        }

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

            var actor = new Actor(
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
