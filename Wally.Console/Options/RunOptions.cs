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

        [Option('m', "model", Required = false, Default = null,
            HelpText = "Override the AI model for this run (e.g. gpt-4.1, claude-sonnet-4).")]
        public string Model { get; set; }
    }
}