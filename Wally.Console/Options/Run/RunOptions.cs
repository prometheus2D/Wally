using CommandLine;

namespace Wally.Console.Options.Run
{
    [Verb("run", HelpText = "Run an Actor on the given prompt. Use --loop to iterate until completion.")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "The name of the Actor to run.")]
        public string ActorName { get; set; }

        [Value(1, Required = true, HelpText = "The prompt to process.")]
        public string Prompt { get; set; }

        [Option('m', "model", Required = false, Default = null,
            HelpText = "Override the AI model for this run (e.g. gpt-4.1, claude-sonnet-4).")]
        public string Model { get; set; }

        [Option("loop", Required = false, Default = false,
            HelpText = "Run in iterative loop mode (repeats until completion keyword or max iterations).")]
        public bool Looped { get; set; }

        [Option('l', "loop-name", Required = false, Default = null,
            HelpText = "Name of a loop definition from the Loops/ folder (e.g. CodeReview, Refactor). Implies --loop.")]
        public string LoopName { get; set; }

        [Option('n', "max-iterations", Required = false, Default = 0,
            HelpText = "Maximum loop iterations. 0 uses the loop definition or workspace config default. Implies --loop when > 1.")]
        public int MaxIterations { get; set; }

        [Option('w', "wrapper", Required = false, Default = null,
            HelpText = "Override the LLM wrapper for this run (e.g. Copilot, AutoCopilot). Must match a Wrappers/*.json name.")]
        public string Wrapper { get; set; }
    }
}
