using CommandLine;

namespace Wally.Console.Options.Run
{
    [Verb("run", HelpText = "Run a prompt through an actor and/or a named pipeline loop.")]
    public class RunOptions
    {
        [Value(0, Required = false, HelpText = "The prompt to process. Omit to resume a resumable loop that already has persisted state.")]
        public string Prompt { get; set; }

        [Option('a', "actor", Required = false, Default = null,
            HelpText = "The name of the Actor to use. Omit to send the prompt directly without actor context.")]
        public string ActorName { get; set; }

        [Option('m', "model", Required = false, Default = null,
            HelpText = "Override the AI model for this run (e.g. gpt-4.1, claude-sonnet-4).")]
        public string Model { get; set; }

        [Option('l', "loop-name", Required = false, Default = null,
            HelpText = "Name of a pipeline definition from the Loops/ folder (e.g. CodeReview, Refactor).")]
        public string LoopName { get; set; }

        [Option('w', "wrapper", Required = false, Default = null,
            HelpText = "Override the LLM wrapper for this run. Must match a Wrappers/*.json name.")]
        public string Wrapper { get; set; }

        [Option("no-history", Required = false, Default = false,
            HelpText = "Suppress conversation history injection into the prompt.")]
        public bool NoHistory { get; set; }
    }
}
