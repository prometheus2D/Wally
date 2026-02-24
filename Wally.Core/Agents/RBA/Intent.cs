namespace Wally.Core.Agents.RBA
{
    public class Intent
    {
        public string Name { get; set; }
        public string Prompt { get; set; }

        public Intent(string name, string prompt)
        {
            Name = name;
            Prompt = prompt;
        }
    }
}