namespace Wally.Core.Agents.RBA
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
        /// Initializes a new instance of the AcceptanceCriteria class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="prompt">The prompt.</param>
        public AcceptanceCriteria(string name, string prompt)
        {
            Name = name;
            Prompt = prompt;
        }
    }
}