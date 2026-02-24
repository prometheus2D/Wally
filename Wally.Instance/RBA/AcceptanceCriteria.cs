namespace Wally.Instance.RBA
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
    }
}