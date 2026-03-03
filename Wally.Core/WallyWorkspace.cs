using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Wally.Core.Actors;
using Wally.Core.Providers;

namespace Wally.Core
{
    /// <summary>
    /// Represents a Wally workspace bound to a folder on disk.
    ///
    /// The workspace model is built around two directories:
    /// <list type="bullet">
    ///   <item><b>WorkSource</b> — the root of the user's codebase (e.g. <c>C:\repos\MyApp</c>).
    ///         This is the directory whose files provide context to the LLM provider.</item>
    ///   <item><b>WorkspaceFolder</b> — the <c>.wally/</c> folder inside the WorkSource that
    ///         holds config, actor definitions, loop definitions, and LLM wrapper definitions.</item>
    /// </list>
    ///
    /// <code>
    ///   &lt;WorkSource&gt;/                e.g. C:\repos\MyApp
    ///       .wally/                      WorkspaceFolder
    ///           wally-config.json
    ///           Actors/
    ///               &lt;ActorName&gt;/       one folder per actor
    ///                   actor.json       name, rolePrompt, criteriaPrompt, intentPrompt
    ///                   Docs/            actor-private documentation
    ///           Docs/                    workspace-level documentation
    ///           Templates/               document templates
    ///           Loops/                   loop definition JSON files
    ///           Providers/               LLM wrapper definition JSON files
    ///           Logs/                    session logs
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

        /// <summary>
        /// The absolute path to the WorkSource directory — the root of the user's
        /// codebase.  This is the parent of <see cref="WorkspaceFolder"/>.
        /// </summary>
        public string WorkSource { get; private set; }

        /// <summary>
        /// The directory whose contents provide file context to the LLM provider.
        /// Always resolves to <see cref="WorkSource"/>.
        /// </summary>
        public string SourcePath => WorkSource;

        public bool IsLoaded => !string.IsNullOrEmpty(WorkspaceFolder);

        // — Configuration —————————————————————————————————————————————————————

        public WallyConfig Config { get; private set; } = new WallyConfig();

        // — Actor list ————————————————————————————————————————————————————————

        /// <summary>
        /// One <see cref="Actor"/> per actor folder under <c>Actors/</c>.
        /// Each actor carries its own private RBA prompts and documentation context.
        /// Actors are pure data — LLM execution is handled by
        /// <see cref="WallyEnvironment.ExecuteActor"/>.
        /// </summary>
        [JsonIgnore]
        public List<Actor> Actors { get; private set; } = new();

        /// <summary>
        /// Loop definitions loaded from the <c>Loops/</c> folder.
        /// Each defines a reusable iterative execution pattern.
        /// </summary>
        [JsonIgnore]
        public List<WallyLoopDefinition> Loops { get; private set; } = new();

        /// <summary>
        /// LLM wrappers loaded from the <c>Providers/</c> folder.
        /// Each defines a complete CLI recipe for calling an LLM backend.
        /// </summary>
        [JsonIgnore]
        public List<LlmWrapper> LlmWrappers { get; private set; } = new();

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
            WorkSource = Path.GetDirectoryName(WorkspaceFolder)!;
            LlmWrappers = WallyHelper.LoadLlmWrappers(WorkspaceFolder, Config);
            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
            Loops = WallyHelper.LoadLoopDefinitions(WorkspaceFolder, Config);
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
        /// Also reloads LLM wrappers to pick up changes.
        /// Use after adding or editing actor folders on disk mid-session.
        /// </summary>
        public void ReloadActors()
        {
            RequireLoaded();
            LlmWrappers = WallyHelper.LoadLlmWrappers(WorkspaceFolder, Config);
            Actors = WallyHelper.LoadActors(WorkspaceFolder, Config, this);
        }

        /// <summary>
        /// Re-reads all loop definition files from disk and rebuilds <see cref="Loops"/>.
        /// </summary>
        public void ReloadLoops()
        {
            RequireLoaded();
            Loops = WallyHelper.LoadLoopDefinitions(WorkspaceFolder, Config);
        }

        /// <summary>
        /// Re-reads all LLM wrapper files from disk and rebuilds
        /// <see cref="LlmWrappers"/>.
        /// </summary>
        public void ReloadProviders()
        {
            RequireLoaded();
            LlmWrappers = WallyHelper.LoadLlmWrappers(WorkspaceFolder, Config);
        }

        // — Guard ———————————————————————————————————————————————————————————­

        public void RequireLoaded()
        {
            if (!IsLoaded)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'setup <path>' or 'load <path>' first.");
        }
    }
}
