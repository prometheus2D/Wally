using System;
using System.Diagnostics;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// A Copilot Actor that uses GitHub Copilot CLI for code suggestions and explanations.
    /// </summary>
    public class CopilotActor : Actor
    {
        /// <summary>
        /// Initializes a new instance of the CopilotActor class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        public CopilotActor(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent)
            : base(role, acceptanceCriteria, intent)
        {
        }

        /// <summary>
        /// Determines if changes should be made (false for suggestions only).
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>False.</returns>
        public override bool ShouldMakeChanges(string processedPrompt) => false;

        /// <summary>
        /// Generates a response using GitHub Copilot CLI suggest.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>A response string from Copilot.</returns>
        public override string Respond(string processedPrompt)
        {
            try
            {
                // Construct the full prompt including role, intent, and criteria
                string fullPrompt = $"Role: {Role.Prompt}\nIntent: {Intent.Prompt}\nAcceptance Criteria: {AcceptanceCriteria.Prompt}\nPrompt: {processedPrompt}";

                // Use GitHub Copilot CLI to suggest code
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"copilot suggest \"{fullPrompt.Replace("\"", "\\\"")}\"", // Escape quotes
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
                    return $"Copilot Suggestion:\n{output}";
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