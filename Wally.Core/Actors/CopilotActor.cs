using System;
using System.Diagnostics;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A Copilot Actor that forwards workspace-enriched prompts to the GitHub Copilot CLI
    /// for suggestions (read-only — never applies code changes directly).
    /// </summary>
    public class CopilotActor : Actor
    {
        public CopilotActor(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                            WallyWorkspace? workspace = null)
            : base(role, acceptanceCriteria, intent, workspace)
        {
        }

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
                string fullPrompt =
                    $"Role: {Role.Prompt}\n" +
                    $"Intent: {Intent.Prompt}\n" +
                    $"Acceptance Criteria: {AcceptanceCriteria.Prompt}\n" +
                    $"Prompt: {processedPrompt}";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"copilot suggest \"{fullPrompt.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
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