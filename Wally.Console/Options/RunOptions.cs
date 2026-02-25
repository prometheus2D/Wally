using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run", HelpText = "Run all Actors on the given prompt, or a specific Actor if specified.")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "The prompt to process.")]
        public string Prompt { get; set; }

        [Value(1, Required = false, HelpText = "The name of the specific Actor to run (optional).")]
        public string ActorName { get; set; }
    }
}