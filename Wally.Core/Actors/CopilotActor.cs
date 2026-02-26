using System;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A read-only Copilot actor. Forwards the workspace-enriched prompt to
    /// <c>gh copilot suggest</c> and returns the suggestion as text — never
    /// applies code changes directly.
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
        /// Generates a suggestion by forwarding the workspace-enriched prompt to
        /// <c>gh copilot suggest</c>.
        /// </summary>
        public override string Respond(string processedPrompt)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName               = "gh",
                        Arguments              = $"copilot suggest \"{processedPrompt.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        UseShellExecute        = false,
                        CreateNoWindow         = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error  = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0
                    ? $"Copilot Suggestion:\n{output}"
                    : $"Error from Copilot: {error}";
            }
            catch (Exception ex)
            {
                return $"Failed to call Copilot CLI: {ex.Message}";
            }
        }
    }
}