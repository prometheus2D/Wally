using Wally.Instance.RBA;

namespace Wally.Instance.Agents
{
    /// <summary>
    /// A custom Wally agent that integrates all components for comprehensive action.
    /// </summary>
    public class WallyAgent : Agent
    {
        /// <summary>
        /// Initializes a new instance of the WallyAgent class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        public WallyAgent(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent)
            : base(role, acceptanceCriteria, intent)
        {
        }

        /// <summary>
        /// Generates a comprehensive response.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>A response string.</returns>
        public override string Respond(string processedPrompt)
        {
            return $"Wally Agent: Comprehensive response to '{processedPrompt}' using role prompt '{Role.Prompt}', intent prompt '{Intent.Prompt}', and criteria prompt '{AcceptanceCriteria.Prompt}'. Ready for action!";
        }
    }
}