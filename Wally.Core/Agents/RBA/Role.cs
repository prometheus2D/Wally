namespace Wally.Core.Agents.RBA
{
    /// <summary>
    /// Represents a role to be roleplayed by an AI, with core prompts provided by a human.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// The role to play, e.g., "detective", "teacher".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The intent or goal of the role.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// The time-length tier: "epoch" (long-term), "story" (medium), "task" (short).
        /// </summary>
        public string Tier { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Role"/> class.
        /// </summary>
        /// <param name="name">The name of the role.</param>
        /// <param name="prompt">The prompt or goal of the role.</param>
        /// <param name="tier">The tier (optional).</param>
        public Role(string name, string prompt, string tier = null)
        {
            Name = name;
            Prompt = prompt;
            Tier = tier;
        }
    }
}