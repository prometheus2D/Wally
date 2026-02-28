using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A read-only Copilot actor. Forwards the workspace-enriched prompt to
    /// <c>gh copilot</c> and returns the response as text — never applies code
    /// changes directly.
    /// <para>
    /// Each call to <see cref="Respond"/> is fully stateless — a new
    /// <c>gh copilot -p</c> process is spawned and exits after completion.
    /// There is no conversation memory between calls; context must be
    /// carried forward explicitly in the prompt.
    /// </para>
    /// </summary>
    public class CopilotActor : Actor
    {
        public CopilotActor(string name, string folderPath,
                            Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                            WallyWorkspace? workspace = null)
            : base(name, folderPath, role, acceptanceCriteria, intent, workspace) { }

        /// <summary>Copilot Actor never applies changes directly; it always responds with text.</summary>
        public override bool ShouldMakeChanges(string processedPrompt) => false;

        /// <summary>
        /// Generates a response by forwarding the workspace-enriched prompt to
        /// <c>gh copilot -p</c> (non-interactive mode).
        /// <para>
        /// Each invocation is stateless — a fresh process is spawned, the prompt
        /// is sent, the response is captured, and the process exits. The
        /// <c>WorkingDirectory</c> is set to <see cref="WallyWorkspace.WorkSource"/>
        /// so Copilot CLI sees the target codebase. The <c>--model</c> flag is
        /// added when <see cref="WallyConfig.DefaultModel"/> is configured.
        /// </para>
        /// </summary>
        public override string Respond(string processedPrompt)
        {
            try
            {
                // Resolve source path up front — used for both --add-dir and WorkingDirectory.
                string? sourcePath = Workspace?.SourcePath;
                bool hasSourcePath = !string.IsNullOrWhiteSpace(sourcePath) && Directory.Exists(sourcePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName               = "gh",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    RedirectStandardInput  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8
                };

                // Build argument list: gh copilot [--model <m>] [--add-dir <src>] -s -p "<prompt>"
                // Using ArgumentList avoids all shell-escaping issues — the OS
                // passes each entry as a discrete argv element.
                startInfo.ArgumentList.Add("copilot");

                // Add --model: per-run override takes priority, then config default.
                // Passing "default" as the override explicitly uses the config's DefaultModel.
                bool isDefaultKeyword = string.Equals(ModelOverride, "default", StringComparison.OrdinalIgnoreCase);
                string? model = !string.IsNullOrWhiteSpace(ModelOverride) && !isDefaultKeyword
                    ? ModelOverride
                    : Workspace?.Config?.DefaultModel;
                if (!string.IsNullOrWhiteSpace(model))
                {
                    startInfo.ArgumentList.Add("--model");
                    startInfo.ArgumentList.Add(model);
                }

                // Grant Copilot read access to the source directory so it can
                // glob and read files without interactive permission prompts.
                if (hasSourcePath)
                {
                    startInfo.ArgumentList.Add("--add-dir");
                    startInfo.ArgumentList.Add(sourcePath!);
                }

                // -s (silent) suppresses stats/spinners, giving clean text output.
                startInfo.ArgumentList.Add("-s");

                // -p for non-interactive mode (exits after completion).
                startInfo.ArgumentList.Add("-p");
                startInfo.ArgumentList.Add(processedPrompt);

                // Set working directory to SourcePath so Copilot CLI sees the
                // target codebase for file context.
                if (hasSourcePath)
                    startInfo.WorkingDirectory = sourcePath!;

                using var process = new Process { StartInfo = startInfo };

                process.Start();

                // Close stdin immediately — we are not sending interactive input.
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();
                string error  = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger?.LogCliError(Name, process.ExitCode, error);
                    return string.IsNullOrWhiteSpace(error)
                        ? $"Copilot exited with code {process.ExitCode}."
                        : $"Error from Copilot (exit {process.ExitCode}):\n{error}";
                }

                return string.IsNullOrWhiteSpace(output)
                    ? "(Copilot returned an empty response)"
                    : output.Trim();
            }
            catch (Exception ex)
            {
                Logger?.LogCliError(Name, -1, ex.Message);
                return $"Failed to call Copilot CLI: {ex.Message}";
            }
        }
    }
}