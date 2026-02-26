using System;
using System.Collections.Generic;
using System.IO;
using Wally.Core;

namespace Wally.Core
{
    /// <summary>
    /// Contains the implementation logic for Wally commands.
    /// Each method accepts the <see cref="WallyEnvironment"/> it should operate on —
    /// no static environment state is held here.
    /// </summary>
    public static class WallyCommands
    {
        // ?? Guard ?????????????????????????????????????????????????????????????

        private static WallyEnvironment? RequireWorkspace(WallyEnvironment env, string commandName)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine(
                    $"Command '{commandName}' requires a workspace. " +
                    $"Use 'load <path>' or 'create <path>' first.");
                return null;
            }
            return env;
        }

        // ?? Workspace lifecycle ???????????????????????????????????????????????

        /// <summary>Loads a workspace from <paramref name="path"/> into <paramref name="env"/>.</summary>
        public static void HandleLoad(WallyEnvironment env, string path)
        {
            env.LoadWorkspace(path);
            PrintWorkspaceSummary("Workspace loaded.", env);
        }

        /// <summary>Scaffolds a new workspace at <paramref name="path"/> and loads it.</summary>
        public static void HandleCreate(WallyEnvironment env, string path)
        {
            env.CreateWorkspace(path, WallyHelper.ResolveConfig());
            PrintWorkspaceSummary("Workspace created.", env);
        }

        /// <summary>Self-assembles a workspace in the exe directory and loads it.</summary>
        public static void HandleSetup(WallyEnvironment env)
        {
            env.SetupLocal();
            PrintWorkspaceSummary("Workspace ready.", env);
        }

        /// <summary>Saves the active workspace config and all prompt files to <paramref name="path"/>.</summary>
        public static void HandleSave(WallyEnvironment env, string path)
        {
            if (RequireWorkspace(env, "save") == null) return;
            env.SaveToWorkspace(path);
            Console.WriteLine($"Workspace saved to: {path}");
        }

        // ?? Reference management ??????????????????????????????????????????????

        public static void HandleAddFolder(WallyEnvironment env, string folderPath)
        {
            if (RequireWorkspace(env, "add-folder") == null) return;
            env.AddFolderReference(folderPath);
            Console.WriteLine($"Folder reference added: {folderPath}");
        }

        public static void HandleAddFile(WallyEnvironment env, string filePath)
        {
            if (RequireWorkspace(env, "add-file") == null) return;
            env.AddFileReference(filePath);
            Console.WriteLine($"File reference added: {filePath}");
        }

        public static void HandleRemoveFolder(WallyEnvironment env, string folderPath)
        {
            if (RequireWorkspace(env, "remove-folder") == null) return;
            bool removed = env.RemoveFolderReference(folderPath);
            Console.WriteLine(removed
                ? $"Folder reference removed: {folderPath}"
                : $"Folder reference not found: {folderPath}");
        }

        public static void HandleRemoveFile(WallyEnvironment env, string filePath)
        {
            if (RequireWorkspace(env, "remove-file") == null) return;
            bool removed = env.RemoveFileReference(filePath);
            Console.WriteLine(removed
                ? $"File reference removed: {filePath}"
                : $"File reference not found: {filePath}");
        }

        public static void HandleClearReferences(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "clear-refs") == null) return;
            env.ClearReferences();
            Console.WriteLine("All folder and file references cleared.");
        }

        // ?? Running actors ????????????????????????????????????????????????????

        public static List<string> HandleRun(WallyEnvironment env, string prompt, string actorName = null)
        {
            if (RequireWorkspace(env, "run") == null) return new List<string>();
            return !string.IsNullOrEmpty(actorName)
                ? env.RunActor(prompt, actorName)
                : env.RunActors(prompt);
        }

        public static List<string> HandleRunIterative(
            WallyEnvironment env, string prompt, int maxIterationsOverride = 0)
        {
            if (RequireWorkspace(env, "run-iterative") == null) return new List<string>();

            if (maxIterationsOverride > 0)
                env.MaxIterations = maxIterationsOverride;

            Console.WriteLine($"Running iterative mode (max {env.MaxIterations} iterations)...");

            return env.RunActorsIterative(prompt, (iteration, responses) =>
            {
                Console.WriteLine($"--- Iteration {iteration} ---");
                foreach (var response in responses)
                    Console.WriteLine(response);
                if (responses.Count == 0)
                    Console.WriteLine("No responses. Stopping early.");
            });
        }

        // ?? Workspace inspection ??????????????????????????????????????????????

        /// <summary>
        /// Lists the loaded RBA definitions (Roles, Criteria, Intents), built actors,
        /// and all registered folder and file references.
        /// </summary>
        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;

            var ws  = env.Workspace!;
            var cfg = ws.Config;

            // ?? Roles ?????????????????????????????????????????????????????????
            Console.WriteLine($"Roles ({cfg.Roles.Count}):");
            if (cfg.Roles.Count == 0)
                Console.WriteLine("  (none)");
            foreach (var r in cfg.Roles)
            {
                string tier = string.IsNullOrWhiteSpace(r.Tier) ? "" : $"  [{r.Tier}]";
                Console.WriteLine($"  {r.Name}{tier}");
                Console.WriteLine($"    {r.Prompt}");
            }

            // ?? Criteria ??????????????????????????????????????????????????????
            Console.WriteLine($"Criteria ({cfg.AcceptanceCriterias.Count}):");
            if (cfg.AcceptanceCriterias.Count == 0)
                Console.WriteLine("  (none)");
            foreach (var c in cfg.AcceptanceCriterias)
            {
                string tier = string.IsNullOrWhiteSpace(c.Tier) ? "" : $"  [{c.Tier}]";
                Console.WriteLine($"  {c.Name}{tier}");
                Console.WriteLine($"    {c.Prompt}");
            }

            // ?? Intents ???????????????????????????????????????????????????????
            Console.WriteLine($"Intents ({cfg.Intents.Count}):");
            if (cfg.Intents.Count == 0)
                Console.WriteLine("  (none)");
            foreach (var i in cfg.Intents)
            {
                string tier = string.IsNullOrWhiteSpace(i.Tier) ? "" : $"  [{i.Tier}]";
                Console.WriteLine($"  {i.Name}{tier}");
                Console.WriteLine($"    {i.Prompt}");
            }

            // ?? Actors (cartesian product) ????????????????????????????????????
            Console.WriteLine($"Actors ({ws.Actors.Count}):  " +
                $"({cfg.Roles.Count} roles × {cfg.AcceptanceCriterias.Count} criteria × {cfg.Intents.Count} intents)");
            if (ws.Actors.Count == 0)
                Console.WriteLine("  (none — add prompt files to Roles/, Criteria/, Intents/)");
            foreach (var actor in ws.Actors)
                Console.WriteLine($"  {actor.Role.Name} / {actor.AcceptanceCriteria.Name} / {actor.Intent.Name}");

            // ?? Context references ????????????????????????????????????????????
            Console.WriteLine($"Folder References ({ws.FolderReferences.Count}):");
            if (ws.FolderReferences.Count == 0) Console.WriteLine("  (none)");
            foreach (var f in ws.FolderReferences) Console.WriteLine($"  {f}");

            Console.WriteLine($"File References ({ws.FileReferences.Count}):");
            if (ws.FileReferences.Count == 0) Console.WriteLine("  (none)");
            foreach (var f in ws.FileReferences) Console.WriteLine($"  {f}");
        }

        /// <summary>Displays workspace paths, RBA counts, reference counts, and runtime settings.</summary>
        public static void HandleInfo(WallyEnvironment env)
        {
            if (!env.HasWorkspace)
            {
                Console.WriteLine("Status:           No workspace loaded.");
                Console.WriteLine("                  Use 'load <path>' or 'create <path>' first.");
                return;
            }

            var ws  = env.Workspace!;
            var cfg = ws.Config;

            Console.WriteLine($"Status:           Workspace loaded");
            Console.WriteLine($"Parent folder:    {ws.ParentFolder}");
            Console.WriteLine($"Project folder:   {ws.ProjectFolder}");
            Console.WriteLine($"Workspace folder: {ws.WorkspaceFolder}");
            Console.WriteLine();
            Console.WriteLine($"RBA prompt folders:");
            Console.WriteLine($"  Roles:          {Path.Combine(ws.WorkspaceFolder, cfg.RolesFolderName)}  ({cfg.Roles.Count} loaded)");
            Console.WriteLine($"  Criteria:       {Path.Combine(ws.WorkspaceFolder, cfg.CriteriaFolderName)}  ({cfg.AcceptanceCriterias.Count} loaded)");
            Console.WriteLine($"  Intents:        {Path.Combine(ws.WorkspaceFolder, cfg.IntentsFolderName)}  ({cfg.Intents.Count} loaded)");
            Console.WriteLine();
            Console.WriteLine($"Actors:           {ws.Actors.Count}  ({cfg.Roles.Count}R × {cfg.AcceptanceCriterias.Count}C × {cfg.Intents.Count}I)");
            Console.WriteLine($"Folder refs:      {ws.FolderReferences.Count}");
            Console.WriteLine($"File refs:        {ws.FileReferences.Count}");
            Console.WriteLine($"Max iterations:   {env.MaxIterations}");
        }

        // ?? Legacy actor loading ??????????????????????????????????????????????

        /// <summary>
        /// Loads actors from <paramref name="jsonPath"/>, or auto-resolves
        /// <c>default-agents.json</c> from the workspace/exe directory when path is null or empty.
        /// </summary>
        public static void HandleLoadActors(WallyEnvironment env, string jsonPath = null)
        {
            if (RequireWorkspace(env, "load-actors") == null) return;
            try
            {
                if (string.IsNullOrWhiteSpace(jsonPath))
                {
                    env.LoadDefaultActors();
                    string resolved = WallyHelper.ResolveDefaultAgentsPath(env.WorkspaceFolder!) ?? "(unknown)";
                    Console.WriteLine($"Default actors loaded from {resolved}");
                }
                else
                {
                    env.LoadDefaultActors(jsonPath);
                    Console.WriteLine($"Actors loaded from {jsonPath}");
                }
                Console.WriteLine($"  Actors loaded: {env.Actors.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load actors: {ex.Message}");
            }
        }

        // ?? Help ??????????????????????????????????????????????????????????????

        public static void HandleHelp()
        {
            Console.WriteLine("Wally — AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("No workspace required:");
            Console.WriteLine("  setup                    Scaffold workspace + prompt files next to exe, then load.");
            Console.WriteLine("  create <path>            Scaffold a new workspace at <path> (parent folder).");
            Console.WriteLine("  load <path>              Load an existing workspace from <path>.");
            Console.WriteLine("  info                     Show workspace paths, RBA counts, and settings.");
            Console.WriteLine("  help                     Show this message.");
            Console.WriteLine();
            Console.WriteLine("Workspace required:");
            Console.WriteLine("  save <path>              Save config + prompt files to <path>.");
            Console.WriteLine("  list                     List Roles, Criteria, Intents, Actors, and refs.");
            Console.WriteLine("  add-folder <path>        Register a folder for Copilot context.");
            Console.WriteLine("  add-file <path>          Register a file for Copilot context.");
            Console.WriteLine("  remove-folder <path>     Deregister a folder.");
            Console.WriteLine("  remove-file <path>       Deregister a file.");
            Console.WriteLine("  clear-refs               Clear all folder and file references.");
            Console.WriteLine("  run \"<prompt>\" [actor]   Run actors on prompt (all, or one by name).");
            Console.WriteLine("  run-iterative \"<prompt>\" Run actors iteratively; -m N to cap iterations.");
            Console.WriteLine("  load-actors [path]       Load legacy actors from JSON (omit to auto-resolve).");
            Console.WriteLine();
            Console.WriteLine("Prompt files:");
            Console.WriteLine("  Edit .txt files in .wally/Roles/, .wally/Criteria/, .wally/Intents/");
            Console.WriteLine("  Filename: <Name>.txt  or  <Name>.<Tier>.txt  (e.g. Developer.task.txt)");
            Console.WriteLine("  Actors are rebuilt as Roles × Criteria × Intents on every load.");
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private static void PrintWorkspaceSummary(string header, WallyEnvironment env)
        {
            var cfg = env.Workspace!.Config;
            Console.WriteLine(header);
            Console.WriteLine($"  Parent:     {env.ParentFolder}");
            Console.WriteLine($"  Project:    {env.ProjectFolder}");
            Console.WriteLine($"  Workspace:  {env.WorkspaceFolder}");
            Console.WriteLine($"  Roles:      {cfg.Roles.Count}  " +
                $"Criteria: {cfg.AcceptanceCriterias.Count}  " +
                $"Intents: {cfg.Intents.Count}  " +
                $"? {env.Actors.Count} actors");
        }
    }
}