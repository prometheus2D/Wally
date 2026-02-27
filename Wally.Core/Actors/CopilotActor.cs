using System;
using System.Diagnostics;
using System.IO;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A read-only Copilot actor. Forwards the workspace-enriched prompt to
    /// <c>gh copilot</c> and returns the response as text — never applies code
    /// changes directly.
    /// The prompt is fully structured by <see cref="Actor.ProcessPrompt"/> before
    /// this method is called.
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
        /// <c>gh copilot explain</c>.
        /// <para>
        /// The process <c>WorkingDirectory</c> is set to
        /// <see cref="WallyWorkspace.SourcePath"/> so that Copilot CLI receives
        /// the correct file and directory context.  The <c>--model</c> flag is
        /// added when a model is configured in <see cref="WallyConfig.Model"/>.
        /// </para>
        /// </summary>
        public override string Respond(string processedPrompt)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName               = "gh",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    RedirectStandardInput  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                // Build argument list: gh copilot explain [--model <m>] "<prompt>"
                // Using ArgumentList avoids all shell-escaping issues — the OS
                // passes each entry as a discrete argv element.
                startInfo.ArgumentList.Add("copilot");
                startInfo.ArgumentList.Add("explain");

                // Resolve model from config (per-actor override ? default ? omit).
                string? model = Workspace?.Config?.Model?.ResolveForActor(Name);
                if (!string.IsNullOrWhiteSpace(model))
                {
                    startInfo.ArgumentList.Add("--model");
                    startInfo.ArgumentList.Add(model);
                }

                startInfo.ArgumentList.Add(processedPrompt);

                // Set working directory to SourcePath so Copilot CLI sees the
                // target codebase for file context.
                string? sourcePath = Workspace?.SourcePath;
                if (!string.IsNullOrWhiteSpace(sourcePath) && Directory.Exists(sourcePath))
                    startInfo.WorkingDirectory = sourcePath;

                using var process = new Process { StartInfo = startInfo };

                process.Start();

                // Close stdin immediately — we are not sending interactive input.
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();
                string error  = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
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
                return $"Failed to call Copilot CLI: {ex.Message}";
            }
        }
    }
}