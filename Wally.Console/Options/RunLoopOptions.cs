using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run-loop", HelpText = "Run an Actor in an iterative loop until completion or max iterations.")]
    public class RunLoopOptions
    {
        [Value(0, Required = true, HelpText = "The name of the Actor to run.")]
        public string ActorName { get; set; }

        [Value(1, Required = true, HelpText = "The prompt to start the loop with.")]
        public string Prompt { get; set; }

        [Option('m', "model", Required = false, Default = null,
            HelpText = "Override the AI model for this run (e.g. gpt-4.1, claude-sonnet-4).")]
        public string Model { get; set; }

        [Option('n', "max-iterations", Required = false, Default = 0,
            HelpText = "Maximum loop iterations. 0 uses the workspace config MaxIterations.")]
        public int MaxIterations { get; set; }
    }
}
