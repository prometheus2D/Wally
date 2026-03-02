using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Wally.Core.Actors;
using Wally.Core.Logging;

namespace Wally.Core
{
    public class WallyEnvironment
    {
        // — Active workspace ——————————————————————————————————————————————————

        [JsonIgnore]
        public WallyWorkspace? Workspace { get; private set; }

        public bool HasWorkspace => Workspace?.IsLoaded == true;

        // — Session logging ——————————————————————————————————————————————————

        /// <summary>
        /// The logger for this environment lifetime. Created once at construction;
        /// bound to the workspace Logs folder the first time a workspace is loaded.
        /// </summary>
        [JsonIgnore]
        public SessionLogger Logger { get; }

        // — Folder pass-throughs ——————————————————————————————————————————————

        /// <summary>The workspace folder path (e.g. <c>/repo/.wally</c>).</summary>
        public string? WorkspaceFolder => Workspace?.WorkspaceFolder;

        /// <summary>
        /// The WorkSource directory — the root of the user's codebase.
        /// This is the parent of the <c>.wally/</c> workspace folder and the
        /// directory whose files provide context to <c>gh copilot</c>.
        /// </summary>
        public string? WorkSource => HasWorkspace ? Workspace!.WorkSource : null;

        /// <summary>
        /// The source directory whose files provide context to <c>gh copilot</c>.
        /// Delegates to <see cref="WallyWorkspace.SourcePath"/> (which is <see cref="WorkSource"/>).
        /// </summary>
        public string? SourcePath => HasWorkspace ? Workspace!.SourcePath : null;

        // — Pass-throughs —————————————————————————————————————————————————————

        [JsonIgnore] public List<Actor> Actors => Workspace?.Actors ?? _emptyActors;
        private static readonly List<Actor> _emptyActors = new();

        // — Constructor ——————————————————————————————————————————————————————

        public WallyEnvironment()
        {
            Logger = new SessionLogger();
        }

        // — Workspace lifecycle ———————————————————————————————————————————————

        /// <summary>Loads the workspace at <paramref name="workspaceFolder"/>.</summary>
        public void LoadWorkspace(string workspaceFolder)
        {
            Workspace = WallyWorkspace.Load(workspaceFolder);
            BindLogger();
            InjectLoggerIntoActors();
        }

        /// <summary>
        /// Ensures a workspace exists at the given <paramref name="workSourcePath"/>.
        /// <para>
        /// When <paramref name="workSourcePath"/> is supplied, the workspace folder is
        /// <c>&lt;workSourcePath&gt;/.wally</c>.  When <see langword="null"/>, the default
        /// workspace folder next to the exe is used.
        /// </para>
        /// If the workspace folder does not yet contain a config, a default workspace is
        /// scaffolded from the shipped template.
        /// </summary>
        public void SetupLocal(string workSourcePath = null)
        {
            string workspaceFolder;
            if (workSourcePath != null)
            {
                // If the path is not rooted (e.g. "workspace", "my-project"),
                // treat it as a subdirectory of the exe directory.
                if (!Path.IsPathRooted(workSourcePath))
                    workSourcePath = Path.Combine(WallyHelper.GetExeDirectory(), workSourcePath);

                workSourcePath = Path.GetFullPath(workSourcePath);
                Directory.CreateDirectory(workSourcePath);
                workspaceFolder = Path.Combine(workSourcePath, WallyHelper.DefaultWorkspaceFolderName);
            }
            else
            {
                workspaceFolder = WallyHelper.GetDefaultWorkspaceFolder();
            }

            WallyConfig config = WallyHelper.ResolveConfig(workspaceFolder);

            string configPath = Path.Combine(workspaceFolder, WallyHelper.ConfigFileName);
            if (!File.Exists(configPath))
                WallyHelper.CreateDefaultWorkspace(workspaceFolder, config);

            LoadWorkspace(workspaceFolder);
        }

        /// <summary>
        /// Saves the current workspace. If <paramref name="workspaceFolder"/> matches the
        /// loaded workspace, saves in place. Otherwise scaffolds a new workspace at that
        /// path and loads it.
        /// </summary>
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
                // Scaffold at the new location and load it.
                string workSourcePath = Path.GetDirectoryName(Path.GetFullPath(workspaceFolder))!;
                SetupLocal(workSourcePath);
                Workspace!.Save();
            }
        }

        public void SaveWorkspace() => RequireWorkspace().Save();

        /// <summary>
        /// Re-reads all actor folders from disk and rebuilds the actor list.
        /// </summary>
        public void ReloadActors()
        {
            RequireWorkspace().ReloadActors();
            InjectLoggerIntoActors();
        }

        // — Actor management ——————————————————————————————————————————————————

        /// <summary>
        /// Returns the actor whose name matches <paramref name="name"/> (case-insensitive),
        /// or <see langword="null"/>.
        /// </summary>
        public Actor? GetActor(string name) =>
            Actors.FirstOrDefault(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns the first actor of type <typeparamref name="T"/>, or <see langword="null"/>.
        /// </summary>
        public T? GetActor<T>() where T : Actor => Workspace?.GetActor<T>();

        // — Running actors ————————————————————————————————————————————————————

        public List<string> RunActors(string prompt)
        {
            RequireWorkspace();
            var responses = new List<string>();
            foreach (var actor in Actors)
            {
                Logger.LogPrompt(actor.Name, prompt, actor.ModelOverride ?? Workspace!.Config.DefaultModel);
                var sw = Stopwatch.StartNew();

                string response = actor.Act(prompt);

                sw.Stop();
                Logger.LogResponse(actor.Name, response, sw.ElapsedMilliseconds);

                if (response != null)
                    responses.Add($"[Role: {actor.Role.Name}]\n{response}");
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
            {
                Logger.LogError($"Actor '{actorName}' not found.", "run");
                return new List<string> { $"Actor '{actorName}' not found." };
            }

            Logger.LogPrompt(actor.Name, prompt, actor.ModelOverride ?? Workspace!.Config.DefaultModel);
            var sw = Stopwatch.StartNew();

            string response = actor.Act(prompt);

            sw.Stop();
            Logger.LogResponse(actor.Name, response, sw.ElapsedMilliseconds);

            return response != null
                ? new List<string> { $"[Role: {actor.Role.Name}]\n{response}" }
                : new List<string>();
        }

        // — Guard —————————————————————————————————————————————————————————————

        public WallyWorkspace RequireWorkspace()
        {
            if (!HasWorkspace)
                throw new InvalidOperationException(
                    "No workspace is loaded. Use 'setup <path>' or 'load <path>' first.");
            return Workspace!;
        }

        // — Static factory ————————————————————————————————————————————————————

        public static WallyEnvironment LoadDefault() => WallyHelper.LoadDefault();

        // — Private helpers ———————————————————————————————————————————————————

        /// <summary>
        /// Binds the session logger to the current workspace's Logs folder.
        /// Creates the Logs directory on disk if it doesn't exist.
        /// Safe to call multiple times — only the first bind takes effect
        /// (subsequent workspace reloads reuse the same session log folder).
        /// </summary>
        private void BindLogger()
        {
            if (!HasWorkspace || Logger.LogFolder != null) return;

            Logger.RotationMinutes = Workspace!.Config.LogRotationMinutes;
            Logger.Bind(Workspace.WorkspaceFolder, Workspace.Config.LogsFolderName);
            Logger.LogInfo($"Session started — workspace: {Workspace.WorkspaceFolder}");
        }

        /// <summary>
        /// Sets <see cref="SessionLogger"/> on every loaded actor so the actor
        /// pipeline can log processed prompts and CLI interactions.
        /// </summary>
        private void InjectLoggerIntoActors()
        {
            foreach (var actor in Actors)
                actor.Logger = Logger;
        }
    }
}