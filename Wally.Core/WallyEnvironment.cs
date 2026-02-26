using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Wally.Core.Actors;

namespace Wally.Core
{
    /// <summary>
    /// Runtime environment for Wally. Holds a reference to the active workspace and exposes
    /// actor-execution and workspace-management operations.
    /// Once loaded, three sibling folders are accessible: ParentFolder, ProjectFolder, WorkspaceFolder.
    /// </summary>
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

        // ?? Reference pass-throughs ???????????????????????????????????????????

        [JsonIgnore] public List<string> FolderReferences => Workspace?.FolderReferences ?? _emptyStrings;
        [JsonIgnore] public List<string> FileReferences   => Workspace?.FileReferences   ?? _emptyStrings;
        private static readonly List<string> _emptyStrings = new();

        [JsonIgnore] public List<Actor> Actors => Workspace?.Actors ?? _emptyActors;
        private static readonly List<Actor> _emptyActors = new();

        // ?? Workspace lifecycle ???????????????????????????????????????????????

        /// <summary>Loads an existing workspace from <paramref name="parentFolder"/>.</summary>
        public void LoadWorkspace(string parentFolder) =>
            Workspace = WallyWorkspace.Load(parentFolder);

        /// <summary>Scaffolds a new workspace under <paramref name="parentFolder"/> then loads it.</summary>
        public void CreateWorkspace(string parentFolder, WallyConfig config = null)
        {
            WallyHelper.CreateDefaultWorkspace(parentFolder, config);
            LoadWorkspace(parentFolder);
        }

        /// <summary>
        /// Self-assembly: uses the exe directory as the parent folder, scaffolding if needed.
        /// </summary>
        public void SetupLocal()
        {
            string parentFolder = WallyHelper.GetDefaultParentFolder();
            WallyConfig config  = WallyHelper.ResolveConfig();

            string expectedConfig = Path.Combine(
                parentFolder, config.WorkspaceFolderName, WallyHelper.ConfigFileName);

            if (!File.Exists(expectedConfig))
                WallyHelper.CreateDefaultWorkspace(parentFolder, config);

            LoadWorkspace(parentFolder);
        }

        /// <summary>Saves the current workspace config back to disk.</summary>
        public void SaveWorkspace() => RequireWorkspace().Save();

        // ?? Legacy compatibility ??????????????????????????????????????????????

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

        public void LoadDefaultActors(string jsonPath)
        {
            var ws   = RequireWorkspace();
            string json = File.ReadAllText(jsonPath);
            var data = System.Text.Json.JsonSerializer.Deserialize<DefaultActorsData>(json);
            if (data != null)
                foreach (var role in data.Roles)
                    foreach (var criteria in data.AcceptanceCriterias)
                        foreach (var intent in data.Intents)
                            ws.AddActor(new WallyActor(role, criteria, intent, ws));
        }

        /// <summary>
        /// Loads the default actor definitions from <c>default-agents.json</c>, resolving
        /// the file from the workspace folder or the exe directory automatically.
        /// Throws <see cref="FileNotFoundException"/> when the file cannot be located.
        /// </summary>
        public void LoadDefaultActors()
        {
            var ws   = RequireWorkspace();
            string? path = WallyHelper.ResolveDefaultAgentsPath(ws.WorkspaceFolder);
            if (path == null)
                throw new FileNotFoundException(
                    $"Could not find '{WallyHelper.DefaultAgentsFileName}' in the workspace " +
                    $"folder or the exe directory. Run 'setup' first or pass an explicit path.");
            LoadDefaultActors(path);
        }

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
                    responses.Add($"{actor.GetType().Name}: {response}");
            }
            return responses;
        }

        public List<string> RunActor(string prompt, string actorName)
        {
            RequireWorkspace();
            var actor = Actors.Find(a => a.GetType().Name == actorName);
            if (actor == null)
                return new List<string> { $"Actor '{actorName}' not found." };

            string response = actor.Act(prompt);
            return response != null
                ? new List<string> { $"{actor.GetType().Name}: {response}" }
                : new List<string>();
        }

        public List<string> RunActorsIterative(string initialPrompt,
            Action<int, List<string>>? onIteration = null)
        {
            RequireWorkspace();
            string currentPrompt    = initialPrompt;
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