using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run", HelpText = "Run a specific Actor on the given prompt.")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "The name of the Actor to run.")]
        public string ActorName { get; set; }

        [Value(1, Required = true, HelpText = "The prompt to process.")]
        public string Prompt { get; set; }
    }
}