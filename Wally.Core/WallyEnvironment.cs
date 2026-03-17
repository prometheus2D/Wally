using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Wally.Core.Actors;
using Wally.Core.Logging;
using Wally.Core.Providers;

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

        // — Conversation history ——————————————————————————————————————————————

        /// <summary>
        /// Ordered conversation history (prompt/response pairs). Created once at
        /// construction; bound to the workspace History folder on workspace load.
        /// </summary>
        [JsonIgnore]
        public ConversationLogger History { get; }

        // — Folder pass-throughs ——————————————————————————————————————————————

        /// <summary>The workspace folder path (e.g. <c>/repo/.wally</c>).</summary>
        public string? WorkspaceFolder => Workspace?.WorkspaceFolder;

        /// <summary>
        /// The WorkSource directory — the root of the user's codebase.
        /// This is the parent of the <c>.wally/</c> workspace folder and the
        /// directory whose files provide context to the LLM wrapper.
        /// </summary>
        public string? WorkSource => HasWorkspace ? Workspace!.WorkSource : null;

        /// <summary>
        /// The source directory whose files provide context to the LLM wrapper.
        /// Delegates to <see cref="WallyWorkspace.SourcePath"/> (which is <see cref="WorkSource"/>).
        /// </summary>
        public string? SourcePath => HasWorkspace ? Workspace!.SourcePath : null;

        // — Pass-throughs ———————————————————————————————————————————————————

        [JsonIgnore] public List<Actor> Actors => Workspace?.Actors ?? _emptyActors;
        private static readonly List<Actor> _emptyActors = new();

        [JsonIgnore] public List<WallyLoopDefinition> Loops => Workspace?.Loops ?? _emptyLoops;
        private static readonly List<WallyLoopDefinition> _emptyLoops = new();

        [JsonIgnore] public List<WallyRunbook> Runbooks => Workspace?.Runbooks ?? _emptyRunbooks;
        private static readonly List<WallyRunbook> _emptyRunbooks = new();

        // — Constructor ——————————————————————————————————————————————————————

        public WallyEnvironment()
        {
            Logger = new SessionLogger();
            History = new ConversationLogger();
        }

        // — Workspace lifecycle ———————————————————————————————————————————————

        /// <summary>Loads the workspace at <paramref name="workspaceFolder"/>.</summary>
        public void LoadWorkspace(string workspaceFolder)
        {
            Workspace = WallyWorkspace.Load(workspaceFolder);
            BindLogger();
            BindHistory();
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
                workSourcePath = Path.GetDirectoryName(workspaceFolder)!;
            }

            WallyConfig config = WallyHelper.ResolveConfig(workspaceFolder);

            string configPath = Path.Combine(workspaceFolder, WallyHelper.ConfigFileName);
            if (!File.Exists(configPath))
                WallyHelper.CreateDefaultWorkspace(workspaceFolder, config);

            // Copy the wally exe + runtime into the WorkSource so the user
            // can run .\wally directly from their codebase root.
            int copied = WallyHelper.CopyExeToWorkSource(workSourcePath);
            if (copied > 0)
                Console.WriteLine($"  Copied {copied} runtime file(s) to {workSourcePath}");

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
        /// Closes the current workspace, resetting the environment to an unloaded state.
        /// The session logger is unbound (file handles released) so the workspace
        /// folder can be safely deleted. The logger remains active — new entries are
        /// buffered in memory until a workspace is loaded again.
        /// </summary>
        public void CloseWorkspace()
        {
            History.Unbind();
            Logger.Unbind();
            Workspace = null;
        }

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

        /// <summary>
        /// Returns the loop definition whose name matches <paramref name="name"/> (case-insensitive),
        /// or <see langword="null"/>.
        /// </summary>
        public WallyLoopDefinition? GetLoop(string name) =>
            Loops.FirstOrDefault(l =>
                string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns the runbook whose name matches <paramref name="name"/> (case-insensitive),
        /// or <see langword="null"/>.
        /// </summary>
        public WallyRunbook? GetRunbook(string name) =>
            Runbooks.FirstOrDefault(r =>
                string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

        // — LLM wrapper resolution ———————————————————————————————————————————

        /// <summary>
        /// Returns the active <see cref="LLMWrapper"/> for this workspace,
        /// resolved from <see cref="WallyConfig.DefaultWrapper"/>.
        /// Throws if no wrapper matches the config.
        /// </summary>
        public LLMWrapper ResolveWrapper() => ResolveWrapper(null);

        /// <summary>
        /// Returns the <see cref="LLMWrapper"/> matching <paramref name="wrapperOverride"/>,
        /// or the workspace default when <paramref name="wrapperOverride"/> is null/empty.
        /// Throws if no wrapper matches.
        /// </summary>
        public LLMWrapper ResolveWrapper(string? wrapperOverride)
        {
            var ws = RequireWorkspace();
            string wrapperName = !string.IsNullOrWhiteSpace(wrapperOverride)
                ? wrapperOverride!
                : ws.Config.DefaultWrapper;

            var wrapper = WallyHelper.ResolveWrapper(wrapperName, ws.LlmWrappers);
            if (wrapper == null)
                throw new InvalidOperationException(
                    $"No LLM wrapper found for '{wrapperName}'. " +
                    $"Check the Wrappers/ folder in your workspace.");
            return wrapper;
        }

        // — Running actors ————————————————————————————————————————————————————

        /// <summary>
        /// Executes a prompt directly through the LLM wrapper without any actor
        /// enrichment. Use this when no actor is specified — the user's prompt is
        /// sent as-is.
        /// </summary>
        /// <param name="prompt">The raw user prompt.</param>
        /// <param name="modelOverride">Model override, or <see langword="null"/> for config default.</param>
        /// <param name="wrapperOverride">Wrapper override, or <see langword="null"/> for config default.</param>
        /// <param name="loopName">Loop name for history metadata, or <see langword="null"/>.</param>
        /// <param name="iteration">0-based iteration index for history metadata.</param>
        /// <param name="skipHistory">
        /// When <see langword="true"/>, suppresses history injection into the prompt
        /// but still records the turn. Used for loop iterations &gt; 1 and <c>--no-history</c>.
        /// </param>
        public string ExecutePrompt(
            string prompt,
            string? modelOverride = null,
            string? wrapperOverride = null,
            string? loopName = null,
            int iteration = 0,
            bool skipHistory = false,
            CancellationToken cancellationToken = default)
        {
            var wrapper = ResolveWrapper(wrapperOverride);
            var ws = Workspace!;

            // Resolve model: explicit override ? config default.
            bool isDefaultKeyword = string.Equals(modelOverride, "default", StringComparison.OrdinalIgnoreCase);
            string? model = !string.IsNullOrWhiteSpace(modelOverride) && !isDefaultKeyword
                ? modelOverride
                : ws.Config.DefaultModel;

            // — History injection (direct mode: prepend to prompt) ————————
            string effectivePrompt = prompt;
            if (!skipHistory && wrapper.UseConversationHistory)
            {
                var recentTurns = History.GetRecentTurns(ConversationLogger.MaxInjectedTurns, null);
                string? historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
                if (historyBlock != null)
                    effectivePrompt = historyBlock + "\n" + prompt;
            }

            Logger.LogProcessedPrompt("(no actor)", effectivePrompt, model);

            // — Execute ———————————————————————————————————————————————————
            var sw = Stopwatch.StartNew();
            string response = wrapper.Execute(effectivePrompt, ws.SourcePath, model, Logger, cancellationToken);
            sw.Stop();

            // — Record turn ——————————————————————————————————————————————
            bool isError = IsWrapperError(response, wrapper.Name);
            History.RecordTurn(new ConversationTurn
            {
                Timestamp   = DateTimeOffset.UtcNow,
                SessionId   = Logger.SessionId.ToString("N"),
                ActorName   = null,
                WrapperName = wrapper.Name,
                Model       = model,
                Prompt      = prompt,
                Response    = response,
                IsError     = isError,
                ElapsedMs   = sw.ElapsedMilliseconds,
                LoopName    = loopName,
                Iteration   = iteration
            });

            return response;
        }

        /// <summary>
        /// Executes a single actor: Setup ? ProcessPrompt ? LlmWrapper.Execute.
        /// </summary>
        public string ExecuteActor(
            Actor actor,
            string prompt,
            string? modelOverride = null,
            string? wrapperOverride = null,
            string? loopName = null,
            int iteration = 0,
            bool skipHistory = false,
            CancellationToken cancellationToken = default)
        {
            var wrapper = ResolveWrapper(wrapperOverride);
            var ws = Workspace!;

            actor.Setup();

            // — History injection (actor mode: pass to ProcessPrompt) —————
            string? historyBlock = null;
            if (!skipHistory && wrapper.UseConversationHistory)
            {
                var recentTurns = History.GetRecentTurns(ConversationLogger.MaxInjectedTurns, actor.Name);
                historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
            }

            string processed = actor.ProcessPrompt(prompt, historyBlock);

            // Resolve model: explicit override ? config default.
            bool isDefaultKeyword = string.Equals(modelOverride, "default", StringComparison.OrdinalIgnoreCase);
            string? model = !string.IsNullOrWhiteSpace(modelOverride) && !isDefaultKeyword
                ? modelOverride
                : ws.Config.DefaultModel;

            Logger.LogProcessedPrompt(actor.Name, processed, model);

            // — Execute ———————————————————————————————————————————————————
            var sw = Stopwatch.StartNew();
            string response = wrapper.Execute(processed, ws.SourcePath, model, Logger, cancellationToken);
            sw.Stop();

            // — Record turn ——————————————————————————————————————————————
            bool isError = IsWrapperError(response, wrapper.Name);
            History.RecordTurn(new ConversationTurn
            {
                Timestamp   = DateTimeOffset.UtcNow,
                SessionId   = Logger.SessionId.ToString("N"),
                ActorName   = actor.Name,
                WrapperName = wrapper.Name,
                Model       = model,
                Prompt      = prompt,
                Response    = response,
                IsError     = isError,
                ElapsedMs   = sw.ElapsedMilliseconds,
                LoopName    = loopName,
                Iteration   = iteration
            });

            return response;
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
        /// Binds the conversation history logger to the current workspace's History folder.
        /// Creates the History directory on disk if it doesn't exist.
        /// Safe to call multiple times — only the first bind takes effect.
        /// </summary>
        private void BindHistory()
        {
            if (!HasWorkspace || History.HistoryFolder != null) return;
            History.Bind(Workspace!.WorkspaceFolder, ConversationLogger.DefaultFolderName);
        }

        /// <summary>
        /// Sets <see cref="SessionLogger"/> on every loaded actor so the actor
        /// pipeline can log processed prompts.
        /// </summary>
        private void InjectLoggerIntoActors()
        {
            foreach (var actor in Actors)
                actor.Logger = Logger;
        }

        /// <summary>
        /// Detects whether a wrapper response string represents an error condition.
        /// Matches the known error patterns produced by <see cref="LLMWrapper"/>.
        /// </summary>
        private static bool IsWrapperError(string response, string wrapperName)
        {
            return response.StartsWith($"Error from {wrapperName}", StringComparison.Ordinal)
                || response.StartsWith($"{wrapperName} exited with code", StringComparison.Ordinal)
                || response.StartsWith($"Failed to call {wrapperName}", StringComparison.Ordinal)
                || response.StartsWith($"({wrapperName} returned an empty response)", StringComparison.Ordinal);
        }
    }
}