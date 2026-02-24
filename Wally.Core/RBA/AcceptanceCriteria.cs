namespace Wally.Core.RBA
{
    /// <summary>
    /// Represents the acceptance criteria for evaluating the success of a roleplay.
    /// </summary>
    public class AcceptanceCriteria
    {
        /// <summary>
        /// The name of the criteria.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prompt associated with the criteria.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// The time-length tier: "epoch" (long-term), "story" (medium), "task" (short).
        /// </summary>
        public string Tier { get; set; }

        /// <summary>
        /// Initializes a new instance of the AcceptanceCriteria class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="prompt">The prompt.</param>
        /// <param name="tier">The tier (optional).</param>
        public AcceptanceCriteria(string name, string prompt, string tier = null)
        {
            Name = name;
            Prompt = prompt;
            Tier = tier;
        }
    }
}