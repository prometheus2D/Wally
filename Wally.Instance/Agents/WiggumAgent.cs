using Wally.Instance.RBA;

namespace Wally.Instance.Agents
{
    /// <summary>
    /// A simple custom Wiggum agent for roleplaying and responding.
    /// </summary>
    public class WiggumAgent : Agent
    {
        /// <summary>
        /// Initializes a new instance of the WiggumAgent class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        public WiggumAgent(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent)
            : base(role, acceptanceCriteria, intent)
        {
        }

        /// <summary>
        /// Generates a response in Wiggum style.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>A response string.</returns>
        public override string Respond(string processedPrompt)
        {
            return $"Wiggum: Aye carumba! Responding to '{processedPrompt}' with role '{Role.Name}' and intent '{Intent.Name}'. Acceptance criteria '{AcceptanceCriteria.Name}' met? Probably!";
        }
    }
}