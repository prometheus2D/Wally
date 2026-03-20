using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Wally.Core.Actions;
using Wally.Core.Logging;

namespace Wally.Core.Actors
{
    /// <summary>
    /// Represents a Wally Actor — a personality defined by RBA prompts
    /// (Role, AcceptanceCriteria, Intent) plus an optional set of declared
    /// <see cref="ActorAction"/>s that constrain what the actor may do at runtime.
    /// <para>
    /// The actor owns the personality and prompt enrichment pipeline.
    /// It knows nothing about LLM wrappers or execution — that is the
    /// responsibility of <see cref="WallyEnvironment"/>.
    /// </para>
    /// </summary>
    public class Actor
    {
        // ?? Identity ?????????????????????????????????????????????????????????

        /// <summary>The actor name — taken from the folder name on disk.</summary>
        public string Name { get; set; }

        /// <summary>The absolute path to this actor's folder inside the workspace.</summary>
        public string FolderPath { get; set; }

        // ?? RBA prompts ???????????????????????????????????????????????????????

        /// <summary>The role prompt for this actor (the "R" in RBA).</summary>
        public string RolePrompt { get; set; } = string.Empty;

        /// <summary>The acceptance criteria prompt for this actor (the "B" in RBA).</summary>
        public string CriteriaPrompt { get; set; } = string.Empty;

        /// <summary>The intent prompt for this actor (the "A" in RBA).</summary>
        public string IntentPrompt { get; set; } = string.Empty;

        // ?? Documentation ????????????????????????????????????????????????????

        /// <summary>
        /// The name of the subfolder inside this actor's directory that holds
        /// actor-private documentation files. Default: <c>Docs</c>.
        /// </summary>
        public string DocsFolderName { get; set; } = "Docs";

        // ?? Capability constraints ????????????????????????????????????????????

        /// <summary>
        /// Priority-ordered list of wrapper names this actor is permitted to run
        /// through. When empty, any loaded wrapper may be used (no restriction).
        /// <para>
        /// The first entry in the list is treated as this actor's preferred
        /// wrapper when no explicit <see cref="PreferredWrapper"/> override is set.
        /// </para>
        /// </summary>
        public List<string> AllowedWrappers { get; set; } = new();

        /// <summary>
        /// List of loop names this actor is permitted to run.
        /// When empty, any loaded loop may be used (no restriction).
        /// </summary>
        public List<string> AllowedLoops { get; set; } = new();

        /// <summary>
        /// The wrapper this actor prefers by default.
        /// When non-null, overrides the workspace-level <c>DefaultWrapper</c> for
        /// this actor. Must be a name present in <see cref="AllowedWrappers"/>
        /// (when that list is non-empty) to take effect.
        /// </summary>
        public string? PreferredWrapper { get; set; }

        /// <summary>
        /// The loop this actor prefers by default.
        /// When non-null, overrides the workspace-level <c>ResolvedDefaultLoop</c>
        /// for this actor. Must be a name present in <see cref="AllowedLoops"/>
        /// (when that list is non-empty) to take effect.
        /// </summary>
        public string? PreferredLoop { get; set; }

        // ?? Actions (Option C: actor-declared capabilities) ???????????????????

        /// <summary>
        /// The set of actions this actor is permitted to invoke via structured
        /// response blocks.  An empty list means the actor produces text-only
        /// responses — no file writes, reads, or other side-effects.
        /// <para>
        /// <see cref="Actions"/> are declared in <c>actor.json</c> and loaded
        /// at workspace startup. At execution time <see cref="WallyEnvironment"/>
        /// passes this list to <see cref="ActionDispatcher"/> which enforces
        /// the allow-list against every LLM response.
        /// </para>
        /// </summary>
        public List<ActorAction> Actions { get; set; } = new();

        // ?? Workspace context ?????????????????????????????????????????????????

        /// <summary>
        /// The workspace this Actor operates in. Provides access to
        /// <see cref="WallyWorkspace.WorkSource"/> for prompt enrichment.
        /// May be <see langword="null"/> when an Actor is constructed outside a workspace.
        /// </summary>
        public WallyWorkspace? Workspace { get; set; }

        /// <summary>
        /// Optional session logger injected by <see cref="WallyEnvironment"/>.
        /// When set, the actor pipeline logs the processed prompt at each stage.
        /// </summary>
        public SessionLogger? Logger { get; set; }

        // ?? Constructor ???????????????????????????????????????????????????????

        /// <summary>
        /// Initializes an Actor with RBA prompts and an optional workspace.
        /// </summary>
        public Actor(string name, string folderPath,
                     string rolePrompt, string criteriaPrompt, string intentPrompt,
                     WallyWorkspace? workspace = null)
        {
            Name           = name;
            FolderPath     = folderPath;
            RolePrompt     = rolePrompt;
            CriteriaPrompt = criteriaPrompt;
            IntentPrompt   = intentPrompt;
            Workspace      = workspace;
        }

        // ?? Prompt generation ?????????????????????????????????????????????????

        /// <summary>
        /// Generates a standard prompt from this actor's RBA components
        /// (Role, AcceptanceCriteria, Intent) with no user input.
        /// </summary>
        public virtual string GeneratePrompt()
        {
            return GeneratePrompt(null);
        }

        /// <summary>
        /// Generates a standard prompt from this actor's RBA components
        /// (Role, AcceptanceCriteria, Intent), wrapping the supplied
        /// <paramref name="userPrompt"/> inside the actor's RBA context.
        /// </summary>
        public virtual string GeneratePrompt(string? userPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# Actor: {Name}");

            if (!string.IsNullOrWhiteSpace(RolePrompt))
                sb.AppendLine($"## Role\n{RolePrompt}");

            if (!string.IsNullOrWhiteSpace(CriteriaPrompt))
                sb.AppendLine($"## Acceptance Criteria\n{CriteriaPrompt}");

            if (!string.IsNullOrWhiteSpace(IntentPrompt))
                sb.AppendLine($"## Intent\n{IntentPrompt}");

            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                sb.AppendLine();
                sb.AppendLine($"## Prompt\n{userPrompt}");
            }

            return sb.ToString().TrimEnd();
        }

        // ?? Pipeline ??????????????????????????????????????????????????????????

        /// <summary>Called once before prompt processing to perform any setup.</summary>
        public virtual void Setup() { }

        /// <summary>
        /// Enriches <paramref name="prompt"/> with the actor's RBA context,
        /// documentation context, optional conversation history, and — when
        /// this actor has declared actions — an action manifest section that
        /// instructs the LLM on which actions it may invoke and the required
        /// fenced-block format.
        /// </summary>
        /// <param name="prompt">The raw user prompt.</param>
        /// <param name="conversationHistory">
        /// A pre-formatted markdown block of recent conversation turns, or
        /// <see langword="null"/> to skip history injection.
        /// </param>
        public virtual string ProcessPrompt(string prompt, string? conversationHistory = null)
        {
            var sb = new StringBuilder();

            // ?? Actor system context ?????????????????????????????????????????
            sb.AppendLine($"# Actor: {Name}");
            if (!string.IsNullOrWhiteSpace(RolePrompt))
                sb.AppendLine($"## Role\n{RolePrompt}");
            if (!string.IsNullOrWhiteSpace(CriteriaPrompt))
                sb.AppendLine($"## Acceptance Criteria\n{CriteriaPrompt}");
            if (!string.IsNullOrWhiteSpace(IntentPrompt))
                sb.AppendLine($"## Intent\n{IntentPrompt}");

            // ?? Action manifest ??????????????????????????????????????????????
            // Injected only when the actor has declared at least one action.
            // Tells the LLM which actions are available, their signatures, and
            // the exact fenced-block format it must use to invoke them.
            if (Actions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Available Actions");
                sb.AppendLine(
                    "You may invoke the following actions by emitting one or more fenced " +
                    "`action` blocks in your response. **Only use actions from this list.** " +
                    "Actions not listed here are not available to you and will be rejected.");
                sb.AppendLine();
                sb.AppendLine("**Block format:**");
                sb.AppendLine("````");
                sb.AppendLine("```action");
                sb.AppendLine("name: <action_name>");
                sb.AppendLine("<param1>: <value1>");
                sb.AppendLine("<param2>: |");
                sb.AppendLine("  Multi-line value");
                sb.AppendLine("  indented by two spaces");
                sb.AppendLine("```");
                sb.AppendLine("````");
                sb.AppendLine();
                sb.AppendLine("**Declared actions:**");
                foreach (var action in Actions)
                    sb.AppendLine(action.ToManifestLine());
            }

            // ?? Documentation context ????????????????????????????????????????
            var docFiles = GetDocumentationFiles();
            if (docFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Documentation Context");
                sb.AppendLine("The following documentation files are available for reference. " +
                              "Consult them when they are relevant to the task:");
                foreach (var (relativePath, source) in docFiles)
                    sb.AppendLine($"- `{relativePath}` ({source})");
            }

            // ?? Conversation history ?????????????????????????????????????????
            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                sb.AppendLine();
                sb.Append(conversationHistory);
            }

            sb.AppendLine();

            // ?? User prompt ??????????????????????????????????????????????????
            sb.AppendLine("## Prompt");
            sb.AppendLine(prompt);

            return sb.ToString().TrimEnd();
        }

        // ?? Capability helpers ????????????????????????????????????????????????

        /// <summary>
        /// Returns <see langword="true"/> when the given wrapper name is
        /// permitted for this actor (either the allow-list is empty, meaning
        /// unrestricted, or the name appears in <see cref="AllowedWrappers"/>).
        /// Comparison is case-insensitive.
        /// </summary>
        public bool IsWrapperAllowed(string wrapperName) =>
            AllowedWrappers.Count == 0 ||
            AllowedWrappers.Any(w => string.Equals(w, wrapperName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns <see langword="true"/> when the given loop name is
        /// permitted for this actor (either the allow-list is empty, meaning
        /// unrestricted, or the name appears in <see cref="AllowedLoops"/>).
        /// Comparison is case-insensitive.
        /// </summary>
        public bool IsLoopAllowed(string loopName) =>
            AllowedLoops.Count == 0 ||
            AllowedLoops.Any(l => string.Equals(l, loopName, StringComparison.OrdinalIgnoreCase));

        // ?? Documentation helpers ?????????????????????????????????????????????

        /// <summary>
        /// Enumerates documentation files from the actor's <c>Docs/</c> folder
        /// and the workspace-level <c>Docs/</c> folder.
        /// </summary>
        protected virtual List<(string RelativePath, string Source)> GetDocumentationFiles()
        {
            var results = new List<(string, string)>();
            var docExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".md", ".txt", ".rst", ".adoc"
            };

            // Actor-level docs
            if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
            {
                string actorDocsDir = Path.Combine(FolderPath, DocsFolderName);
                if (Directory.Exists(actorDocsDir))
                {
                    foreach (string file in Directory.GetFiles(actorDocsDir, "*", SearchOption.AllDirectories))
                    {
                        if (docExtensions.Contains(Path.GetExtension(file)))
                        {
                            string relativePath = Workspace != null
                                ? Path.GetRelativePath(Workspace.WorkSource, file)
                                : Path.GetFileName(file);
                            results.Add((relativePath, "actor docs"));
                        }
                    }
                }
            }

            // Workspace-level docs
            if (Workspace != null && !string.IsNullOrEmpty(Workspace.WorkspaceFolder))
            {
                string wsDocsDir = Path.Combine(Workspace.WorkspaceFolder, Workspace.Config.DocsFolderName);
                if (Directory.Exists(wsDocsDir))
                {
                    foreach (string file in Directory.GetFiles(wsDocsDir, "*", SearchOption.AllDirectories))
                    {
                        if (docExtensions.Contains(Path.GetExtension(file)))
                        {
                            string relativePath = Path.GetRelativePath(Workspace.WorkSource, file);
                            results.Add((relativePath, "workspace docs"));
                        }
                    }
                }
            }

            return results;
        }
    }
}