using System;
using System.Diagnostics;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A custom Wally Actor that integrates all components for comprehensive action.
    /// </summary>
    public class WallyActor : Actor
    {
        /// <summary>
        /// Initializes a new instance of the WallyActor class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        /// <param name="workspace">The optional workspace.</param>
        public WallyActor(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent,
                          WallyWorkspace? workspace = null)
            : base(role, acceptanceCriteria, intent, workspace)
        {
        }

        /// <summary>
        /// Generates a comprehensive response using GitHub Copilot CLI.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>A response string from Copilot.</returns>
        public override string Respond(string processedPrompt)
        {
            try
            {
                // Construct the full prompt including role, intent, and criteria
                string fullPrompt = $"Role: {Role.Prompt}\nIntent: {Intent.Prompt}\nAcceptance Criteria: {AcceptanceCriteria.Prompt}\nPrompt: {processedPrompt}";

                // Use GitHub Copilot CLI to explain or suggest based on the prompt
                // Assuming 'gh copilot explain' can handle general prompts; adjust as needed
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"copilot explain \"{fullPrompt.Replace("\"", "\\\"")}\"", // Escape quotes
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return $"Copilot Response:\n{output}";
                }
                else
                {
                    return $"Error from Copilot: {error}";
                }
            }
            catch (Exception ex)
            {
                return $"Failed to call Copilot CLI: {ex.Message}";
            }
        }
    }
}