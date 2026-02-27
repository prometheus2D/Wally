using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run-iterative", HelpText = "Run actors iteratively, feeding each response back as the next prompt. Targets all actors, or one by name with --actor.")]
    public class RunIterativeOptions
    {
        [Value(0, Required = true, HelpText = "The initial prompt to process.")]
        public string Prompt { get; set; }

        [Option('a', "actor", Required = false, Default = null,
            HelpText = "Name of a specific actor to run iteratively (default: run all actors).")]
        public string ActorName { get; set; }

        [Option('m', "max-iterations", Required = false, Default = 0,
            HelpText = "Override the maximum number of iterations (0 = use environment default).")]
        public int MaxIterations { get; set; }

        [Option("model", Required = false, Default = null,
            HelpText = "Override the AI model for this run (e.g. gpt-4.1, claude-sonnet-4).")]
        public string Model { get; set; }
    }
}
