namespace Wally.Core.RBA
{
    /// <summary>
    /// Represents the intent or goal that an actor pursues.
    /// </summary>
    public class Intent
    {
        /// <summary>
        /// The name of the intent.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prompt associated with the intent.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Initializes a new instance of the Intent class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="prompt">The prompt.</param>
        public Intent(string name, string prompt)
        {
            Name = name;
            Prompt = prompt;
        }
    }
}