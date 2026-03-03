using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Wally.Core.Logging;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// Abstract base class for Wally Actors.
    /// Each Actor is given a <see cref="WallyWorkspace"/> reference at construction time
    /// so that <see cref="ProcessPrompt"/> can enrich prompts with workspace context.
    /// </summary>
    public abstract class Actor
    {
        // — Identity ——————————————————————————————————————————————————————————

        /// <summary>The actor name — taken from the folder name on disk.</summary>
        public string Name { get; set; }

        /// <summary>The absolute path to this actor's folder inside the workspace.</summary>
        public string FolderPath { get; set; }

        // — RBA identity ——————————————————————————————————————————————————————

        /// <summary>The role this Actor plays.</summary>
        public Role Role { get; set; }

        /// <summary>The acceptance criteria this Actor targets.</summary>
        public AcceptanceCriteria AcceptanceCriteria { get; set; }

        /// <summary>The intent this Actor pursues.</summary>
        public Intent Intent { get; set; }

        // — Documentation ————————————————————————————————————————————————————

        /// <summary>
        /// The name of the subfolder inside this actor's directory that holds
        /// actor-private documentation files. Default: <c>Docs</c>.
        /// </summary>
        public string DocsFolderName { get; set; } = "Docs";

        // — Workspace context —————————————————————————————————————————————

        /// <summary>
        /// The workspace this Actor operates in. Provides access to
        /// <see cref="WallyWorkspace.WorkSource"/> for prompt enrichment.
        /// May be <see langword="null"/> when an Actor is constructed outside a workspace.
        /// </summary>
        public WallyWorkspace? Workspace { get; set; }

        /// <summary>
        /// Optional session logger injected by <see cref="WallyEnvironment"/>.
        /// When set, the actor pipeline logs the processed prompt and responses
        /// at each stage of <see cref="Act"/>.
        /// </summary>
        public SessionLogger? Logger { get; set; }

        // — Runtime overrides ———————————————————————————————————————————

        /// <summary>
        /// When set, overrides <see cref="WallyConfig.DefaultModel"/> for the
        /// current run. Cleared after each <see cref="Act"/> call so it does not
        /// leak into subsequent invocations.
        /// </summary>
        public string? ModelOverride { get; set; }

        // — Constructor ———————————————————————————————————————————————————————

        /// <summary>
        /// Initializes an Actor with RBA components and an optional workspace.
        /// </summary>
        protected Actor(string name, string folderPath,
                        Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                        WallyWorkspace? workspace = null)
        {
            Name               = name;
            FolderPath         = folderPath;
            Role               = role;
            AcceptanceCriteria = acceptanceCriteria;
            Intent             = intent;
            Workspace          = workspace;
        }

        // — Prompt generation ——————————————————————————————————————————————

        /// <summary>
        /// Generates a standard prompt from this actor's RBA components
        /// (Role, AcceptanceCriteria, Intent) with no user input.
        /// </summary>
        /// <returns>A formatted prompt string built from the actor's RBA identity.</returns>
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

            if (!string.IsNullOrWhiteSpace(Role.Prompt))
                sb.AppendLine($"## Role\n{Role.Prompt}");

            if (!string.IsNullOrWhiteSpace(AcceptanceCriteria.Prompt))
                sb.AppendLine($"## Acceptance Criteria\n{AcceptanceCriteria.Prompt}");

            if (!string.IsNullOrWhiteSpace(Intent.Prompt))
                sb.AppendLine($"## Intent\n{Intent.Prompt}");

            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                sb.AppendLine();
                sb.AppendLine($"## Prompt\n{userPrompt}");
            }

            return sb.ToString().TrimEnd();
        }

        // — Overridable pipeline ——————————————————————————————————————————————

        /// <summary>Called once before every <see cref="Act"/> to perform any setup.</summary>
        public virtual void Setup() { }

        /// <summary>
        /// Enriches <paramref name="prompt"/> with the actor's RBA context
        /// before it is passed to <see cref="Respond"/> or
        /// <see cref="ApplyCodeChanges"/>.
        /// <para>
        /// When the actor has a <c>Docs/</c> folder on disk, the files it contains
        /// are listed in a <c>## Documentation Context</c> section so the LLM knows
        /// they exist and can reference them. Workspace-level docs are included too.
        /// The files themselves are accessible to <c>gh copilot</c> via <c>--add-dir</c>.
        /// </para>
        /// Override to customise prompt shaping; call <c>base.ProcessPrompt</c> to retain
        /// the default enrichment.
        /// </summary>
        public virtual string ProcessPrompt(string prompt)
        {
            var sb = new StringBuilder();

            // Actor system context
            sb.AppendLine($"# Actor: {Role.Name}");
            if (!string.IsNullOrWhiteSpace(Role.Prompt))
                sb.AppendLine($"## Role\n{Role.Prompt}");
            if (!string.IsNullOrWhiteSpace(AcceptanceCriteria.Prompt))
                sb.AppendLine($"## Acceptance Criteria\n{AcceptanceCriteria.Prompt}");
            if (!string.IsNullOrWhiteSpace(Intent.Prompt))
                sb.AppendLine($"## Intent\n{Intent.Prompt}");

            // Documentation context — list available doc files so the LLM
            // knows they exist and can consult them for additional context.
            var docFiles = GetDocumentationFiles();
            if (docFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Documentation Context");
                sb.AppendLine("The following documentation files are available for reference. " +
                              "Consult them when they are relevant to the task:");
                foreach (var (relativePath, source) in docFiles)
                {
                    sb.AppendLine($"- `{relativePath}` ({source})");
                }
            }

            sb.AppendLine();

            // User prompt
            sb.AppendLine("## Prompt");
            sb.AppendLine(prompt);

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Returns <see langword="true"/> when this Actor wants to apply code changes
        /// rather than return a text response.
        /// </summary>
        public virtual bool ShouldMakeChanges(string processedPrompt) => false;

        /// <summary>Applies code changes directly based on the processed prompt.</summary>
        public virtual void ApplyCodeChanges(string processedPrompt) { }

        /// <summary>Generates a text response based on the processed prompt.</summary>
        public abstract string Respond(string processedPrompt);

        // — Entry point ———————————————————————————————————————————————————————

        /// <summary>
        /// Runs the full actor pipeline: Setup ? ProcessPrompt ? (ApplyCodeChanges | Respond).
        /// Returns the text response, or <see langword="null"/> when code changes were applied.
        /// </summary>
        public string Act(string prompt)
        {
            Setup();
            string processed = ProcessPrompt(prompt);

            // Log the full enriched prompt that will be sent to the CLI.
            string? model = ModelOverride ?? Workspace?.Config?.DefaultModel;
            Logger?.LogProcessedPrompt(Name, processed, model);

            try
            {
                if (ShouldMakeChanges(processed))
                {
                    ApplyCodeChanges(processed);
                    return null;
                }
                return Respond(processed);
            }
            finally
            {
                // Clear the per-run override so it doesn't leak.
                ModelOverride = null;
            }
        }

        // — Documentation helpers ——————————————————————————————————————————

        /// <summary>
        /// Enumerates documentation files from the actor's <c>Docs/</c> folder
        /// and the workspace-level <c>Docs/</c> folder. Returns a list of
        /// (relativePath, source) tuples where <c>source</c> is "actor docs" or "workspace docs".
        /// Only includes common documentation file types (.md, .txt, .rst, .adoc).
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