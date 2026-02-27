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
    /// The prompt is fully structured by <see cref="Actor.ProcessPrompt"/> before
    /// this method is called.
    /// </summary>
    public class CopilotActor : Actor
    {
        /// <summary>
        /// When set, the next <see cref="Respond"/> call will pass
        /// <c>--resume &lt;SessionId&gt;</c> to <c>gh copilot</c> so the
        /// conversation continues in the same Copilot session.
        /// Set automatically after the first call to <see cref="Respond"/>
        /// if a session ID is captured.
        /// </summary>
        public string? CopilotSessionId { get; set; }

        public CopilotActor(string name, string folderPath,
                            Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                            WallyWorkspace? workspace = null)
            : base(name, folderPath, role, acceptanceCriteria, intent, workspace) { }

        /// <summary>Copilot Actor never applies changes directly; it always responds with text.</summary>
        public override bool ShouldMakeChanges(string processedPrompt) => false;

        /// <summary>
        /// Clears the <see cref="CopilotSessionId"/> so the next call starts
        /// a fresh Copilot session with no prior conversation context.
        /// </summary>
        public void ResetSession()
        {
            CopilotSessionId = null;
        }

        /// <summary>
        /// Generates a response by forwarding the workspace-enriched prompt to
        /// <c>gh copilot -p</c> (non-interactive mode).
        /// <para>
        /// The process <c>WorkingDirectory</c> is set to
        /// <see cref="WallyWorkspace.WorkSource"/> so that Copilot CLI receives
        /// the correct file and directory context. The <c>--model</c> flag is
        /// added when <see cref="WallyConfig.DefaultModel"/> is configured.
        /// When <see cref="CopilotSessionId"/> is set, <c>--resume</c> is added
        /// to continue the previous Copilot conversation.
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

                // Build argument list: gh copilot [--resume <id>] [--model <m>] [--add-dir <src>] -s -p "<prompt>"
                startInfo.ArgumentList.Add("copilot");

                // Resume a previous session if we have a session ID.
                if (!string.IsNullOrWhiteSpace(CopilotSessionId))
                {
                    startInfo.ArgumentList.Add("--resume");
                    startInfo.ArgumentList.Add(CopilotSessionId);
                }

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