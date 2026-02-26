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

        /// <summary>The workspace folder path (e.g. <c>/repo/.wally</c>).</summary>
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

        // ?? Workspace lifecycle ???????????????????????????????????????????????

        /// <summary>Loads the workspace at <paramref name="workspaceFolder"/>.</summary>
        public void LoadWorkspace(string workspaceFolder) =>
            Workspace = WallyWorkspace.Load(workspaceFolder);

        /// <summary>
        /// Scaffolds a new workspace at <paramref name="workspaceFolder"/> then loads it.
        /// </summary>
        public void CreateWorkspace(string workspaceFolder, WallyConfig config = null)
        {
            WallyHelper.CreateDefaultWorkspace(workspaceFolder, config);
            LoadWorkspace(workspaceFolder);
        }

        /// <summary>
        /// Ensures a workspace exists at <paramref name="workspaceFolder"/> (or the default
        /// <c>.wally</c> folder next to the exe when <see langword="null"/>), then loads it.
        /// </summary>
        public void SetupLocal(string workspaceFolder = null)
        {
            workspaceFolder ??= WallyHelper.GetDefaultWorkspaceFolder();
            WallyConfig config = WallyHelper.ResolveConfig(workspaceFolder);

            string configPath = Path.Combine(workspaceFolder, WallyHelper.ConfigFileName);
            if (!File.Exists(configPath))
                WallyHelper.CreateDefaultWorkspace(workspaceFolder, config);

            LoadWorkspace(workspaceFolder);
        }

        public void SaveWorkspace() => RequireWorkspace().Save();

        // ?? Legacy compat ?????????????????????????????????????????????????????

        /// <summary>Alias for <see cref="LoadWorkspace"/>.</summary>
        public void LoadFromWorkspace(string workspaceFolder) => LoadWorkspace(workspaceFolder);

        public void SaveToWorkspace(string workspaceFolder)
        {
            if (HasWorkspace && string.Equals(
                    Path.GetFullPath(Workspace!.WorkspaceFolder),
                    Path.GetFullPath(workspaceFolder),
                    StringComparison.OrdinalIgnoreCase))
            {
                Workspace.Save();
            }
            else
            {
                WallyHelper.CreateDefaultWorkspace(workspaceFolder);
                LoadWorkspace(workspaceFolder);
            }
        }

        /// <summary>
        /// Re-reads all actor folders from disk and rebuilds the actor list.
        /// </summary>
        public void ReloadActors() => RequireWorkspace().ReloadActors();

        // ?? Agent management ??????????????????????????????????????????????????

        /// <summary>
        /// Returns the actor whose name matches <paramref name="name"/> (case-insensitive),
        /// or <see langword="null"/>.
        /// </summary>
        public Actor? GetAgent(string name) =>
            Actors.FirstOrDefault(a =>
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

        public List<string> RunActor(string prompt, string actorName)
        {
            RequireWorkspace();
            var actor = Actors.Find(a =>
                string.Equals(a.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Role.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.GetType().Name, actorName, StringComparison.OrdinalIgnoreCase));

            if (actor == null)
                return new List<string> { $"Actor '{actorName}' not found." };

            string response = actor.Act(prompt);
            return response != null
                ? new List<string> { $"{actor.Role.Name}: {response}" }
                : new List<string>();
        }

        public List<string> RunActorsIterative(string initialPrompt,
            Action<int, List<string>>? onIteration = null)
        {
            RequireWorkspace();
            string currentPrompt       = initialPrompt;
            List<string> lastResponses = new();

            for (int i = 1; i <= MaxIterations; i++)
            {
                lastResponses = RunActors(currentPrompt);
                onIteration?.Invoke(i, lastResponses);
                if (lastResponses.Count == 0) break;
                currentPrompt = string.Join(Environment.NewLine, lastResponses);
            }
            return lastResponses;
        }

        public string RunActorIterative(string prompt, string actorName,
            int maxIterationsOverride = 0, Action<int, string>? onIteration = null)
        {
            RequireWorkspace();

            var actor = Actors.Find(a =>
                string.Equals(a.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Role.Name, actorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.GetType().Name, actorName, StringComparison.OrdinalIgnoreCase));

            if (actor == null) return $"Actor '{actorName}' not found.";

            int cap        = maxIterationsOverride > 0 ? maxIterationsOverride : MaxIterations;
            string current = prompt;
            string last    = string.Empty;

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

        public static void CreateDefaultWorkspace(string workspaceFolder, WallyConfig config = null) =>
            WallyHelper.CreateDefaultWorkspace(workspaceFolder, config);
    }
}