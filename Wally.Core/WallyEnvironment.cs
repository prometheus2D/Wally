using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

        // ?? LLM wrapper resolution ????????????????????????????????????????????

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

        /// <summary>
        /// Resolves the best wrapper for an actor, respecting the actor's
        /// <see cref="Actor.PreferredWrapper"/> and <see cref="Actor.AllowedWrappers"/>
        /// constraints before falling back to the workspace default.
        /// </summary>
        /// <param name="actor">The actor being executed.</param>
        /// <param name="wrapperOverride">Explicit caller override, or null.</param>
        /// <returns>The resolved wrapper name to use.</returns>
        public string? ResolveWrapperForActor(Actor actor, string? wrapperOverride)
        {
            var ws = RequireWorkspace();

            // 1 — Explicit caller override always wins, but still enforce allow-list.
            if (!string.IsNullOrWhiteSpace(wrapperOverride))
            {
                if (!actor.IsWrapperAllowed(wrapperOverride!))
                    throw new InvalidOperationException(
                        $"Actor '{actor.Name}' does not permit wrapper '{wrapperOverride}'. " +
                        $"Allowed: [{string.Join(", ", actor.AllowedWrappers)}]");
                return wrapperOverride;
            }

            // 2 — Actor's own preferred wrapper (must pass allow-list if set).
            if (!string.IsNullOrWhiteSpace(actor.PreferredWrapper))
            {
                string preferred = actor.PreferredWrapper!;
                if (actor.IsWrapperAllowed(preferred) &&
                    ws.LlmWrappers.Any(w => string.Equals(w.Name, preferred, StringComparison.OrdinalIgnoreCase)))
                    return preferred;
            }

            // 3 — First entry in AllowedWrappers that is actually loaded.
            if (actor.AllowedWrappers.Count > 0)
            {
                foreach (string name in actor.AllowedWrappers)
                {
                    if (ws.LlmWrappers.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                        return name;
                }
            }

            // 4 — Workspace default (allow-list check when non-empty).
            string wsDefault = ws.Config.DefaultWrapper;
            if (!string.IsNullOrWhiteSpace(wsDefault))
            {
                if (!actor.IsWrapperAllowed(wsDefault))
                    throw new InvalidOperationException(
                        $"Actor '{actor.Name}' does not permit the workspace default wrapper '{wsDefault}'. " +
                        $"Allowed: [{string.Join(", ", actor.AllowedWrappers)}]");
                return wsDefault;
            }

            return null;
        }

        /// <summary>
        /// Validates that a loop name is permitted for the given actor.
        /// Throws when the actor has a non-empty <see cref="Actor.AllowedLoops"/>
        /// list and the requested loop is not in it.
        /// </summary>
        public void EnforceLoopPolicy(Actor actor, string? loopName)
        {
            if (string.IsNullOrWhiteSpace(loopName)) return;
            if (!actor.IsLoopAllowed(loopName!))
                throw new InvalidOperationException(
                    $"Actor '{actor.Name}' does not permit loop '{loopName}'. " +
                    $"Allowed: [{string.Join(", ", actor.AllowedLoops)}]");
        }

        // ?? Running actors ?????????????????????????????????????????????????????

        /// <summary>
        /// Synchronous wrapper — delegates to <see cref="ExecutePromptAsync"/>
        /// </summary>
        public string ExecutePrompt(
            string prompt,
            string? modelOverride = null,
            string? wrapperOverride = null,
            string? loopName = null,
            int iteration = 0,
            bool skipHistory = false,
            CancellationToken cancellationToken = default)
            => ExecutePromptAsync(prompt, modelOverride, wrapperOverride, loopName, iteration,
                    skipHistory, cancellationToken)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Executes a prompt directly through the LLM wrapper without any actor
        /// enrichment. Genuinely async — does not block the calling thread.
        /// </summary>
        public async Task<string> ExecutePromptAsync(
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

            bool isDefaultKeyword = string.Equals(modelOverride, "default", StringComparison.OrdinalIgnoreCase);
            string? model = !string.IsNullOrWhiteSpace(modelOverride) && !isDefaultKeyword
                ? modelOverride
                : ws.Config.DefaultModel;

            string effectivePrompt = prompt;
            if (!skipHistory && wrapper.UseConversationHistory)
            {
                var recentTurns = History.GetRecentTurns(ConversationLogger.MaxInjectedTurns, null);
                string? historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
                if (historyBlock != null)
                    effectivePrompt = historyBlock + "\n" + prompt;
            }

            Logger.LogProcessedPrompt("(no actor)", effectivePrompt, model);

            var sw = Stopwatch.StartNew();
            string response = await wrapper.ExecuteAsync(effectivePrompt, ws.SourcePath, model, Logger, cancellationToken)
                .ConfigureAwait(false);
            sw.Stop();

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
        /// Synchronous wrapper — delegates to <see cref="ExecuteActorAsync"/>
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
            => ExecuteActorAsync(actor, prompt, modelOverride, wrapperOverride, loopName, iteration,
                    skipHistory, cancellationToken)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Executes a single actor: Setup ? ProcessPrompt ? LlmWrapper.ExecuteAsync.
        /// Genuinely async — does not block the calling thread.
        /// </summary>
        public async Task<string> ExecuteActorAsync(
            Actor actor,
            string prompt,
            string? modelOverride = null,
            string? wrapperOverride = null,
            string? loopName = null,
            int iteration = 0,
            bool skipHistory = false,
            CancellationToken cancellationToken = default)
        {
            string? resolvedWrapperName = ResolveWrapperForActor(actor, wrapperOverride);
            if (!string.IsNullOrWhiteSpace(loopName))
                EnforceLoopPolicy(actor, loopName);

            var wrapper = resolvedWrapperName != null
                ? (WallyHelper.ResolveWrapper(resolvedWrapperName, Workspace!.LlmWrappers)
                   ?? throw new InvalidOperationException($"Wrapper '{resolvedWrapperName}' not found."))
                : ResolveWrapper(null);

            var ws = Workspace!;

            actor.Setup();

            string? historyBlock = null;
            if (!skipHistory && wrapper.UseConversationHistory)
            {
                var recentTurns = History.GetRecentTurns(ConversationLogger.MaxInjectedTurns, actor.Name);
                historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
            }

            string processed = actor.ProcessPrompt(prompt, historyBlock);

            bool isDefaultKeyword = string.Equals(modelOverride, "default", StringComparison.OrdinalIgnoreCase);
            string? model = !string.IsNullOrWhiteSpace(modelOverride) && !isDefaultKeyword
                ? modelOverride
                : ws.Config.DefaultModel;

            Logger.LogProcessedPrompt(actor.Name, processed, model);

            var sw = Stopwatch.StartNew();
            string response = await wrapper.ExecuteAsync(processed, ws.SourcePath, model, Logger, cancellationToken)
                .ConfigureAwait(false);
            sw.Stop();

            if (wrapper.CanMakeChanges && !string.IsNullOrEmpty(response))
                response = actor.PerformActions(response, ws);

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