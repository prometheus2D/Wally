using System;
using System.Diagnostics;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// The default Wally actor. Forwards the workspace-enriched prompt (already
    /// structured with agent context by <see cref="Actor.ProcessPrompt"/>) to the
    /// GitHub Copilot CLI via <c>gh copilot explain</c>.
    /// </summary>
    public class WallyActor : Actor
    {
        public WallyActor(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                          WallyWorkspace? workspace = null)
            : base(role, acceptanceCriteria, intent, workspace) { }

        /// <summary>
        /// Forwards the fully-structured prompt to <c>gh copilot explain</c> and
        /// returns the CLI output. The prompt already contains the agent's Role,
        /// AcceptanceCriteria, Intent, and workspace context from
        /// <see cref="Actor.ProcessPrompt"/>.
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
                        Arguments              = $"copilot explain \"{processedPrompt.Replace("\"", "\\\"")}\"",
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
                    ? $"Copilot Response:\n{output}"
                    : $"Error from Copilot: {error}";
            }
            catch (Exception ex)
            {
                return $"Failed to call Copilot CLI: {ex.Message}";
            }
        }
    }
}