using System;
using System.Collections.Generic;
using System.IO;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        // ?? Workspace lifecycle ???????????????????????????????????????????????

        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            env.Logger.LogCommand("load", $"Loaded workspace from {path}");
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        public static void HandleSetup(WallyEnvironment env, string? workSourcePath = null, bool verifyOnly = false)
        {
            if (verifyOnly)
            {
                string wsFolder = ResolveWorkspaceFolder(workSourcePath);
                Console.WriteLine($"Verifying workspace at: {wsFolder}");
                Console.WriteLine();
                var issues = WallyHelper.CheckWorkspace(wsFolder);
                if (issues.Count == 0)
                    Console.WriteLine("\u2713 Workspace structure is valid. No issues found.");
                else
                {
                    Console.WriteLine($"Issues found ({issues.Count}):");
                    foreach (var issue in issues) Console.WriteLine($"  {issue}");
                    Console.WriteLine();
                    Console.WriteLine("Run 'setup' without --verify to create/repair the workspace.");
                }
                env.Logger.LogCommand("setup", $"Verified workspace at {wsFolder}: {issues.Count} issue(s)");
                return;
            }
            env.SetupLocal(workSourcePath);
            env.Logger.LogCommand("setup", $"Workspace ready at {env.WorkspaceFolder}");
            PrintWorkspaceSummary("Workspace ready.", env);
        }

        public static void HandleRepair(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder;
            if (!string.IsNullOrWhiteSpace(workSourcePath))
            {
                string src = Path.IsPathRooted(workSourcePath)
                    ? workSourcePath!
                    : Path.Combine(WallyHelper.GetExeDirectory(), workSourcePath!);
                wsFolder = Path.Combine(Path.GetFullPath(src), WallyHelper.DefaultWorkspaceFolderName);
            }
            else if (env.HasWorkspace)
                wsFolder = env.Workspace!.WorkspaceFolder;
            else
                wsFolder = WallyHelper.GetDefaultWorkspaceFolder();

            wsFolder = Path.GetFullPath(wsFolder);
            Console.WriteLine($"Repairing workspace at: {wsFolder}");
            Console.WriteLine();

            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine("Workspace folder does not exist. Use 'setup' to create a new workspace.");
                env.Logger.LogCommand("repair", $"Workspace not found at {wsFolder}");
                return;
            }

            var config = WallyHelper.ResolveConfig(wsFolder);
            var added  = new List<string>();

            EnsureDir(wsFolder, config.ActorsFolderName,    added);
            EnsureDir(wsFolder, config.DocsFolderName,      added);
            EnsureDir(wsFolder, config.TemplatesFolderName, added);
            EnsureDir(wsFolder, config.LoopsFolderName,     added);
            EnsureDir(wsFolder, config.WrappersFolderName,  added);
            EnsureDir(wsFolder, config.RunbooksFolderName,  added);
            EnsureDir(wsFolder, config.LogsFolderName,      added);
            EnsureDir(wsFolder, Logging.ConversationLogger.DefaultFolderName, added);
            EnsureDir(wsFolder, config.ProjectsFolderName, added);

            string actorsDir = Path.Combine(wsFolder, config.ActorsFolderName);
            if (Directory.Exists(actorsDir))
            {
                foreach (string actorDir in Directory.GetDirectories(actorsDir))
                {
                    string actorName  = Path.GetFileName(actorDir);
                    string docsFolder = Path.Combine(actorDir, "Docs");
                    if (!Directory.Exists(docsFolder))
                    {
                        Directory.CreateDirectory(docsFolder);
                        added.Add($"  Actors/{actorName}/Docs/");
                    }
                    EnsureMailboxDir(actorDir, $"actor '{actorName}'", added);
                }
            }

            if (added.Count == 0)
                Console.WriteLine("\u2713 Workspace is already complete — nothing to repair.");
            else
            {
                Console.WriteLine($"Repaired {added.Count} missing component(s):");
                foreach (var item in added) Console.WriteLine($"  \u2713 {item}");
            }
            Console.WriteLine();

            if (env.HasWorkspace && string.Equals(
                    Path.GetFullPath(env.Workspace!.WorkspaceFolder),
                    wsFolder, StringComparison.OrdinalIgnoreCase))
            {
                env.LoadWorkspace(wsFolder);
                Console.WriteLine("Workspace reloaded.");
            }

            env.Logger.LogCommand("repair",
                added.Count == 0
                    ? $"Repair: workspace at {wsFolder} was already complete."
                    : $"Repair: added {added.Count} component(s) to {wsFolder}.");
        }

        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            env.Logger.LogCommand("save", $"Saved workspace to {path}");
            Console.WriteLine($"Workspace saved to: {path}");
        }

        public static void HandleCleanup(WallyEnvironment env, string? workSourcePath = null)
        {
            string wsFolder = ResolveWorkspaceFolder(workSourcePath);
            if (!Directory.Exists(wsFolder))
            {
                Console.WriteLine($"Nothing to clean \u2014 workspace folder does not exist: {wsFolder}");
                env.Logger.LogCommand("cleanup", $"No workspace at {wsFolder}");
                return;
            }
            if (env.HasWorkspace && string.Equals(
                    Path.GetFullPath(env.Workspace!.WorkspaceFolder),
                    Path.GetFullPath(wsFolder), StringComparison.OrdinalIgnoreCase))
            {
                env.CloseWorkspace();
                Console.WriteLine("Closed active workspace.");
            }
            try
            {
                Directory.Delete(wsFolder, recursive: true);
                Console.WriteLine($"Deleted workspace folder: {wsFolder}");
                Console.WriteLine("Run 'setup' to create a fresh workspace.");
                env.Logger.LogCommand("cleanup", $"Deleted {wsFolder}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cleanup failed: {ex.Message}");
                env.Logger.LogError($"Cleanup failed: {ex.Message}", "cleanup");
            }
        }

        public static void HandleClearHistory(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "clear-history") == null) return;
            env.History.ClearHistory();
            env.Logger.LogCommand("clear-history", "Conversation history cleared.");
            Console.WriteLine("Conversation history cleared.");
        }

        // ?? Private repair helpers ????????????????????????????????????????????

        private static void EnsureDir(string parent, string subFolder, List<string> added)
        {
            string full = Path.Combine(parent, subFolder);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                added.Add($"{subFolder}/");
            }
        }

        private static void EnsureMailboxDir(string entityDir, string label, List<string> added)
        {
            foreach (string folder in new[]
            {
                WallyHelper.MailboxInboxFolderName,
                WallyHelper.MailboxOutboxFolderName,
                WallyHelper.MailboxPendingFolderName,
                WallyHelper.MailboxActiveFolderName
            })
            {
                string full = Path.Combine(entityDir, folder);
                if (!Directory.Exists(full))
                {
                    Directory.CreateDirectory(full);
                    string rel = Path.GetRelativePath(
                        Path.GetDirectoryName(entityDir.TrimEnd(Path.DirectorySeparatorChar)) ?? entityDir,
                        full);
                    added.Add($"{rel}{Path.DirectorySeparatorChar}  [{label} mailbox]");
                }
            }
        }
    }
}
