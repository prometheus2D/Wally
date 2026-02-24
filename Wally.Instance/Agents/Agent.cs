using Wally.Instance.RBA;

namespace Wally.Instance.Agents
{
    /// <summary>
    /// Abstract base class for Wally agents that can act on an environment based on roles, acceptance criteria, intents, and prompts.
    /// Agents can make code changes or respond with text, similar to a Copilot agent.
    /// </summary>
    public abstract class Agent
    {
        /// <summary>
        /// The role for this agent.
        /// </summary>
        public Role Role { get; set; }

        /// <summary>
        /// The acceptance criteria for this agent.
        /// </summary>
        public AcceptanceCriteria AcceptanceCriteria { get; set; }

        /// <summary>
        /// The intent for this agent.
        /// </summary>
        public Intent Intent { get; set; }

        /// <summary>
        /// Initializes a new instance of the Agent class.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="acceptanceCriteria">The acceptance criteria.</param>
        /// <param name="intent">The intent.</param>
        protected Agent(Role role, AcceptanceCriteria acceptanceCriteria, Intent intent)
        {
            Role = role;
            AcceptanceCriteria = acceptanceCriteria;
            Intent = intent;
        }

        /// <summary>
        /// Sets up the agent with necessary configurations.
        /// </summary>
        public virtual void Setup() { }

        /// <summary>
        /// Processes the prompt and prepares for action.
        /// </summary>
        /// <param name="prompt">The input prompt.</param>
        /// <returns>Processed information.</returns>
        public virtual string ProcessPrompt(string prompt) { return prompt; }

        /// <summary>
        /// Determines if the action should result in code changes or text response.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>True if code changes, false for text response.</returns>
        public virtual bool ShouldMakeChanges(string processedPrompt) { return false; }

        /// <summary>
        /// Applies code changes based on the prompt.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        public virtual void ApplyCodeChanges(string processedPrompt) { }

        /// <summary>
        /// Generates a text response based on the processed prompt.
        /// </summary>
        /// <param name="processedPrompt">The processed prompt.</param>
        /// <returns>The response string.</returns>
        public abstract string Respond(string processedPrompt);

        /// <summary>
        /// Acts on the given prompt, potentially making code changes or returning a text response.
        /// </summary>
        /// <param name="prompt">The input prompt.</param>
        /// <returns>A response string, or null if changes are made directly.</returns>
        public string Act(string prompt)
        {
            Setup();
            string processed = ProcessPrompt(prompt);
            if (ShouldMakeChanges(processed))
            {
                ApplyCodeChanges(processed);
                return null; // Indicate changes were made
            }
            else
            {
                return Respond(processed);
            }
        }
    }
}