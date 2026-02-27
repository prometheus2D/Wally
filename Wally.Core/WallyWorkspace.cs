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
    ///   &lt;WorkspaceFolder&gt;/          e.g. ".wally/"
    ///       wally-config.json
    ///       Actors/
    ///           &lt;ActorName&gt;/         one folder per actor
    ///               actor.json         name, rolePrompt, criteriaPrompt, intentPrompt
    /// </code>
    ///
    /// Pass the workspace folder path directly to <see cref="Load"/> or
    /// <see cref="LoadFrom"/>. No parent-folder discovery is performed.
    /// </summary>
    public class WallyWorkspace
    {
        // — Identity ——————————————————————————————————————————————————————————

        /// <summary>The absolute path to the workspace folder (e.g. <c>/repo/.wally</c>).</summary>
        public string WorkspaceFolder { get; private set; }

        /// <summary>The absolute path to the project folder (parent of workspace folder).</summary>
        public string ProjectFolder { get; private set; }

        /// <summary>
        /// The directory whose contents provide file context to <c>gh copilot</c>.
        /// Resolved from <see cref="WallyConfig.SourcePath"/> when set; otherwise
        /// falls back to <see cref="ProjectFolder"/>.
        /// </summary>
        public string SourcePath =>
            !string.IsNullOrWhiteSpace(Config.SourcePath)
                ? Path.GetFullPath(Config.SourcePath)
                : ProjectFolder;

        public bool IsLoaded => !string.IsNullOrEmpty(WorkspaceFolder);

        // — Configuration —————————————————————————————————————————————————————

        public WallyConfig Config { get; private set; } = new WallyConfig();

        // — Actor list ————————————————————————————————————————————————————————

        /// <summary>
        /// One <see cref="CopilotActor"/> per actor folder under <c>Actors/</c>.
        /// Each actor carries its own private RBA — no shared state.
        /// </summary>
        [JsonIgnore]
        public List<Actor> Actors { get; private set; } = new();

        // — Static factory ————————————————————————————————————————————————————

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
        /// Config is resolved using the standard fallback chain:
        /// workspace-local ? shipped template ? hard-coded defaults.
        /// </summary>
        public void LoadFrom(string workspaceFolder)
        {
            workspaceFolder = Path.GetFullPath(workspaceFolder);
            Directory.CreateDirectory(workspaceFolder);

            Config = WallyHelper.ResolveConfig(workspaceFolder);

            WorkspaceFolder = workspaceFolder;
            ProjectFolder = Path.GetDirectoryName(WorkspaceFolder);
            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
        }

        // — Saving ———————————————————————————————————————————————————————————

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

        // — Actor management ———————————————————————————————————————————————————

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

        // — Guard ————————————————————————————————————————————————————————————

        public void RequireLoaded()
        {
            if (!IsLoaded)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'load <path>' or 'create <path>' first.");
        }
    }
}
