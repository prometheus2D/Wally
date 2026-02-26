using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Wally.Core.Actors;

namespace Wally.Core
{
    /// <summary>
    /// Represents a Wally workspace bound to a folder on disk.
    ///
    /// The workspace folder is self-contained — everything Wally needs lives inside it:
    /// <code>
    ///   &lt;WorkspaceFolder&gt;/          ? e.g. ".wally/"
    ///       wally-config.json
    ///       Actors/
    ///           &lt;ActorName&gt;/         ? one folder per actor
    ///               actor.json         ? full RBA definition
    /// </code>
    ///
    /// Pass the workspace folder path directly to <see cref="Load"/> or
    /// <see cref="LoadFrom"/>. No parent-folder discovery is performed.
    /// </summary>
    public class WallyWorkspace
    {
        // ?? Identity ??????????????????????????????????????????????????????????

        /// <summary>The absolute path to the workspace folder (e.g. <c>/repo/.wally</c>).</summary>
        public string WorkspaceFolder { get; private set; }

        public bool IsLoaded => !string.IsNullOrEmpty(WorkspaceFolder);

        // ?? Configuration ?????????????????????????????????????????????????????

        public WallyConfig Config { get; private set; } = new WallyConfig();

        // ?? References ????????????????????????????????????????????????????????

        public List<string> FolderReferences { get; private set; } = new();
        public List<string> FileReferences   { get; private set; } = new();

        // ?? Actor list ????????????????????????????????????????????????????????

        /// <summary>
        /// One <see cref="CopilotActor"/> per actor folder under <c>Actors/</c>.
        /// Each actor carries its own private RBA — no shared state.
        /// </summary>
        [JsonIgnore]
        public List<Actor> Actors { get; private set; } = new();

        // ?? Static factory ????????????????????????????????????????????????????

        /// <summary>Loads the workspace at <paramref name="workspaceFolder"/>.</summary>
        public static WallyWorkspace Load(string workspaceFolder)
        {
            var ws = new WallyWorkspace();
            ws.LoadFrom(workspaceFolder);
            return ws;
        }

        /// <summary>
        /// Initialises from <paramref name="workspaceFolder"/>.
        /// The folder is created on disk if it does not yet exist.
        /// </summary>
        public void LoadFrom(string workspaceFolder)
        {
            workspaceFolder = Path.GetFullPath(workspaceFolder);
            Directory.CreateDirectory(workspaceFolder);

            string configPath = Path.Combine(workspaceFolder, WallyHelper.ConfigFileName);
            Config = File.Exists(configPath)
                ? WallyConfig.LoadFromFile(configPath)
                : new WallyConfig();

            WorkspaceFolder = workspaceFolder;
            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
        }

        // ?? Saving ????????????????????????????????????????????????????????????

        /// <summary>
        /// Persists <c>wally-config.json</c> and writes every actor's <c>actor.json</c>
        /// back to its folder so the on-disk state matches in-memory state.
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
            string absolute = Path.GetFullPath(folderPath);
            if (!FolderReferences.Contains(absolute))
                FolderReferences.Add(absolute);
        }

        public bool RemoveFolderReference(string folderPath) =>
            FolderReferences.Remove(Path.GetFullPath(folderPath));

        public void AddFileReference(string filePath)
        {
            string absolute = Path.GetFullPath(filePath);
            if (!FileReferences.Contains(absolute))
                FileReferences.Add(absolute);
        }

        public bool RemoveFileReference(string filePath) =>
            FileReferences.Remove(Path.GetFullPath(filePath));

        public void ClearReferences()
        {
            FolderReferences.Clear();
            FileReferences.Clear();
        }

        // ?? Actor management ??????????????????????????????????????????????????

        public void AddActor(Actor actor) => Actors.Add(actor);

        public T GetActor<T>() where T : Actor => Actors.Find(a => a is T) as T;

        /// <summary>
        /// Re-reads all actor folders from disk and rebuilds <see cref="Actors"/>.
        /// Use after adding or editing actor folders on disk mid-session.
        /// </summary>
        public void ReloadActors()
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
    }
}
