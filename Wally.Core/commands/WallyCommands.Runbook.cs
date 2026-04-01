using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Scripting;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        private sealed class ShellExecutionResult
        {
            public bool Success { get; init; }
            public int ExitCode { get; init; }
            public long ElapsedMilliseconds { get; init; }
            public string Stdout { get; init; } = string.Empty;
            public string Stderr { get; init; } = string.Empty;
        }

        private sealed class CommandExecutionResult
        {
            public bool Success { get; init; }
            public string Output { get; init; } = string.Empty;
        }

        // ?? Runbook entry points ??????????????????????????????????????????????

        public static bool HandleRunbook(
            WallyEnvironment env, string runbookName, string? userPrompt = null, int depth = 0)
        {
            if (depth >= MaxRunbookDepth)
            {
                Console.WriteLine($"Runbook nesting depth exceeded (max {MaxRunbookDepth}). Check for circular runbook calls.");
                env.Logger.LogError($"Runbook depth exceeded for '{runbookName}'.", "runbook");
                return false;
            }
            if (RequireWorkspace(env, "runbook") == null) return false;

            var runbook = env.GetRunbook(runbookName);
            if (runbook == null)
            {
                Console.WriteLine($"Runbook '{runbookName}' not found. Available runbooks:");
                foreach (var r in env.Runbooks)
                    Console.WriteLine($"  {r.Name}{(string.IsNullOrWhiteSpace(r.Description) ? "" : $" \u2014 {r.Description}")}");
                env.Logger.LogError($"Runbook '{runbookName}' not found.", "runbook");
                return false;
            }

            // Route script-format runbooks to the brace-aware executor.
            if (string.Equals(runbook.Format, "script", StringComparison.OrdinalIgnoreCase))
                return HandleRunbookScript(env, runbook, userPrompt, depth);

            // ?? Simple format (legacy one-command-per-line path) ??????????????
            string instanceId = Guid.NewGuid().ToString("N");
            var swTotal = Stopwatch.StartNew();

            env.Logger.LogRunbookStart(instanceId, runbookName, "simple", runbook.Commands.Count);
            env.Logger.LogCommand("runbook", $"Starting '{runbookName}' ({runbook.Commands.Count} commands, depth={depth})");
            Console.WriteLine($"[runbook] Executing '{runbookName}' ({runbook.Commands.Count} commands)");

            for (int i = 0; i < runbook.Commands.Count; i++)
            {
                string line = runbook.Commands[i]
                    .Replace("{workSourcePath}",  env.WorkSource      ?? "")
                    .Replace("{workspaceFolder}", env.WorkspaceFolder ?? "")
                    .Replace("{userPrompt}",      userPrompt          ?? "");

                env.Logger.LogRunbookStep(instanceId, i + 1, SplitArgs(line).FirstOrDefault() ?? "");
                Console.WriteLine($"[runbook:{runbookName}] ({i + 1}/{runbook.Commands.Count}) {line}");
                bool success = ExecuteWallyCommandLineAsync(env, line, depth + 1)
                    .GetAwaiter()
                    .GetResult()
                    .Success;
                if (!success)
                {
                    Console.WriteLine($"[runbook:{runbookName}] Stopped at command {i + 1} due to error.");
                    env.Logger.LogRunbookError(instanceId, i + 1, $"Stopped at: {line}");
                    env.Logger.LogError($"Runbook '{runbookName}' stopped at command {i + 1}: {line}", "runbook");
                    return false;
                }
            }

            swTotal.Stop();
            Console.WriteLine($"[runbook:{runbookName}] Completed ({runbook.Commands.Count} commands).");
            env.Logger.LogRunbookEnd(instanceId, runbookName, runbook.Commands.Count, swTotal.ElapsedMilliseconds);
            env.Logger.LogInfo($"Runbook '{runbookName}' completed ({runbook.Commands.Count} commands).");
            return true;
        }

        /// <summary>
        /// Executes a script-format (<c>Format == "script"</c>) runbook.
        /// <para>
        /// The source is parsed into a <see cref="RunbookStatement"/> tree by
        /// <see cref="RunbookParser"/>; the tree is then executed by a recursive
        /// interpreter that maintains a <em>scope frame</em> (current loop +
        /// active actor) per brace block.
        /// </para>
        /// </summary>
        public static bool HandleRunbookScript(
            WallyEnvironment env,
            WallyRunbook runbook,
            string? userPrompt = null,
            int depth = 0)
        {
            string instanceId = Guid.NewGuid().ToString("N");
            var swTotal = Stopwatch.StartNew();

            env.Logger.LogCommand("runbook", $"[script] Starting '{runbook.Name}' depth={depth}");

            // ?? Parse ?????????????????????????????????????????????????????????
            List<RunbookStatement> stmts;
            try
            {
                stmts = RunbookParser.Parse(runbook.RawSource);
            }
            catch (RunbookParseException ex)
            {
                Console.WriteLine($"[runbook:{runbook.Name}] Parse error � {ex.Message}");
                env.Logger.LogRunbookError(instanceId, 0, ex.Message);
                env.Logger.LogError($"Runbook '{runbook.Name}' parse error: {ex.Message}", "runbook");
                return false;
            }

            env.Logger.LogRunbookStart(instanceId, runbook.Name, "script", stmts.Count);
            Console.WriteLine($"[runbook] Executing '{runbook.Name}' (script, {stmts.Count} top-level statement(s))");

            // ?? Execute ???????????????????????????????????????????????????????
            var frame = new ScriptScopeFrame(activeActor: null, currentLoop: null);
            var ctx   = new ScriptContext(env, runbook.Name, userPrompt, instanceId, depth);

            bool ok = ExecuteStatements(stmts, frame, ctx, ref ctx.StepIndex);

            swTotal.Stop();
            if (ok)
            {
                Console.WriteLine($"[runbook:{runbook.Name}] Completed ({ctx.StepIndex} step(s)).");
                env.Logger.LogRunbookEnd(instanceId, runbook.Name, ctx.StepIndex, swTotal.ElapsedMilliseconds);
                env.Logger.LogInfo($"Runbook '{runbook.Name}' completed ({ctx.StepIndex} step(s)).");
            }
            return ok;
        }

        // ?? Scope frame and execution context ?????????????????????????????????

        private sealed class ScriptScopeFrame
        {
            public string?      ActiveActor { get; set; }
            public RunbookLoop? CurrentLoop { get; set; }

            public ScriptScopeFrame(string? activeActor, RunbookLoop? currentLoop)
            {
                ActiveActor = activeActor;
                CurrentLoop = currentLoop;
            }

            public ScriptScopeFrame CreateChild() => new ScriptScopeFrame(ActiveActor, currentLoop: null);
        }

        private sealed class ScriptContext
        {
            public WallyEnvironment Env         { get; }
            public string           RunbookName { get; }
            public string?          UserPrompt  { get; }
            public string           InstanceId  { get; }
            public int              Depth       { get; }
            public int              StepIndex;

            /// <summary>
            /// Stdout captured from the most recent <c>shell</c> step.
            /// Available as <c>{shellOutput}</c> in subsequent command templates.
            /// Reset to <see cref="string.Empty"/> at the start of each new shell step.
            /// </summary>
            public string LastShellOutput { get; set; } = string.Empty;

            public ScriptContext(WallyEnvironment env, string runbookName, string? userPrompt,
                string instanceId, int depth)
            {
                Env         = env;
                RunbookName = runbookName;
                UserPrompt  = userPrompt;
                InstanceId  = instanceId;
                Depth       = depth;
                StepIndex   = 0;
            }
        }

        // ?? Recursive statement executor ??????????????????????????????????????

        private static bool ExecuteStatements(
            IReadOnlyList<RunbookStatement> stmts, ScriptScopeFrame frame,
            ScriptContext ctx, ref int stepIndex)
        {
            foreach (var stmt in stmts)
                if (!ExecuteStatement(stmt, frame, ctx, ref stepIndex)) return false;
            return true;
        }

        private static bool ExecuteStatement(
            RunbookStatement stmt, ScriptScopeFrame frame,
            ScriptContext ctx, ref int stepIndex)
        {
            stepIndex++;
            ctx.Env.Logger.LogRunbookStep(ctx.InstanceId, stepIndex,
                stmt.GetType().Name.Replace("Runbook", "").ToLowerInvariant());

            switch (stmt)
            {
                case RunbookLoop loop:
                    frame.CurrentLoop = loop;
                    return true;
                case RunbookCall call:
                    return ExecuteCall(call, frame, ctx, ref stepIndex);
                case RunbookShell shell:
                    return ExecuteShell(shell, frame, ctx, stepIndex);
                case RunbookCommand cmd:
                    return ExecuteCommand(cmd, frame, ctx, stepIndex);
                case RunbookOpen:
                    return true;
                default:
                    return true;
            }
        }

        private static bool ExecuteCall(
            RunbookCall call, ScriptScopeFrame parentFrame,
            ScriptContext ctx, ref int stepIndex)
        {
            if (parentFrame.CurrentLoop == null)
            {
                string msg = $"'call' at line {call.LineNumber} has no preceding 'loop {{}}' in the current scope.";
                Console.WriteLine($"[runbook:{ctx.RunbookName}] Error � {msg}");
                ctx.Env.Logger.LogRunbookError(ctx.InstanceId, stepIndex, msg);
                return false;
            }

            var loop      = parentFrame.CurrentLoop;
            var callFrame = parentFrame.CreateChild();

            foreach (var bodyStmt in loop.Body)
            {
                if (bodyStmt is RunbookOpen)
                {
                    if (call.ShotBody != null)
                        if (!ExecuteStatements(call.ShotBody, callFrame, ctx, ref stepIndex)) return false;
                    continue;
                }
                if (!ExecuteStatement(bodyStmt, callFrame, ctx, ref stepIndex)) return false;
            }
            return true;
        }

        private static bool ExecuteShell(
            RunbookShell shell, ScriptScopeFrame frame,
            ScriptContext ctx, int stepIndex)
        {
            string command = ApplyTemplates(shell.Command, ctx);
            string workDir = ctx.Env.WorkSource ?? Directory.GetCurrentDirectory();

            // Reset output before each shell step.
            ctx.LastShellOutput = string.Empty;

            Console.WriteLine($"[runbook:{ctx.RunbookName}] shell> {command}");

            var result = ExecuteShellCommand(command, workDir);
            if (!string.IsNullOrWhiteSpace(result.Stdout)) Console.Write(result.Stdout);
            if (!string.IsNullOrWhiteSpace(result.Stderr)) Console.Error.Write(result.Stderr);

            // Capture stdout so subsequent run commands can reference {shellOutput}.
            ctx.LastShellOutput = result.Stdout;

            ctx.Env.Logger.LogRunbookShell(ctx.InstanceId, stepIndex, command,
                result.ExitCode, result.ElapsedMilliseconds, result.Stdout, result.Stderr);

            if (!result.Success)
            {
                string msg = $"shell exited with code {result.ExitCode}: {command}";
                if (!string.IsNullOrWhiteSpace(result.Stderr)) msg += $"\nstderr: {result.Stderr.TrimEnd()}";
                Console.WriteLine($"[runbook:{ctx.RunbookName}] shell failed (exit {result.ExitCode}) � stopping runbook.");
                ctx.Env.Logger.LogRunbookError(ctx.InstanceId, stepIndex, msg);
                return false;
            }

            return true;
        }

        private static bool ExecuteCommand(
            RunbookCommand cmd, ScriptScopeFrame frame,
            ScriptContext ctx, int stepIndex)
        {
            string line = ApplyTemplates(cmd.Line, ctx);

            string[] args = SplitArgs(line);
            if (args.Length >= 1
                && string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(frame.ActiveActor)
                && WallyArgParser.GetOption(args, "-a", "--actor") == null)
            {
                line = $"{line} -a {frame.ActiveActor}";
                args = SplitArgs(line);
            }

            if (args.Length >= 1 && string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
            {
                string? namedActor = WallyArgParser.GetOption(args, "-a", "--actor");
                if (!string.IsNullOrWhiteSpace(namedActor))
                    frame.ActiveActor = namedActor;
            }

            Console.WriteLine($"[runbook:{ctx.RunbookName}] {line}");
            var result = ExecuteWallyCommandLineAsync(ctx.Env, line, ctx.Depth + 1)
                .GetAwaiter()
                .GetResult();
            if (!result.Success)
            {
                ctx.Env.Logger.LogRunbookError(ctx.InstanceId, stepIndex, $"Command failed: {line}");
                Console.WriteLine($"[runbook:{ctx.RunbookName}] Stopped at step {stepIndex} due to error.");
            }
            return result.Success;
        }

        // ?? Template token replacement ????????????????????????????????????????

        /// <summary>
        /// Replaces well-known template tokens in <paramref name="text"/>.
        /// <list type="bullet">
        /// <item><c>{workSourcePath}</c> � workspace work-source root</item>
        /// <item><c>{workspaceFolder}</c> � .wally folder path</item>
        /// <item><c>{userPrompt}</c> � user prompt passed to the runbook</item>
        /// <item><c>{shellOutput}</c> � stdout from the most recent <c>shell</c> step</item>
        /// </list>
        /// </summary>
        private static string ApplyTemplates(string text, ScriptContext ctx) =>
            text
                .Replace("{workSourcePath}",  ctx.Env.WorkSource      ?? "")
                .Replace("{workspaceFolder}", ctx.Env.WorkspaceFolder ?? "")
                .Replace("{userPrompt}",      ctx.UserPrompt          ?? "")
                .Replace("{shellOutput}",     ctx.LastShellOutput);

        private static ShellExecutionResult ExecuteShellCommand(string command, string workDir)
        {
            var sw = Stopwatch.StartNew();
            int exitCode = -1;
            string stdout = string.Empty;
            string stderr = string.Empty;

            try
            {
                bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

                string shellExe = isWindows ? "cmd.exe" : "/bin/sh";
                string shellArgs = isWindows
                    ? $"/c {command}"
                    : $"-c \"{command.Replace("\"", "\\\"")}\"";

                var psi = new ProcessStartInfo(shellExe, shellArgs)
                {
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to start shell process.");

                stdout = proc.StandardOutput.ReadToEnd();
                stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            catch (Exception ex)
            {
                stderr = ex.Message;
            }
            finally
            {
                sw.Stop();
            }

            return new ShellExecutionResult
            {
                Success = exitCode == 0,
                ExitCode = exitCode,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                Stdout = stdout,
                Stderr = stderr
            };
        }

        private static async Task<CommandExecutionResult> ExecuteWallyCommandLineAsync(
            WallyEnvironment env,
            string line,
            int runbookDepth,
            TextWriter? mirrorOutput = null,
            CancellationToken cancellationToken = default)
        {
            string[] args = SplitArgs(line);
            if (args.Length == 0)
                return new CommandExecutionResult { Success = true };

            if (string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                    return new CommandExecutionResult { Success = false, Output = "Usage: run \"<prompt>\" [-a actor] [-m model] [-w wrapper] [-l name] [--no-history]" };

                string? actorName = WallyArgParser.GetOption(args, "-a", "--actor");
                string? model = WallyArgParser.GetOption(args, "-m", "--model");
                string? wrapper = WallyArgParser.GetOption(args, "-w", "--wrapper");
                string? loopName = WallyArgParser.GetOption(args, "-l", "--loop-name");
                bool noHistory = WallyArgParser.HasFlag(args, "--no-history");

                using var capture = new StringWriter();
                var results = await HandleRunTypedAsync(
                    env,
                    args[1],
                    actorName,
                    model,
                    loopName,
                    wrapper,
                    noHistory,
                    cancellationToken,
                    capture)
                    .ConfigureAwait(false);

                string output = capture.ToString();
                if (string.IsNullOrWhiteSpace(output) && results.Count > 0)
                {
                    output = string.Join(
                        Environment.NewLine + Environment.NewLine,
                        results.Select(result => result.Response));
                }

                if (mirrorOutput != null && !string.IsNullOrWhiteSpace(output))
                    await mirrorOutput.WriteAsync(output).ConfigureAwait(false);

                return new CommandExecutionResult
                {
                    Success = results.Count > 0,
                    Output = output
                };
            }

            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            using var stdoutCapture = TextWriter.Synchronized(new StringWriter());
            using var stderrCapture = TextWriter.Synchronized(new StringWriter());

            try
            {
                Console.SetOut(stdoutCapture);
                Console.SetError(stderrCapture);
                bool success = DispatchCommand(env, args, runbookDepth);

                string output = BuildCommandOutput(stdoutCapture.ToString(), stderrCapture.ToString());
                if (mirrorOutput != null && !string.IsNullOrWhiteSpace(output))
                    await mirrorOutput.WriteAsync(output).ConfigureAwait(false);

                return new CommandExecutionResult { Success = success, Output = output };
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        private static string BuildCommandOutput(string stdout, string stderr)
        {
            if (string.IsNullOrWhiteSpace(stdout))
                return stderr;
            if (string.IsNullOrWhiteSpace(stderr))
                return stdout;
            return $"{stdout.TrimEnd()}{Environment.NewLine}{stderr.TrimEnd()}";
        }
    }
}
