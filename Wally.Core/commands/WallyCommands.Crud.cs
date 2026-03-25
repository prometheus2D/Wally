using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Actors;
using Wally.Core.Providers;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        // ?? Actor CRUD ????????????????????????????????????????????????????????

        public static void HandleAddActor(
            WallyEnvironment env, string name,
            string rolePrompt, string criteriaPrompt, string intentPrompt)
        {
            if (RequireWorkspace(env, "add-actor") == null) return;
            if (env.GetActor(name) != null)
            {
                Console.WriteLine($"Actor '{name}' already exists. Use 'edit-actor' to modify it.");
                return;
            }
            var ws = env.Workspace!;
            string actorDir = Path.Combine(ws.WorkspaceFolder, ws.Config.ActorsFolderName, name);
            var actor = new Actor(name, actorDir, rolePrompt, criteriaPrompt, intentPrompt, ws);
            WallyHelper.SaveActor(ws.WorkspaceFolder, ws.Config, actor);
            env.ReloadActors();
            env.Logger.LogCommand("add-actor", $"Created actor '{name}'");
            Console.WriteLine($"Actor '{name}' created at: {actorDir}");
        }

        public static void HandleEditActor(
            WallyEnvironment env, string name,
            string? rolePrompt, string? criteriaPrompt, string? intentPrompt)
        {
            if (RequireWorkspace(env, "edit-actor") == null) return;
            var actor = env.GetActor(name);
            if (actor == null)
            {
                Console.WriteLine($"Actor '{name}' not found.");
                foreach (var a in env.Actors) Console.WriteLine($"  {a.Name}");
                return;
            }
            if (rolePrompt     != null) actor.RolePrompt     = rolePrompt;
            if (criteriaPrompt != null) actor.CriteriaPrompt = criteriaPrompt;
            if (intentPrompt   != null) actor.IntentPrompt   = intentPrompt;
            WallyHelper.SaveActor(env.Workspace!.WorkspaceFolder, env.Workspace.Config, actor);
            env.Logger.LogCommand("edit-actor", $"Updated actor '{name}'");
            Console.WriteLine($"Actor '{name}' updated.");
        }

        public static void HandleDeleteActor(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-actor") == null) return;
            var actor = env.GetActor(name);
            if (actor == null) { Console.WriteLine($"Actor '{name}' not found."); return; }
            string actorDir = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.ActorsFolderName, name);
            if (Directory.Exists(actorDir))
            {
                Directory.Delete(actorDir, recursive: true);
                env.ReloadActors();
                env.Logger.LogCommand("delete-actor", $"Deleted actor '{name}'");
                Console.WriteLine($"Actor '{name}' deleted.");
            }
            else Console.WriteLine($"Actor folder not found: {actorDir}");
        }

        // ?? Loop CRUD ?????????????????????????????????????????????????????????

        public static void HandleAddLoop(
            WallyEnvironment env, string name,
            string description, string actorName, string startPrompt)
        {
            if (RequireWorkspace(env, "add-loop") == null) return;
            if (env.GetLoop(name) != null)
            {
                Console.WriteLine($"Loop '{name}' already exists. Use 'edit-loop' to modify it.");
                return;
            }
            var ws = env.Workspace!;
            string loopsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName);
            Directory.CreateDirectory(loopsDir);
            var loop = new WallyLoopDefinition
            {
                Name        = name,
                Description = description,
                ActorName   = actorName,
                StartPrompt = startPrompt
            };
            string filePath = Path.Combine(loopsDir, $"{name}.json");
            loop.SaveToFile(filePath);
            ws.ReloadLoops();
            env.Logger.LogCommand("add-loop", $"Created loop '{name}'");
            Console.WriteLine($"Loop '{name}' created at: {filePath}");
        }

        public static void HandleEditLoop(
            WallyEnvironment env, string name,
            string? description, string? actorName, string? startPrompt)
        {
            if (RequireWorkspace(env, "edit-loop") == null) return;
            var loop = env.GetLoop(name);
            if (loop == null)
            {
                Console.WriteLine($"Loop '{name}' not found.");
                foreach (var l in env.Loops) Console.WriteLine($"  {l.Name}");
                return;
            }
            if (description != null) loop.Description = description;
            if (actorName   != null) loop.ActorName   = actorName;
            if (startPrompt != null) loop.StartPrompt = startPrompt;
            string filePath = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.LoopsFolderName, $"{name}.json");
            loop.SaveToFile(filePath);
            env.Logger.LogCommand("edit-loop", $"Updated loop '{name}'");
            Console.WriteLine($"Loop '{name}' updated.");
        }

        public static void HandleDeleteLoop(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-loop") == null) return;
            string filePath = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.LoopsFolderName, $"{name}.json");
            if (!File.Exists(filePath)) { Console.WriteLine($"Loop file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Workspace.ReloadLoops();
            env.Logger.LogCommand("delete-loop", $"Deleted loop '{name}'");
            Console.WriteLine($"Loop '{name}' deleted.");
        }

        // ?? Wrapper CRUD ??????????????????????????????????????????????????????

        public static void HandleAddWrapper(
            WallyEnvironment env, string name,
            string description, string executable, string argumentTemplate,
            bool canMakeChanges, bool useConversationHistory = true)
        {
            if (RequireWorkspace(env, "add-wrapper") == null) return;
            if (env.Workspace!.LlmWrappers.Any(w =>
                    string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Wrapper '{name}' already exists. Use 'edit-wrapper' to modify it.");
                return;
            }
            var ws = env.Workspace!;
            string wrappersDir = Path.Combine(ws.WorkspaceFolder, ws.Config.WrappersFolderName);
            Directory.CreateDirectory(wrappersDir);
            var wrapper = new LLMWrapper
            {
                Name                   = name,
                Description            = description,
                Executable             = executable,
                ArgumentTemplate       = argumentTemplate,
                CanMakeChanges         = canMakeChanges,
                UseConversationHistory = useConversationHistory
            };
            string filePath = Path.Combine(wrappersDir, $"{name}.json");
            wrapper.SaveToFile(filePath);
            ws.ReloadWrappers();
            env.Logger.LogCommand("add-wrapper", $"Created wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' created at: {filePath}");
        }

        public static void HandleEditWrapper(
            WallyEnvironment env, string name,
            string? description, string? executable, string? argumentTemplate,
            bool? canMakeChanges, bool? useConversationHistory = null)
        {
            if (RequireWorkspace(env, "edit-wrapper") == null) return;
            var wrapper = env.Workspace!.LlmWrappers
                .FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
            if (wrapper == null)
            {
                Console.WriteLine($"Wrapper '{name}' not found.");
                foreach (var w in env.Workspace.LlmWrappers) Console.WriteLine($"  {w.Name}");
                return;
            }
            if (description             != null) wrapper.Description            = description;
            if (executable              != null) wrapper.Executable             = executable;
            if (argumentTemplate        != null) wrapper.ArgumentTemplate       = argumentTemplate;
            if (canMakeChanges.HasValue)         wrapper.CanMakeChanges         = canMakeChanges.Value;
            if (useConversationHistory.HasValue) wrapper.UseConversationHistory = useConversationHistory.Value;
            string filePath = Path.Combine(
                env.Workspace.WorkspaceFolder, env.Workspace.Config.WrappersFolderName, $"{name}.json");
            wrapper.SaveToFile(filePath);
            env.Logger.LogCommand("edit-wrapper", $"Updated wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' updated.");
        }

        public static void HandleDeleteWrapper(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-wrapper") == null) return;
            string filePath = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.WrappersFolderName, $"{name}.json");
            if (!File.Exists(filePath)) { Console.WriteLine($"Wrapper file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Workspace.ReloadWrappers();
            env.Logger.LogCommand("delete-wrapper", $"Deleted wrapper '{name}'");
            Console.WriteLine($"Wrapper '{name}' deleted.");
        }

        // ?? Runbook CRUD ??????????????????????????????????????????????????????

        public static void HandleAddRunbook(WallyEnvironment env, string name, string description)
        {
            if (RequireWorkspace(env, "add-runbook") == null) return;
            if (env.GetRunbook(name) != null)
            {
                Console.WriteLine($"Runbook '{name}' already exists. Use 'edit-runbook' to modify it.");
                return;
            }
            var ws = env.Workspace!;
            string runbooksDir = Path.Combine(ws.WorkspaceFolder, ws.Config.RunbooksFolderName);
            Directory.CreateDirectory(runbooksDir);
            var runbook = new WallyRunbook
            {
                Name        = name,
                Description = description,
                Commands    = new List<string>(),
                FilePath    = Path.Combine(runbooksDir, $"{name}.wrb")
            };
            WallyHelper.SaveRunbook(ws.WorkspaceFolder, ws.Config, runbook);
            env.Logger.LogCommand("add-runbook", $"Created runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' created. Edit {runbook.FilePath} to add commands.");
        }

        public static void HandleEditRunbook(WallyEnvironment env, string name, string? description)
        {
            if (RequireWorkspace(env, "edit-runbook") == null) return;
            var runbook = env.GetRunbook(name);
            if (runbook == null)
            {
                Console.WriteLine($"Runbook '{name}' not found.");
                foreach (var r in env.Runbooks) Console.WriteLine($"  {r.Name}");
                return;
            }
            if (description != null) runbook.Description = description;
            WallyHelper.SaveRunbook(env.Workspace!.WorkspaceFolder, env.Workspace.Config, runbook);
            env.Logger.LogCommand("edit-runbook", $"Updated runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' updated.");
        }

        public static void HandleDeleteRunbook(WallyEnvironment env, string name)
        {
            if (RequireWorkspace(env, "delete-runbook") == null) return;
            string filePath = Path.Combine(
                env.Workspace!.WorkspaceFolder, env.Workspace.Config.RunbooksFolderName, $"{name}.wrb");
            if (!File.Exists(filePath)) { Console.WriteLine($"Runbook file not found: {filePath}"); return; }
            File.Delete(filePath);
            env.Logger.LogCommand("delete-runbook", $"Deleted runbook '{name}'");
            Console.WriteLine($"Runbook '{name}' deleted.");
        }

        // ?? Mailbox stubs (not implemented) ???????????????????????????????????

        /// <summary>Not implemented — the LLM-driven mailbox system has been removed as overengineered.</summary>
        public static void HandleProcessMailboxes(WallyEnvironment env)
            => throw new NotImplementedException(
                "process-mailboxes is not implemented. The LLM-driven mailbox system has been removed.");

        /// <summary>Not implemented — the LLM-driven mailbox system has been removed as overengineered.</summary>
        public static async Task HandleProcessMailboxesAsync(
            WallyEnvironment env, CancellationToken cancellationToken)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            throw new NotImplementedException(
                "process-mailboxes is not implemented. The LLM-driven mailbox system has been removed.");
        }

        /// <summary>Not implemented — the LLM-driven mailbox system has been removed as overengineered.</summary>
        public static void HandleRouteOutbox(WallyEnvironment env)
            => throw new NotImplementedException(
                "route-outbox is not implemented. The LLM-driven mailbox system has been removed.");
    }
}
