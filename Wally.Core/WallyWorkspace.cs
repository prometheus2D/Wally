using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Wally.Core.Actors;

namespace Wally.Core
{
    /// <summary>
    /// Represents a Wally workspace bound to a parent folder on disk.
    ///
    /// Layout on disk:
    /// <code>
    ///   &lt;ParentFolder&gt;/
    ///       &lt;ProjectFolderName&gt;/        ? codebase  (default: "Project")
    ///       &lt;WorkspaceFolderName&gt;/      ? Wally     (default: ".wally")
    ///           wally-config.json
    ///           default-agents.json
    ///           Roles/                   ? one .txt per role      (name = filename stem)
    ///           Criteria/                ? one .txt per criterion
    ///           Intents/                 ? one .txt per intent
    /// </code>
    ///
    /// <see cref="ProjectFolder"/> and <see cref="WorkspaceFolder"/> are always siblings —
    /// both children of <see cref="ParentFolder"/>.
    ///
    /// RBA prompts are loaded from the <c>Roles/</c>, <c>Criteria/</c>, and <c>Intents/</c>
    /// subdirectories on every <see cref="LoadFrom"/> call. Edit the <c>.txt</c> files
    /// directly to change actor behaviour without touching any JSON.
    ///
    /// Folders and files that actors should have access to are registered explicitly via
    /// <see cref="AddFolderReference"/> and <see cref="AddFileReference"/>.
    /// </summary>
    public class WallyWorkspace
    {
        // ?? Identity ??????????????????????????????????????????????????????????

        /// <summary>
        /// The absolute path to the common parent folder that contains both
        /// <see cref="ProjectFolder"/> and <see cref="WorkspaceFolder"/> as siblings.
        /// </summary>
        public string ParentFolder { get; private set; }

        /// <summary>
        /// The absolute path to the project subfolder — the codebase Wally operates on.
        /// Sibling of <see cref="WorkspaceFolder"/> inside <see cref="ParentFolder"/>.
        /// </summary>
        public string ProjectFolder { get; private set; }

        /// <summary>
        /// The absolute path to Wally's workspace subfolder where the config file lives.
        /// Sibling of <see cref="ProjectFolder"/> inside <see cref="ParentFolder"/>.
        /// </summary>
        public string WorkspaceFolder { get; private set; }

        /// <summary>
        /// <see langword="true"/> when the workspace has been successfully loaded from disk.
        /// </summary>
        public bool IsLoaded => !string.IsNullOrEmpty(ParentFolder);

        // ?? Configuration ?????????????????????????????????????????????????????

        /// <summary>
        /// The configuration that drives this workspace's actor RBA and runtime settings.
        /// Never null — a default <see cref="WallyConfig"/> is used as fallback.
        /// </summary>
        public WallyConfig Config { get; private set; } = new WallyConfig();

        // ?? References ????????????????????????????????????????????????????????

        /// <summary>
        /// Folder paths registered for actor/copilot access, stored as absolute paths.
        /// Add entries via <see cref="AddFolderReference"/>.
        /// </summary>
        public List<string> FolderReferences { get; private set; } = new List<string>();

        /// <summary>
        /// File paths registered for actor/copilot access, stored as absolute paths.
        /// Add entries via <see cref="AddFileReference"/>.
        /// </summary>
        public List<string> FileReferences { get; private set; } = new List<string>();

        // ?? Actor list ????????????????????????????????????????????????????????

        /// <summary>Actors built from the workspace config's RBA definitions.</summary>
        [JsonIgnore]
        public List<Actor> Actors { get; private set; } = new List<Actor>();

        // ?? Static factory ????????????????????????????????????????????????????

        /// <summary>
        /// Loads a workspace from <paramref name="parentFolder"/>. The workspace and project
        /// subfolders are resolved from the config inside the workspace subfolder, or from
        /// defaults when no config exists yet.
        /// </summary>
        public static WallyWorkspace Load(string parentFolder)
        {
            var ws = new WallyWorkspace();
            ws.LoadFrom(parentFolder);
            return ws;
        }

        /// <summary>
        /// Initialises this instance from <paramref name="path"/>, which may be any one of:
        /// <list type="bullet">
        ///   <item>The parent folder (contains workspace and project as siblings).</item>
        ///   <item>The workspace folder itself (contains <c>wally-config.json</c>).</item>
        ///   <item>The project folder (sibling of the workspace folder).</item>
        /// </list>
        /// The method resolves the canonical three-folder layout from wherever it is called.
        /// </summary>
        public void LoadFrom(string path)
        {
            path = Path.GetFullPath(path);

            string directConfig = Path.Combine(path, WallyHelper.ConfigFileName);

            if (File.Exists(directConfig))
            {
                // path IS the workspace folder
                WorkspaceFolder = path;
                ParentFolder    = Path.GetDirectoryName(path) ?? path;
                Config          = WallyConfig.LoadFromFile(directConfig);
                ProjectFolder   = Path.Combine(ParentFolder, Config.ProjectFolderName);
            }
            else
            {
                // Treat path as the parent folder
                ParentFolder = path;
                var defaultConfig = new WallyConfig();

                string subFolderConfig = Path.Combine(
                    path, defaultConfig.WorkspaceFolderName, WallyHelper.ConfigFileName);

                Config = File.Exists(subFolderConfig)
                    ? WallyConfig.LoadFromFile(subFolderConfig)
                    : new WallyConfig();

                WorkspaceFolder = Path.Combine(ParentFolder, Config.WorkspaceFolderName);
                ProjectFolder   = Path.Combine(ParentFolder, Config.ProjectFolderName);
            }

            Directory.CreateDirectory(WorkspaceFolder);
            Directory.CreateDirectory(ProjectFolder);

            // Populate RBA lists from the per-role/criteria/intent prompt files in the workspace.
            // This always overrides whatever was in wally-config.json for those lists.
            WallyHelper.LoadRbaFromPromptFiles(WorkspaceFolder, Config);

            BuildActors(Config);
        }

        // ?? Saving ????????????????????????????????????????????????????????????

        /// <summary>
        /// Persists the current <see cref="Config"/> to
        /// <c>&lt;WorkspaceFolder&gt;/wally-config.json</c>.
        /// </summary>
        public void Save()
        {
            RequireLoaded();
            Directory.CreateDirectory(WorkspaceFolder);
            Config.SaveToFile(Path.Combine(WorkspaceFolder, WallyHelper.ConfigFileName));
            SaveRbaToPromptFiles();
        }

        /// <summary>
        /// Writes each Role, AcceptanceCriteria, and Intent in <see cref="Config"/> out to
        /// its corresponding <c>.txt</c> file under the workspace's RBA subdirectories.
        /// Existing files are overwritten so the files stay in sync with in-memory state.
        /// </summary>
        private void SaveRbaToPromptFiles()
        {
            WritePromptFiles(Config.RolesFolderName,    Config.Roles,               r => (r.Name, r.Prompt, r.Tier));
            WritePromptFiles(Config.CriteriaFolderName, Config.AcceptanceCriterias, c => (c.Name, c.Prompt, c.Tier));
            WritePromptFiles(Config.IntentsFolderName,  Config.Intents,             i => (i.Name, i.Prompt, i.Tier));
        }

        private void WritePromptFiles<T>(string subDir, IEnumerable<T> items,
            Func<T, (string Name, string Prompt, string? Tier)> selector)
        {
            string dir = Path.Combine(WorkspaceFolder, subDir);
            Directory.CreateDirectory(dir);
            foreach (var item in items)
            {
                var (name, prompt, tier) = selector(item);
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Encode tier in filename when present: "Name.Tier.txt", otherwise "Name.txt"
                string fileName = string.IsNullOrWhiteSpace(tier)
                    ? $"{name}.txt"
                    : $"{name}.{tier}.txt";

                File.WriteAllText(Path.Combine(dir, fileName), prompt ?? string.Empty);
            }
        }

        // ?? Reference management ??????????????????????????????????????????????

        /// <summary>
        /// Registers a folder path for actor/copilot access.
        /// Relative paths are resolved against <see cref="ProjectFolder"/>.
        /// Duplicate entries are ignored.
        /// </summary>
        public void AddFolderReference(string folderPath)
        {
            string absolute = ResolveAbsolute(folderPath);
            if (!FolderReferences.Contains(absolute))
                FolderReferences.Add(absolute);
        }

        /// <summary>Removes a folder reference by path (relative or absolute).</summary>
        public bool RemoveFolderReference(string folderPath) =>
            FolderReferences.Remove(ResolveAbsolute(folderPath));

        /// <summary>
        /// Registers a file path for actor/copilot access.
        /// Relative paths are resolved against <see cref="ProjectFolder"/>.
        /// Duplicate entries are ignored.
        /// </summary>
        public void AddFileReference(string filePath)
        {
            string absolute = ResolveAbsolute(filePath);
            if (!FileReferences.Contains(absolute))
                FileReferences.Add(absolute);
        }

        /// <summary>Removes a file reference by path (relative or absolute).</summary>
        public bool RemoveFileReference(string filePath) =>
            FileReferences.Remove(ResolveAbsolute(filePath));

        /// <summary>Clears all registered folder and file references.</summary>
        public void ClearReferences()
        {
            FolderReferences.Clear();
            FileReferences.Clear();
        }

        // ?? Actor management ??????????????????????????????????????????????????

        /// <summary>Adds an actor to the workspace.</summary>
        public void AddActor(Actor actor) => Actors.Add(actor);

        /// <summary>Returns the first actor of type <typeparamref name="T"/>, or null.</summary>
        public T GetActor<T>() where T : Actor => Actors.Find(a => a is T) as T;

        // ?? Guard ?????????????????????????????????????????????????????????????

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if the workspace has not been loaded.
        /// </summary>
        public void RequireLoaded()
        {
            if (!IsLoaded)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'load <path>' or 'create <path>' first.");
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private void BuildActors(WallyConfig config)
        {
            Actors.Clear();
            foreach (var role in config.Roles)
                foreach (var criteria in config.AcceptanceCriterias)
                    foreach (var intent in config.Intents)
                        Actors.Add(new WallyActor(role, criteria, intent, this));
        }

        /// <summary>Resolves a path to absolute, using <see cref="ProjectFolder"/> as base.</summary>
        private string ResolveAbsolute(string path) =>
            Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(ProjectFolder, path));
    }
}
