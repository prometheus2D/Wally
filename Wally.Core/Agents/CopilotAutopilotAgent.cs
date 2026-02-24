using System;
using Wally.Core.Agents.RBA;

namespace Wally.Core.Agents
{
    /// <summary>
    /// A Copilot autopilot agent that acts like GitHub Copilot's autopilot mode.
    /// </summary>
    public class CopilotAutopilotAgent : Agent
    {
        /// <summary>
        /// Initializes a new instance of the CopilotAutopilotAgent class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        public CopilotAutopilotAgent(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent)
            : base(role, acceptanceCriteria, intent)
        {
        }

        /// <summary>
        /// Determines if changes should be made (always true for autopilot).
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>True.</returns>
        public override bool ShouldMakeChanges(string processedPrompt) => true;

        /// <summary>
        /// Applies code changes by simulating autopilot actions.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        public override void ApplyCodeChanges(string processedPrompt)
        {
            // Simulate applying code changes
            Console.WriteLine($"Copilot Autopilot: Applying changes based on '{processedPrompt}'.");
        }

        /// <summary>
        /// Generates a response (fallback, but changes are preferred).
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>A response string.</returns>
        public override string Respond(string processedPrompt)
        {
            return $"Copilot Autopilot: Processed '{processedPrompt}' and applied changes.";
        }
    }
}