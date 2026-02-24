namespace Wally.Core.RBA
{
    public class Intent
    {
        public string Name { get; set; }
        public string Prompt { get; set; }

        /// <summary>
        /// The time-length tier: "epoch" (long-term), "story" (medium), "task" (short).
        /// </summary>
        public string Tier { get; set; }

        public Intent(string name, string prompt, string tier = null)
        {
            Name = name;
            Prompt = prompt;
            Tier = tier;
        }
    }
}