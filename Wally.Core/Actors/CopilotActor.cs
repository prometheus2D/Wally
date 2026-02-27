using System;
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
        /// The prompt is written to a temporary file and piped via the shell to
        /// avoid argument-length limits and escaping issues.  The process
        /// <c>WorkingDirectory</c> is set to <see cref="Actor.Workspace"/>.<see
        /// cref="WallyWorkspace.SourcePath"/> so that Copilot CLI receives the
        /// correct file and directory context.
        /// </para>
        /// </summary>
        public override string Respond(string processedPrompt)
        {
            string? tempFile = null;
            try
            {
                // Write the full prompt to a temp file so we can pipe it in
                // without hitting argument-length or escaping issues.
                tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, processedPrompt);

                // Build a shell command that pipes the prompt file into gh copilot.
                // "type" on Windows / "cat" on Unix sends the file to stdin.
                bool isWindows = OperatingSystem.IsWindows();
                string catCommand = isWindows
                    ? $"type \"{tempFile}\""
                    : $"cat '{tempFile}'";
                string fullCommand = $"{catCommand} | gh copilot explain";

                string shell, shellArgs;
                if (isWindows)
                {
                    // cmd.exe /c handles pipes natively; do not add outer quotes
                    // around the full command — that breaks when inner quotes exist.
                    shell     = "cmd.exe";
                    shellArgs = $"/c {fullCommand}";
                }
                else
                {
                    shell     = "/bin/sh";
                    shellArgs = $"-c \"{fullCommand}\"";
                }

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = shell,
                    Arguments              = shellArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                // Set working directory to SourcePath so Copilot CLI sees the
                // target codebase for file context.
                string? sourcePath = Workspace?.SourcePath;
                if (!string.IsNullOrWhiteSpace(sourcePath) && Directory.Exists(sourcePath))
                    startInfo.WorkingDirectory = sourcePath;

                var process = new System.Diagnostics.Process { StartInfo = startInfo };

                process.Start();
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
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); }
                    catch { /* best-effort cleanup */ }
                }
            }
        }
    }
}