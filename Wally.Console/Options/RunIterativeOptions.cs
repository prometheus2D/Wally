using CommandLine;

namespace Wally.Console.Options
{
    [Verb("run-iterative", HelpText = "Run all Actors iteratively up to MaxIterations times, feeding responses back as the next prompt.")]
    public class RunIterativeOptions
    {
        [Value(0, Required = true, HelpText = "The initial prompt to process.")]
        public string Prompt { get; set; }

        [Option('m', "max-iterations", Required = false, Default = 0,
            HelpText = "Override the maximum number of iterations (0 = use environment default).")]
        public int MaxIterations { get; set; }
    }
}
