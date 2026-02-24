using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run", HelpText = "Run all Actors on the given prompt.")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "The prompt to process.")]
        public string Prompt { get; set; }
    }
}