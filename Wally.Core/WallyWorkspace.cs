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
    ///       &lt;ProjectFolderName&gt;/    ? codebase  (default: "Project")
    ///       &lt;WorkspaceFolderName&gt;/  ? Wally     (default: ".wally")
    ///           wally-config.json
    ///           Agents/
    ///               &lt;AgentName&gt;/     ? one folder per agent
    ///                   role.txt       ? Role prompt  (optional: # Tier: task)
    ///                   criteria.txt   ? AcceptanceCriteria prompt
    ///                   intent.txt     ? Intent prompt
    /// </code>
    ///
    /// Each subfolder under <c>Agents/</c> defines exactly one <see cref="CopilotActor"/>
    /// with its own private Role, AcceptanceCriteria, and Intent. Add a subfolder to
    /// create a new agent; edit its <c>.txt</c> files to change its behaviour.
    ///
    /// <see cref="ProjectFolder"/> and <see cref="WorkspaceFolder"/> are always siblings
    /// — both children of <see cref="ParentFolder"/>.
    /// </summary>
    public class WallyWorkspace
    {
        // ?? Identity ??????????????????????????????????????????????????????????

        public string ParentFolder    { get; private set; }
        public string ProjectFolder   { get; private set; }
        public string WorkspaceFolder { get; private set; }

        public bool IsLoaded => !string.IsNullOrEmpty(ParentFolder);

        // ?? Configuration ?????????????????????????????????????????????????????

        public WallyConfig Config { get; private set; } = new WallyConfig();

        // ?? References ????????????????????????????????????????????????????????

        public List<string> FolderReferences { get; private set; } = new List<string>();
        public List<string> FileReferences   { get; private set; } = new List<string>();

        // ?? Actor list ????????????????????????????????????????????????????????

        /// <summary>
        /// One <see cref="CopilotActor"/> per agent folder under <c>Agents/</c>.
        /// Each actor carries its own private RBA — no shared state.
        /// </summary>
        [JsonIgnore]
        public List<Actor> Actors { get; private set; } = new List<Actor>();

        // ?? Static factory ????????????????????????????????????????????????????

        public static WallyWorkspace Load(string parentFolder)
        {
            var ws = new WallyWorkspace();
            ws.LoadFrom(parentFolder);
            return ws;
        }

        /// <summary>
        /// Initialises from <paramref name="path"/>, which may be the parent folder,
        /// the workspace folder (contains <c>wally-config.json</c>), or the project folder.
        /// </summary>
        public void LoadFrom(string path)
        {
            path = Path.GetFullPath(path);
            string directConfig = Path.Combine(path, WallyHelper.ConfigFileName);

            if (File.Exists(directConfig))
            {
                WorkspaceFolder = path;
                ParentFolder    = Path.GetDirectoryName(path) ?? path;
                Config          = WallyConfig.LoadFromFile(directConfig);
                ProjectFolder   = Path.Combine(ParentFolder, Config.ProjectFolderName);
            }
            else
            {
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

            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
        }

        // ?? Saving ????????????????????????????????????????????????????????????

        /// <summary>
        /// Persists <c>wally-config.json</c> and writes every actor's prompt files back
        /// to its folder so the on-disk state matches in-memory state.
        /// </summary>
        public void Save()
        {
            RequireLoaded();
            Directory.CreateDirectory(WorkspaceFolder);
            Config.SaveToFile(Path.Combine(WorkspaceFolder, WallyHelper.ConfigFileName));

            foreach (var actor in Actors)
                WallyHelper.SaveActor(WorkspaceFolder, Config, actor);
        }

        // ?? Reference management ??????????????????????????????????????????????

        public void AddFolderReference(string folderPath)
        {
            string absolute = ResolveAbsolute(folderPath);
            if (!FolderReferences.Contains(absolute))
                FolderReferences.Add(absolute);
        }

        public bool RemoveFolderReference(string folderPath) =>
            FolderReferences.Remove(ResolveAbsolute(folderPath));

        public void AddFileReference(string filePath)
        {
            string absolute = ResolveAbsolute(filePath);
            if (!FileReferences.Contains(absolute))
                FileReferences.Add(absolute);
        }

        public bool RemoveFileReference(string filePath) =>
            FileReferences.Remove(ResolveAbsolute(filePath));

        public void ClearReferences()
        {
            FolderReferences.Clear();
            FileReferences.Clear();
        }

        // ?? Actor management ??????????????????????????????????????????????????

        public void AddActor(Actor actor) => Actors.Add(actor);

        public T GetActor<T>() where T : Actor => Actors.Find(a => a is T) as T;

        /// <summary>
        /// Re-reads all agent folders from disk and rebuilds <see cref="Actors"/>.
        /// Use after adding or editing agent folders on disk mid-session.
        /// </summary>
        public void ReloadAgents()
        {
            RequireLoaded();
            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
        }

        // ?? Guard ?????????????????????????????????????????????????????????????

        public void RequireLoaded()
        {
            if (!IsLoaded)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'load <path>' or 'create <path>' first.");
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private string ResolveAbsolute(string path) =>
            Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(ProjectFolder, path));
    }
}
