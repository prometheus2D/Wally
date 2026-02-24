namespace Wally.Instance.RBA
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
        /// Initializes a new instance of the <see cref="Role"/> class.
        /// </summary>
        /// <param name="name">The name of the role.</param>
        /// <param name="prompt">The prompt or goal of the role.</param>
        public Role(string name, string prompt)
        {
            Name = name;
            Prompt = prompt;
        }
    }
}