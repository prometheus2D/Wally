using System;
using System.IO;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        // ?? Workspace inspection ??????????????????????????????????????????????

        public static void HandleList(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list") == null) return;
            env.Logger.LogCommand("list");
            var ws = env.Workspace!;
            Console.WriteLine($"Actors ({ws.Actors.Count}):");
            if (ws.Actors.Count == 0)
                Console.WriteLine($"  (none \u2014 add a subfolder with actor.json to {ws.WorkspaceFolder}/Actors/)");
            foreach (var actor in ws.Actors)
            {
                Console.WriteLine($"  [{actor.Name}]  folder: {actor.FolderPath}");
                PrintRbaLine("    Role",     actor.RolePrompt);
                PrintRbaLine("    Criteria", actor.CriteriaPrompt);
                PrintRbaLine("    Intent",   actor.IntentPrompt);
                if (!string.IsNullOrEmpty(actor.FolderPath))
                {
                    string docsPath = Path.Combine(actor.FolderPath, actor.DocsFolderName);
                    if (Directory.Exists(docsPath))
                        Console.WriteLine($"    Docs folder: {docsPath}");
                    if (Directory.Exists(Path.Combine(actor.FolderPath, WallyHelper.MailboxInboxFolderName)))
                        Console.WriteLine($"    Mailbox: Inbox / Outbox / Pending / Active");
                }
            }
        }

        public static void HandleListLoops(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-loops") == null) return;
            env.Logger.LogCommand("list-loops");
            var loops = env.Loops;
            Console.WriteLine($"Loops ({loops.Count}):");
            if (loops.Count == 0)
            {
                Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Loops/)");
                return;
            }

            foreach (var loop in loops)
            {
                Console.WriteLine($"  [{loop.Name}]");
                if (!string.IsNullOrWhiteSpace(loop.Description))
                    Console.WriteLine($"    Description: {loop.Description}");

                if (loop.HasSteps)
                {
                    Console.WriteLine($"    Mode:        pipeline ({loop.Steps.Count} step(s))");
                    for (int i = 0; i < loop.Steps.Count; i++)
                    {
                        var s = loop.Steps[i];
                        string actorDisplay = string.IsNullOrWhiteSpace(s.ActorName)
                            ? (string.IsNullOrWhiteSpace(loop.ActorName)
                                ? "(direct mode)"
                                : loop.ActorName + " (fallback)")
                            : s.ActorName;
                        Console.WriteLine($"    Step {i + 1}: [{(string.IsNullOrWhiteSpace(s.Name) ? $"step-{i+1}" : s.Name)}]  Actor: {actorDisplay}");
                        if (!string.IsNullOrWhiteSpace(s.Description))
                            Console.WriteLine($"             {s.Description}");
                    }
                }
                else if (loop.IsAgentLoop)
                {
                    Console.WriteLine($"    Mode:        agent-loop (max {loop.MaxIterations} iteration(s))");
                    Console.WriteLine($"    Actor:       {(string.IsNullOrWhiteSpace(loop.ActorName) ? "(caller must specify)" : loop.ActorName)}");
                    if (!string.IsNullOrWhiteSpace(loop.StopKeyword))
                        Console.WriteLine($"    StopKeyword: {loop.StopKeyword}");
                    Console.WriteLine($"    FeedbackMode: {loop.FeedbackMode}");
                    PrintRbaLine("    Prompt", loop.StartPrompt);
                }
                else
                {
                    Console.WriteLine($"    Mode:        single-actor");
                    Console.WriteLine($"    Actor:       {(string.IsNullOrWhiteSpace(loop.ActorName) ? "(caller must specify)" : loop.ActorName)}");
                    PrintRbaLine("    Prompt", loop.StartPrompt);
                }
            }
        }

        public static void HandleListWrappers(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-wrappers") == null) return;
            env.Logger.LogCommand("list-wrappers");
            var wrappers = env.Workspace!.LlmWrappers;
            Console.WriteLine($"Wrappers ({wrappers.Count}):");
            if (wrappers.Count == 0)
            {
                Console.WriteLine($"  (none \u2014 add .json files to {env.WorkspaceFolder}/Wrappers/)");
                return;
            }
            foreach (var w in wrappers)
            {
                Console.WriteLine($"  [{w.Name}]");
                if (!string.IsNullOrWhiteSpace(w.Description))
                    Console.WriteLine($"    Description:  {w.Description}");
                Console.WriteLine($"    Executable:   {w.Executable}");
                Console.WriteLine($"    Template:     {w.ArgumentTemplate}");
                Console.WriteLine($"    CanMakeChanges:         {w.CanMakeChanges}");
                Console.WriteLine($"    UseConversationHistory: {w.UseConversationHistory}");
            }
        }

        public static void HandleListRunbooks(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "list-runbooks") == null) return;
            env.Logger.LogCommand("list-runbooks");
            var runbooks = env.Runbooks;
            Console.WriteLine($"Runbooks ({runbooks.Count}):");
            if (runbooks.Count == 0)
            {
                Console.WriteLine($"  (none \u2014 add .wrb files to {env.WorkspaceFolder}/Runbooks/)");
                return;
            }
            foreach (var rb in runbooks)
            {
                Console.WriteLine($"  [{rb.Name}]");
                if (!string.IsNullOrWhiteSpace(rb.Description))
                    Console.WriteLine($"    Description: {rb.Description}");
                Console.WriteLine($"    Format:   {rb.Format}");
                Console.WriteLine($"    Commands: {rb.Commands.Count}");
                Console.WriteLine($"    File:     {rb.FilePath}");
            }
        }

        public static void HandleInfo(WallyEnvironment env)
        {
            env.Logger.LogCommand("info");
            if (!env.HasWorkspace)
            {
                Console.WriteLine("Status:           No workspace loaded.");
                Console.WriteLine("                  Use 'load <path>' or 'setup <path>' first.");
                return;
            }
            var ws  = env.Workspace!;
            var cfg = ws.Config;
            Console.WriteLine($"Status:           Workspace loaded");
            Console.WriteLine($"WorkSource:       {ws.WorkSource}");
            Console.WriteLine($"Workspace folder: {ws.WorkspaceFolder}");
            Console.WriteLine($"Actors folder:    {Path.Combine(ws.WorkspaceFolder, cfg.ActorsFolderName)}");
            Console.WriteLine($"Docs folder:      {Path.Combine(ws.WorkspaceFolder, cfg.DocsFolderName)}");
            Console.WriteLine($"Templates folder: {Path.Combine(ws.WorkspaceFolder, cfg.TemplatesFolderName)}");
            Console.WriteLine($"Logs folder:      {Path.Combine(ws.WorkspaceFolder, cfg.LogsFolderName)}");

            string projectsDir = Path.Combine(ws.WorkspaceFolder, cfg.ProjectsFolderName);
            bool hasProjects    = Directory.Exists(projectsDir);
            int  projectCount   = hasProjects ? Directory.GetDirectories(projectsDir).Length : 0;
            Console.WriteLine($"Projects folder:  {projectsDir}{(hasProjects ? $" ({projectCount} project(s))" : " (not initialised \u2014 run setup)")}");

            Console.WriteLine($"Actors:           {ws.Actors.Count}");
            foreach (var a in ws.Actors)
            {
                bool hasMailbox = !string.IsNullOrEmpty(a.FolderPath) &&
                    Directory.Exists(Path.Combine(a.FolderPath, WallyHelper.MailboxInboxFolderName));
                Console.WriteLine($"  {a.Name}{(hasMailbox ? "  [mailbox]" : "")}");
            }
            Console.WriteLine($"Loops:            {ws.Loops.Count}");
            foreach (var l in ws.Loops)
                Console.WriteLine($"  {l.Name}{(string.IsNullOrWhiteSpace(l.Description) ? "" : $" \u2014 {l.Description}")}");
            Console.WriteLine($"Wrappers:         {ws.LlmWrappers.Count}");
            foreach (var w in ws.LlmWrappers)
                Console.WriteLine($"  {w.Name}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $" \u2014 {w.Description}")}");
            Console.WriteLine($"Runbooks:         {ws.Runbooks.Count}");
            foreach (var r in ws.Runbooks)
                Console.WriteLine($"  {r.Name}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" \u2014 {r.Description}")}");
            Console.WriteLine();
            Console.WriteLine($"Default model:    {cfg.DefaultModel   ?? "(none)"}");
            Console.WriteLine($"Default wrapper:  {cfg.DefaultWrapper ?? "(none)"}");
            if (!string.IsNullOrEmpty(cfg.ResolvedDefaultLoop))
                Console.WriteLine($"Default loop:     {cfg.ResolvedDefaultLoop}");
            if (!string.IsNullOrEmpty(cfg.ResolvedDefaultRunbook))
                Console.WriteLine($"Default runbook:  {cfg.ResolvedDefaultRunbook}");
            if (cfg.DefaultModels.Count   > 0)
                Console.WriteLine($"Models:           {string.Join(", ", cfg.DefaultModels)}");
            if (cfg.DefaultWrappers.Count > 0)
                Console.WriteLine($"Wrappers:         {string.Join(", ", cfg.DefaultWrappers)}");
            Console.WriteLine();
            Console.WriteLine($"Session ID:       {env.Logger.SessionId:N}");
            Console.WriteLine($"Session started:  {env.Logger.StartedAt:u}");
            Console.WriteLine($"Session log:      {env.Logger.LogFolder ?? "(not bound)"}");
            Console.WriteLine($"Current log file: {env.Logger.CurrentLogFile ?? "(none)"}");
            Console.WriteLine($"Log rotation:     {(cfg.LogRotationMinutes > 0 ? $"every {cfg.LogRotationMinutes} min" : "disabled")}");
        }

        public static void HandleReloadActors(WallyEnvironment env)
        {
            if (RequireWorkspace(env, "reload-actors") == null) return;
            env.ReloadActors();
            env.Logger.LogCommand("reload-actors", $"Reloaded {env.Actors.Count} actors");
            Console.WriteLine($"Actors reloaded: {env.Actors.Count}");
            foreach (var a in env.Actors) Console.WriteLine($"  {a.Name}");
        }

        // ?? Help / Tutorial ???????????????????????????????????????????????????

        public static void HandleHelp()
        {
            Console.WriteLine("Wally \u2014 AI Actor Environment Manager");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("Quick start:");
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine("  wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine("  wally run \"What is this project about?\"");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  setup [<path>] [--verify]     Set up / verify a workspace.");
            Console.WriteLine("  repair [<path>]               Add any missing workspace components.");
            Console.WriteLine("  load <path>                   Load an existing .wally/ workspace.");
            Console.WriteLine("  info                          Show workspace info and session details.");
            Console.WriteLine("  tutorial                      Step-by-step guide.");
            Console.WriteLine("  commands                      Show this help message.");
            Console.WriteLine();
            Console.WriteLine("  run \"<prompt>\" [options]");
            Console.WriteLine("    -a, --actor <name>          Actor to use (adds RBA context).");
            Console.WriteLine("    -m, --model <model>         Override the AI model.");
            Console.WriteLine("    -w, --wrapper <name>        Override the LLM wrapper.");
            Console.WriteLine("    -l, --loop-name <name>      Run a named pipeline definition.");
            Console.WriteLine("    --no-history                Suppress conversation history injection.");
            Console.WriteLine();
            Console.WriteLine("  runbook <name> [\"<prompt>\"]   Execute a runbook (.wrb command sequence).");
            Console.WriteLine();
            Console.WriteLine("  Actors:   list | add-actor | edit-actor | delete-actor | reload-actors");
            Console.WriteLine("  Loops:    list-loops | add-loop | edit-loop | delete-loop");
            Console.WriteLine("  Wrappers: list-wrappers | add-wrapper | edit-wrapper | delete-wrapper");
            Console.WriteLine("  Runbooks: list-runbooks | add-runbook | edit-runbook | delete-runbook");
            Console.WriteLine();
            Console.WriteLine("  save <path> | cleanup [<path>] | clear-history");
            Console.WriteLine();
            Console.WriteLine("Runbook script format (.wrb):");
            Console.WriteLine("  shell <cmd>                   Run a shell command; stops runbook on non-zero exit.");
            Console.WriteLine("                                CWD = WorkSource root. stdout available as {shellOutput}.");
            Console.WriteLine("  loop {                        Define a reusable block.");
            Console.WriteLine("    run \"...\" -a Actor");
            Console.WriteLine("    open                        Injection slot for call's shot body.");
            Console.WriteLine("  }");
            Console.WriteLine("  call                          Execute the current loop (open = no-op).");
            Console.WriteLine("  call {                        Execute the current loop, injecting shot body at open.");
            Console.WriteLine("    run \"...\" -a Actor");
            Console.WriteLine("  }");
        }

        public static void HandleTutorial()
        {
            Console.WriteLine("Wally \u2014 Getting Started Tutorial");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("STEP 1: SET UP A WORKSPACE");
            Console.WriteLine("  wally setup C:\\repos\\MyApp");
            Console.WriteLine();
            Console.WriteLine("STEP 2: RUN A PROMPT");
            Console.WriteLine("  wally run \"What does this codebase do?\"");
            Console.WriteLine("  wally run \"Review the auth module\" -a Engineer");
            Console.WriteLine();
            Console.WriteLine("STEP 3: ADD AN ACTOR");
            Console.WriteLine("  wally add-actor SecurityAuditor -r \"You are a security auditor\" -c \"Find vulnerabilities\" -i \"Produce a security report\"");
            Console.WriteLine();
            Console.WriteLine("STEP 4: WRITE A SCRIPT RUNBOOK");
            Console.WriteLine("  shell git log --oneline -10");
            Console.WriteLine("  run \"{shellOutput}\" -a Engineer   # pass shell output to actor");
            Console.WriteLine("  loop {");
            Console.WriteLine("      run \"Review from your perspective\"");
            Console.WriteLine("      open");
            Console.WriteLine("      run \"Wrap up\"");
            Console.WriteLine("  }");
            Console.WriteLine("  call { run \"Flag technical issues\" -a Engineer }");
            Console.WriteLine("  call { run \"Check security\" -a BusinessAnalyst }");
            Console.WriteLine();
            Console.WriteLine("STEP 5: INSPECT & EXPLORE");
            Console.WriteLine("  wally info | wally list | wally list-loops | wally list-wrappers");
            Console.WriteLine("  wally commands");
        }
    }
}
