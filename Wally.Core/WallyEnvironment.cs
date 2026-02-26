using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Wally.Core.Actors;

namespace Wally.Core
{
    public class WallyEnvironment
    {
        // ?? Active workspace ??????????????????????????????????????????????????

        [JsonIgnore]
        public WallyWorkspace? Workspace { get; private set; }

        public bool HasWorkspace => Workspace?.IsLoaded == true;

        // ?? Folder pass-throughs ??????????????????????????????????????????????

        public string? ParentFolder    => Workspace?.ParentFolder;
        public string? ProjectFolder   => Workspace?.ProjectFolder;
        public string? WorkspaceFolder => Workspace?.WorkspaceFolder;

        // ?? Runtime settings ??????????????????????????????????????????????????

        public int MaxIterations
        {
            get => HasWorkspace ? Workspace!.Config.MaxIterations : _maxIterations;
            set
            {
                _maxIterations = value;
                if (HasWorkspace) Workspace!.Config.MaxIterations = value;
            }
        }
        private int _maxIterations = 10;

        // ?? Pass-throughs ?????????????????????????????????????????????????????

        [JsonIgnore] public List<string> FolderReferences => Workspace?.FolderReferences ?? _emptyStrings;
        [JsonIgnore] public List<string> FileReferences   => Workspace?.FileReferences   ?? _emptyStrings;
        private static readonly List<string> _emptyStrings = new();

        [JsonIgnore] public List<Actor> Actors => Workspace?.Actors ?? _emptyActors;
        private static readonly List<Actor> _emptyActors = new();

        [JsonIgnore]
        public IReadOnlyList<AgentDefinition> AgentDefinitions =>
            Workspace?.AgentDefinitions ?? Array.Empty<AgentDefinition>();

        // ?? Workspace lifecycle ???????????????????????????????????????????????

        public void LoadWorkspace(string parentFolder) =>
            Workspace = WallyWorkspace.Load(parentFolder);

        public void CreateWorkspace(string parentFolder, WallyConfig config = null)
        {
            WallyHelper.CreateDefaultWorkspace(parentFolder, config);
            LoadWorkspace(parentFolder);
        }

        public void SetupLocal(string parentFolder = null)
        {
            parentFolder ??= WallyHelper.GetDefaultParentFolder();
            WallyConfig config  = WallyHelper.ResolveConfig();

            string expectedConfig = Path.Combine(
                parentFolder, config.WorkspaceFolderName, WallyHelper.ConfigFileName);

            if (!File.Exists(expectedConfig))
                WallyHelper.CreateDefaultWorkspace(parentFolder, config);

            LoadWorkspace(parentFolder);
        }

        public void SaveWorkspace() => RequireWorkspace().Save();

        // ?? Legacy compat ?????????????????????????????????????????????????????

        public void LoadFromWorkspace(string path) => LoadWorkspace(path);

        public void SaveToWorkspace(string path)
        {
            if (HasWorkspace && string.Equals(
                    Path.GetFullPath(Workspace!.ParentFolder),
                    Path.GetFullPath(path),
                    StringComparison.OrdinalIgnoreCase))
            {
                Workspace.Save();
            }
            else
            {
                WallyHelper.CreateDefaultWorkspace(path);
                LoadWorkspace(path);
            }
        }

        /// <summary>
        /// Re-reads all agent folders from disk and rebuilds the actor list.
        /// Use after adding or editing agent folders without reloading the whole workspace.
        /// </summary>
        public void ReloadAgents() => RequireWorkspace().ReloadAgents();

        // ?? Agent management ??????????????????????????????????????????????????

        /// <summary>
        /// Returns the <see cref="AgentDefinition"/> whose name matches
        /// <paramref name="name"/> (case-insensitive), or <see langword="null"/>.
        /// </summary>
        public AgentDefinition? GetAgent(string name) =>
            AgentDefinitions.FirstOrDefault(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        // ?? Reference management ??????????????????????????????????????????????

        public void AddFolderReference(string folderPath) =>
            RequireWorkspace().AddFolderReference(folderPath);

        public void AddFileReference(string filePath) =>
            RequireWorkspace().AddFileReference(filePath);

        public bool RemoveFolderReference(string folderPath) =>
            RequireWorkspace().RemoveFolderReference(folderPath);

        public bool RemoveFileReference(string filePath) =>
            RequireWorkspace().RemoveFileReference(filePath);

        public void ClearReferences() => RequireWorkspace().ClearReferences();

        // ?? Actor management ??????????????????????????????????????????????????

        public T? GetActor<T>() where T : Actor => Workspace?.GetActor<T>();

        // ?? Running actors ????????????????????????????????????????????????????

        public List<string> RunActors(string prompt)
        {
            RequireWorkspace();
            var responses = new List<string>();
            foreach (var actor in Actors)
            {
                string response = actor.Act(prompt);
                if (response != null)
                    responses.Add($"{actor.Role.Name}: {response}");
            }
            return responses;
        }

        /// <summary>
        /// Runs the actor whose agent name (Role.Name) or type name matches
        /// <paramref name="actorName"/> (case-insensitive).
        /// </summary>
        public List<string> RunActor(string prompt, string actorName)
        {
            RequireWorkspace();
            var actor = Actors.Find(a =>
                string.Equals(a.Role.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.GetType().Name, actorName, StringComparison.OrdinalIgnoreCase));

            if (actor == null)
                return new List<string> { $"Actor '{actorName}' not found." };

            string response = actor.Act(prompt);
            return response != null
                ? new List<string> { $"{actor.Role.Name}: {response}" }
                : new List<string>();
        }

        /// <summary>
        /// Runs all actors iteratively up to <see cref="MaxIterations"/> times, feeding
        /// the combined responses of each iteration back as the next prompt.
        /// </summary>
        public List<string> RunActorsIterative(string initialPrompt,
            Action<int, List<string>>? onIteration = null)
        {
            RequireWorkspace();
            string currentPrompt       = initialPrompt;
            List<string> lastResponses = new List<string>();

            for (int i = 1; i <= MaxIterations; i++)
            {
                lastResponses = RunActors(currentPrompt);
                onIteration?.Invoke(i, lastResponses);
                if (lastResponses.Count == 0) break;
                currentPrompt = string.Join(Environment.NewLine, lastResponses);
            }
            return lastResponses;
        }

        /// <summary>
        /// Runs a single named actor iteratively, feeding each response back as the next
        /// prompt until <paramref name="maxIterationsOverride"/> (or <see cref="MaxIterations"/>)
        /// is reached or the actor returns an empty response. Agent context is re-applied at
        /// every step via <see cref="Actor.ProcessPrompt"/>.
        /// </summary>
        /// <param name="prompt">The initial prompt.</param>
        /// <param name="actorName">Role.Name or type name of the actor to run (case-insensitive).</param>
        /// <param name="maxIterationsOverride">Cap for this run; 0 = use <see cref="MaxIterations"/>.</param>
        /// <param name="onIteration">
        /// Optional callback invoked after each iteration with the 1-based index and response.
        /// </param>
        /// <returns>The final non-empty response, or an error string when the actor is not found.</returns>
        public string RunActorIterative(string prompt, string actorName,
            int maxIterationsOverride = 0, Action<int, string>? onIteration = null)
        {
            RequireWorkspace();

            var actor = Actors.Find(a =>
                string.Equals(a.Role.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.GetType().Name, actorName, StringComparison.OrdinalIgnoreCase));

            if (actor == null)
                return $"Actor '{actorName}' not found.";

            int cap           = maxIterationsOverride > 0 ? maxIterationsOverride : MaxIterations;
            string current    = prompt;
            string last       = string.Empty;

            for (int i = 1; i <= cap; i++)
            {
                string response = actor.Act(current);

                if (string.IsNullOrWhiteSpace(response))
                {
                    onIteration?.Invoke(i, string.Empty);
                    break;
                }

                onIteration?.Invoke(i, response);
                last    = response;
                current = actor.ProcessPrompt(response);
            }

            return last;
        }

        // ?? Guard ?????????????????????????????????????????????????????????????

        public WallyWorkspace RequireWorkspace()
        {
            if (!HasWorkspace)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'load <path>' or 'create <path>' first.");
            return Workspace!;
        }

        // ?? Static factory helpers ????????????????????????????????????????????

        public static WallyEnvironment LoadDefault() => WallyHelper.LoadDefault();

        public static void CreateDefaultWorkspace(string parentFolder, WallyConfig config = null) =>
            WallyHelper.CreateDefaultWorkspace(parentFolder, config);
    }
}