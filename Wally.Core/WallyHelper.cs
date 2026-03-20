using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wally.Core.Actions;
using Wally.Core.Actors;
using Wally.Core.Providers;

namespace Wally.Core
{
    public static class WallyHelper
    {
        // ? Well-known file names ??????????????????????????????????????????????

        /// <summary>Name of the config file that lives at the workspace folder root.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>Name of each actor's definition file inside its actor folder.</summary>
        public const string ActorFileName = "actor.json";

        // ? Mailbox folder names ???????????????????????????????????????????????

        /// <summary>Standard inbox folder name — created inside each actor folder.</summary>
        public const string MailboxInboxFolderName   = "Inbox";

        /// <summary>Standard outbox folder name.</summary>
        public const string MailboxOutboxFolderName  = "Outbox";

        /// <summary>Standard pending folder name.</summary>
        public const string MailboxPendingFolderName = "Pending";

        /// <summary>Standard active folder name.</summary>
        public const string MailboxActiveFolderName  = "Active";

        // ? Projects hierarchy folder names ???????????????????????????????????

        /// <summary>Folder name for epoch containers inside a project folder.</summary>
        public const string ProjectEpochsFolderName  = "Epochs";

        /// <summary>Folder name for sprint containers inside an epoch folder.</summary>
        public const string ProjectSprintsFolderName = "Sprints";

        /// <summary>Folder name for task containers inside an epoch or sprint folder.</summary>
        public const string ProjectTasksFolderName   = "Tasks";

        // ? Default workspace folder name ?????????????????????????????????????

        /// <summary>
        /// The conventional workspace folder name dropped into a project root.
        /// </summary>
        public const string DefaultWorkspaceFolderName = ".wally";

        /// <summary>
        /// The folder that ships alongside the executable and contains the default
        /// workspace template (<c>wally-config.json</c>, <c>Actors/</c>, etc.).
        /// </summary>
        public const string DefaultTemplateFolderName = "Default";

        // ? Workspace root resolution ??????????????????????????????????????????

        /// <summary>Returns the default workspace folder path: <c>&lt;exeDir&gt;/.wally</c>.</summary>
        public static string GetDefaultWorkspaceFolder() =>
            Path.Combine(GetExeDirectory(), DefaultWorkspaceFolderName);

        /// <summary>Returns the path to the default template folder shipped with the exe.</summary>
        public static string GetDefaultTemplateFolder() =>
            Path.Combine(GetExeDirectory(), DefaultTemplateFolderName);

        // ? Workspace scaffolding ??????????????????????????????????????????????

        /// <summary>
        /// Scaffolds a complete workspace at <paramref name="workspaceFolder"/>.
        /// Creates the <c>Projects/</c> folder, standard tooling folders, and
        /// per-actor mailboxes.
        /// </summary>
        public static void CreateDefaultWorkspace(string workspaceFolder, WallyConfig config = null)
        {
            Directory.CreateDirectory(workspaceFolder);

            string templateFolder = GetDefaultTemplateFolder();
            if (Directory.Exists(templateFolder))
            {
                CopyDirectoryNoOverwrite(templateFolder, workspaceFolder);
            }
            else
            {
                config ??= new WallyConfig();
                string destConfig = Path.Combine(workspaceFolder, ConfigFileName);
                if (!File.Exists(destConfig))
                    config.SaveToFile(destConfig);
            }

            config ??= new WallyConfig();

            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.DocsFolderName));
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.TemplatesFolderName));
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.LoopsFolderName));
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.WrappersFolderName));
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.RunbooksFolderName));
            Directory.CreateDirectory(Path.Combine(workspaceFolder, Logging.ConversationLogger.DefaultFolderName));

            // Projects/ — shared project store (Epochs ? Sprints ? Tasks).
            // Scaffolded empty; structure is created at runtime by actors/users.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.ProjectsFolderName));

            // Per-actor mailboxes created on first actor save; scaffold actors dir now.
            Directory.CreateDirectory(Path.Combine(workspaceFolder, config.ActorsFolderName));
            EnsureAllMailboxFolders(workspaceFolder, config);
        }

        // ? Mailbox helpers ????????????????????????????????????????????????????

        /// <summary>
        /// Creates the four standard mailbox folders (Inbox, Outbox, Pending, Active)
        /// inside <paramref name="entityDir"/>. Used for actor-private mailboxes.
        /// Idempotent — safe to call on every load or setup.
        /// </summary>
        public static void CreateMailboxFolders(string entityDir)
        {
            Directory.CreateDirectory(Path.Combine(entityDir, MailboxInboxFolderName));
            Directory.CreateDirectory(Path.Combine(entityDir, MailboxOutboxFolderName));
            Directory.CreateDirectory(Path.Combine(entityDir, MailboxPendingFolderName));
            Directory.CreateDirectory(Path.Combine(entityDir, MailboxActiveFolderName));
        }

        /// <summary>
        /// Creates mailbox folders for every actor subfolder currently on disk.
        /// Idempotent — safe to call on every load or setup.
        /// </summary>
        public static void EnsureAllMailboxFolders(string workspaceFolder, WallyConfig config = null)
        {
            config ??= ResolveConfig(workspaceFolder);

            // Per-actor mailboxes: .wally/Actors/<Name>/Inbox|Outbox|Pending|Active
            string actorsDir = Path.Combine(workspaceFolder, config.ActorsFolderName);
            if (!Directory.Exists(actorsDir)) return;

            foreach (string actorDir in Directory.GetDirectories(actorsDir))
                CreateMailboxFolders(actorDir);
        }

        /// <summary>
        /// Creates a new project folder scaffold under <c>Projects/&lt;projectName&gt;/</c>
        /// with an <c>Epochs/</c> subfolder. The project folder and epoch/sprint/task
        /// subfolders are created on demand at runtime.
        /// Returns the full path to the new project folder.
        /// </summary>
        public static string CreateProjectFolder(string workspaceFolder, string projectName, WallyConfig config = null)
        {
            config ??= ResolveConfig(workspaceFolder);
            string projectDir = Path.Combine(workspaceFolder, config.ProjectsFolderName, projectName);
            Directory.CreateDirectory(Path.Combine(projectDir, ProjectEpochsFolderName));
            return projectDir;
        }

        /// <summary>
        /// Creates an epoch folder under <c>Projects/&lt;projectName&gt;/Epochs/&lt;epochName&gt;/</c>
        /// with <c>Tasks/</c> and <c>Sprints/</c> subfolders pre-created.
        /// Returns the full path to the new epoch folder.
        /// </summary>
        public static string CreateEpochFolder(string workspaceFolder, string projectName, string epochName, WallyConfig config = null)
        {
            config ??= ResolveConfig(workspaceFolder);
            string epochDir = Path.Combine(
                workspaceFolder, config.ProjectsFolderName,
                projectName, ProjectEpochsFolderName, epochName);
            Directory.CreateDirectory(Path.Combine(epochDir, ProjectTasksFolderName));
            Directory.CreateDirectory(Path.Combine(epochDir, ProjectSprintsFolderName));
            return epochDir;
        }

        /// <summary>
        /// Creates a sprint folder under <c>Epochs/&lt;epochName&gt;/Sprints/&lt;sprintName&gt;/</c>
        /// with a <c>Tasks/</c> subfolder pre-created.
        /// Returns the full path to the new sprint folder.
        /// </summary>
        public static string CreateSprintFolder(string workspaceFolder, string projectName, string epochName, string sprintName, WallyConfig config = null)
        {
            config ??= ResolveConfig(workspaceFolder);
            string sprintDir = Path.Combine(
                workspaceFolder, config.ProjectsFolderName,
                projectName, ProjectEpochsFolderName, epochName,
                ProjectSprintsFolderName, sprintName);
            Directory.CreateDirectory(Path.Combine(sprintDir, ProjectTasksFolderName));
            return sprintDir;
        }

        // ? Actor loading ?????????????????????????????????????????????????????

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
                {
                    var actor = LoadActorFromDirectory(actorDir, workspace);
                    if (actor != null) actors.Add(actor);
                }
            }

            if (actors.Count == 0)
                actors.AddRange(LoadDefaultActors(config, workspace));

            return actors;
        }

        /// <summary>
        /// Writes an actor's definition back to its <c>actor.json</c>, creating the
        /// folder if needed. Also creates the actor's <c>Docs/</c> subfolder and
        /// the four mailbox folders (Inbox, Outbox, Pending, Active).
        /// Now persists AllowedWrappers, AllowedLoops, PreferredWrapper,
        /// PreferredLoop, and Actions.
        /// </summary>
        public static void SaveActor(string workspaceFolder, WallyConfig config, Actor actor)
        {
            string actorDir = Path.Combine(workspaceFolder, config.ActorsFolderName, actor.Name);
            Directory.CreateDirectory(actorDir);

            Directory.CreateDirectory(Path.Combine(actorDir, actor.DocsFolderName));

            // Actor-level mailbox
            CreateMailboxFolders(actorDir);

            // Serialise actions as plain anonymous objects (no C# type info in JSON)
            var actionsData = actor.Actions.Select(a => new
            {
                name         = a.Name,
                description  = a.Description,
                pathPattern  = a.PathPattern,
                isMutating   = a.IsMutating,
                parameters   = a.Parameters.Select(p => new
                {
                    name        = p.Name,
                    type        = p.Type,
                    description = p.Description,
                    required    = p.Required
                }).ToList()
            }).ToList();

            var obj = new
            {
                name             = actor.Name,
                enabled          = actor.Enabled,
                rolePrompt       = actor.RolePrompt,
                criteriaPrompt   = actor.CriteriaPrompt,
                intentPrompt     = actor.IntentPrompt,
                docsFolderName   = actor.DocsFolderName,
                allowedWrappers  = actor.AllowedWrappers,
                allowedLoops     = actor.AllowedLoops,
                preferredWrapper = actor.PreferredWrapper,
                preferredLoop    = actor.PreferredLoop,
                actions          = actionsData
            };

            File.WriteAllText(
                Path.Combine(actorDir, ActorFileName),
                JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ? Config resolution ?????????????????????????????????????????????????

        /// <summary>
        /// Loads <c>wally-config.json</c> from <paramref name="workspaceFolder"/> when it
        /// exists, then tries the shipped template folder, otherwise returns defaults.
        /// </summary>
        public static WallyConfig ResolveConfig(string workspaceFolder = null)
        {
            workspaceFolder ??= GetDefaultWorkspaceFolder();

            string configPath = Path.Combine(workspaceFolder, ConfigFileName);
            if (File.Exists(configPath))
                return WallyConfig.LoadFromFile(configPath);

            string templateConfig = Path.Combine(GetDefaultTemplateFolder(), ConfigFileName);
            if (File.Exists(templateConfig))
                return WallyConfig.LoadFromFile(templateConfig);

            return new WallyConfig();
        }

        // ? Default environment loading ????????????????????????????????????????

        public static WallyEnvironment LoadDefault()
        {
            var env = new WallyEnvironment();
            env.SetupLocal();
            return env;
        }

        // ? Workspace verification ?????????????????????????????????????????????

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

            // Tooling folders
            CheckDir(issues, workspaceFolder, config.ActorsFolderName);
            CheckDir(issues, workspaceFolder, config.DocsFolderName);
            CheckDir(issues, workspaceFolder, config.TemplatesFolderName);
            CheckDir(issues, workspaceFolder, config.LoopsFolderName);
            CheckDir(issues, workspaceFolder, config.WrappersFolderName);
            CheckDir(issues, workspaceFolder, config.LogsFolderName);
            CheckDir(issues, workspaceFolder, config.RunbooksFolderName);
            CheckDir(issues, workspaceFolder, Logging.ConversationLogger.DefaultFolderName);

            // Projects store
            CheckDir(issues, workspaceFolder, config.ProjectsFolderName);

            // Per-actor structure
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
                    CheckMailboxFolders(issues, actorDir, $"Actor '{actorName}'");
                }
            }

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

        private static void CheckMailboxFolders(List<string> issues, string entityDir, string entityLabel)
        {
            foreach (string folder in new[] { MailboxInboxFolderName, MailboxOutboxFolderName, MailboxPendingFolderName, MailboxActiveFolderName })
            {
                if (!Directory.Exists(Path.Combine(entityDir, folder)))
                    issues.Add($"{entityLabel} missing {folder} mailbox folder");
            }
        }

        // ? Exe deployment ?????????????????????????????????????????????????????

        /// <summary>
        /// Copies the Wally executable and its runtime dependencies from the
        /// current exe directory into <paramref name="workSourcePath"/>.
        /// </summary>
        public static int CopyExeToWorkSource(string workSourcePath)
        {
            string exeDir = GetExeDirectory();
            workSourcePath = Path.GetFullPath(workSourcePath);

            if (string.Equals(exeDir, workSourcePath, StringComparison.OrdinalIgnoreCase))
                return 0;

            var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                DefaultTemplateFolderName,
                DefaultWorkspaceFolderName
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
                if (skipDirs.Contains(dirName)) continue;
                count += CopyRuntimeFiles(subDir, Path.Combine(destDir, dirName), skipDirs);
            }

            return count;
        }

        // ? Directory utilities ????????????????????????????????????????????????

        /// <summary>Recursively copies <paramref name="sourceDir"/> into <paramref name="destDir"/>.</summary>
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        // ? LLM wrapper loading ????????????????????????????????????????????????

        /// <summary>Loads all LLM wrapper JSON files from the workspace Wrappers/ folder.</summary>
        public static List<LLMWrapper> LoadLlmWrappers(string workspaceFolder, WallyConfig config)
        {
            string wrappersDir = Path.Combine(workspaceFolder, config.WrappersFolderName);
            var wrappers = LLMWrapper.LoadFromFolder(wrappersDir);

            if (wrappers.Count == 0)
            {
                string defaultDir = Path.Combine(GetDefaultTemplateFolder(), config.WrappersFolderName);
                wrappers = LLMWrapper.LoadFromFolder(defaultDir);
            }

            return wrappers;
        }

        /// <summary>Finds an <see cref="LLMWrapper"/> by name (case-insensitive).</summary>
        public static LLMWrapper? ResolveWrapper(string wrapperName, List<LLMWrapper> wrappers) =>
            wrappers.FirstOrDefault(w => string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase));

        // ? Loop loading ???????????????????????????????????????????????????????

        /// <summary>Loads all loops from the workspace Loops/ folder.</summary>
        public static List<WallyLoopDefinition> LoadLoops(string workspaceFolder, WallyConfig config)
        {
            string loopsDir = Path.Combine(workspaceFolder, config.LoopsFolderName);
            var loops = WallyLoopDefinition.LoadFromFolder(loopsDir);

            if (loops.Count == 0)
            {
                string defaultLoopsDir = Path.Combine(GetDefaultTemplateFolder(), config.LoopsFolderName);
                loops = WallyLoopDefinition.LoadFromFolder(defaultLoopsDir);
            }

            return loops;
        }

        // ? Runbook loading / saving ???????????????????????????????????????????

        /// <summary>Loads all .wrb runbook files from the workspace Runbooks/ folder.</summary>
        public static List<WallyRunbook> LoadRunbooks(string workspaceFolder, WallyConfig config)
        {
            string runbooksDir = Path.Combine(workspaceFolder, config.RunbooksFolderName);
            var runbooks = WallyRunbook.LoadFromFolder(runbooksDir);

            if (runbooks.Count == 0)
            {
                string defaultDir = Path.Combine(GetDefaultTemplateFolder(), config.RunbooksFolderName);
                runbooks = WallyRunbook.LoadFromFolder(defaultDir);
            }

            return runbooks;
        }

        /// <summary>Writes a runbook to its .wrb file on disk.</summary>
        public static void SaveRunbook(string workspaceFolder, WallyConfig config, WallyRunbook runbook)
        {
            string runbooksDir = Path.Combine(workspaceFolder, config.RunbooksFolderName);
            Directory.CreateDirectory(runbooksDir);

            string filePath = runbook.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = Path.Combine(runbooksDir, $"{runbook.Name}.wrb");

            var lines = new List<string>();

            // Persist the enabled directive first so it is easy to spot at the
            // top of the file. Only write "false" explicitly — absence means true.
            if (!runbook.Enabled)
                lines.Add("# enabled: false");

            if (!string.IsNullOrWhiteSpace(runbook.Description))
                lines.Add($"# {runbook.Description}");

            lines.AddRange(runbook.Commands);

            File.WriteAllLines(filePath, lines);
            runbook.FilePath = Path.GetFullPath(filePath);
        }

        // ? Private helpers ????????????????????????????????????????????????????

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
                CopyDirectoryNoOverwrite(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        private static List<Actor> LoadDefaultActors(WallyConfig config, WallyWorkspace workspace)
        {
            var actors = new List<Actor>();
            string defaultActorsDir = Path.Combine(GetDefaultTemplateFolder(), config.ActorsFolderName);
            if (!Directory.Exists(defaultActorsDir)) return actors;
            foreach (string actorDir in Directory.GetDirectories(defaultActorsDir))
            {
                var actor = LoadActorFromDirectory(actorDir, workspace, isFallback: true);
                if (actor != null) actors.Add(actor);
            }
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
            bool   enabled        = true;

            var allowedWrappers  = new List<string>();
            var allowedLoops     = new List<string>();
            string? preferredWrapper = null;
            string? preferredLoop    = null;
            var actions          = new List<ActorAction>();

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
                    preferredWrapper = TryGetString(root, "preferredWrapper");
                    preferredLoop    = TryGetString(root, "preferredLoop");
                    enabled          = TryGetBool(root, "enabled") ?? true;

                    allowedWrappers = TryGetStringList(root, "allowedWrappers");
                    allowedLoops    = TryGetStringList(root, "allowedLoops");
                    actions         = TryGetActions(root);
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to parse '{jsonPath}': {ex.Message}. " +
                        $"Actor '{folderName}' will load with empty prompts.");
                }
            }

            // Return null sentinel so the caller can filter without constructing
            // a partially initialised actor object.
            if (!enabled) return null!;

            var actor = new Actor(
                name,
                isFallback ? string.Empty : actorDir,
                rolePrompt,
                criteriaPrompt,
                intentPrompt,
                workspace);

            actor.Enabled          = enabled;
            actor.DocsFolderName   = docsFolderName;
            actor.AllowedWrappers  = allowedWrappers;
            actor.AllowedLoops     = allowedLoops;
            actor.PreferredWrapper = preferredWrapper;
            actor.PreferredLoop    = preferredLoop;
            actor.Actions          = actions;

            return actor;
        }

        // ? JSON helpers ???????????????????????????????????????????????????????

        private static string? TryGetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;

        private static List<string> TryGetStringList(JsonElement element, string propertyName)
        {
            var result = new List<string>();
            if (!element.TryGetProperty(propertyName, out var prop) ||
                prop.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) result.Add(s!);
                }
            }
            return result;
        }

        private static List<ActorAction> TryGetActions(JsonElement root)
        {
            var result = new List<ActorAction>();
            if (!root.TryGetProperty("actions", out var actionsEl) ||
                actionsEl.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var actionEl in actionsEl.EnumerateArray())
            {
                if (actionEl.ValueKind != JsonValueKind.Object) continue;

                var action = new ActorAction
                {
                    Name        = TryGetString(actionEl, "name")        ?? string.Empty,
                    Description = TryGetString(actionEl, "description") ?? string.Empty,
                    PathPattern = TryGetString(actionEl, "pathPattern"),
                    IsMutating  = TryGetBool(actionEl, "isMutating") ?? false,
                    Parameters  = new List<ActionParameter>()
                };

                if (string.IsNullOrWhiteSpace(action.Name)) continue;

                if (actionEl.TryGetProperty("parameters", out var paramsEl) &&
                    paramsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var paramEl in paramsEl.EnumerateArray())
                    {
                        if (paramEl.ValueKind != JsonValueKind.Object) continue;
                        action.Parameters.Add(new ActionParameter
                        {
                            Name        = TryGetString(paramEl, "name")        ?? string.Empty,
                            Type        = TryGetString(paramEl, "type")        ?? "string",
                            Description = TryGetString(paramEl, "description") ?? string.Empty,
                            Required    = TryGetBool(paramEl, "required")      ?? true
                        });
                    }
                }

                result.Add(action);
            }
            return result;
        }

        private static bool? TryGetBool(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop)) return null;
            if (prop.ValueKind == JsonValueKind.True)  return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
            return null;
        }
    }
}
