using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Wally.Core.Logging;

namespace Wally.Core.Actors
{
    /// <summary>
    /// Represents a Wally Actor — a personality defined by RBA prompts
    /// (Role, AcceptanceCriteria, Intent).
    /// </summary>
    public class Actor
    {
        public string Name { get; set; }
        public string FolderPath { get; set; }

        public string RolePrompt { get; set; } = string.Empty;
        public string CriteriaPrompt { get; set; } = string.Empty;
        public string IntentPrompt { get; set; } = string.Empty;

        public string DocsFolderName { get; set; } = "Docs";

        public bool Enabled { get; set; } = true;

        public List<string> AllowedWrappers { get; set; } = new();
        public List<string> AllowedLoops { get; set; } = new();
        public string? PreferredWrapper { get; set; }
        public string? PreferredLoop { get; set; }

        /// <summary>
        /// Names of abilities this actor opts into, declared in <c>actor.json</c>
        /// as <c>"abilities": [...]</c>. Persisted on save for round-trip fidelity.
        /// </summary>
        public List<string> Abilities { get; set; } = new();

        public WallyWorkspace? Workspace { get; set; }
        public SessionLogger? Logger { get; set; }

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

        public virtual string GeneratePrompt() => GeneratePrompt(null);

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

        public virtual void Setup() { }

        public virtual string ProcessPrompt(string prompt, string? conversationHistory = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# Actor: {Name}");
            if (!string.IsNullOrWhiteSpace(RolePrompt))
                sb.AppendLine($"## Role\n{RolePrompt}");
            if (!string.IsNullOrWhiteSpace(CriteriaPrompt))
                sb.AppendLine($"## Acceptance Criteria\n{CriteriaPrompt}");
            if (!string.IsNullOrWhiteSpace(IntentPrompt))
                sb.AppendLine($"## Intent\n{IntentPrompt}");

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

            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                sb.AppendLine();
                sb.Append(conversationHistory);
            }

            sb.AppendLine();
            sb.AppendLine("## Prompt");
            sb.AppendLine(prompt);

            return sb.ToString().TrimEnd();
        }

        public bool IsWrapperAllowed(string wrapperName) =>
            AllowedWrappers.Count == 0 ||
            AllowedWrappers.Any(w => string.Equals(w, wrapperName, StringComparison.OrdinalIgnoreCase));

        public bool IsLoopAllowed(string loopName) =>
            AllowedLoops.Count == 0 ||
            AllowedLoops.Any(l => string.Equals(l, loopName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Executes any action blocks found in the given LLM response.
        /// This is the main entry point for actors to "do" their abilities.
        /// </summary>
        /// <param name="llmResponse">The response from the LLM that may contain action blocks.</param>
        /// <param name="workspace">The workspace context for executing actions.</param>
        /// <returns>The original response with action execution results appended.</returns>
        public virtual string PerformActions(string llmResponse, WallyWorkspace workspace)
        {
            return ActionDispatcher.ProcessActionBlocks(this, llmResponse, workspace, Logger);
        }

        protected virtual List<(string RelativePath, string Source)> GetDocumentationFiles()
        {
            var results = new List<(string, string)>();
            var docExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".md", ".txt", ".rst", ".adoc"
            };

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

                string actorsParentDir = Path.GetDirectoryName(FolderPath)!;
                if (!string.IsNullOrEmpty(actorsParentDir) && Directory.Exists(actorsParentDir))
                {
                    foreach (string file in Directory.GetFiles(actorsParentDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (docExtensions.Contains(Path.GetExtension(file)))
                        {
                            string relativePath = Workspace != null
                                ? Path.GetRelativePath(Workspace.WorkSource, file)
                                : Path.GetFileName(file);
                            results.Add((relativePath, "actors shared docs"));
                        }
                    }
                }
            }

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