using CommandLine;

namespace Wally.Console.Options
{
    [Verb("add-loop", HelpText = "Create a new loop definition in the workspace.")]
    public class AddLoopOptions
    {
        [Value(0, Required = true, HelpText = "Name for the new loop (becomes the JSON file name).")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = "",
            HelpText = "Description of what this loop does.")]
        public string Description { get; set; } = string.Empty;

        [Option('a', "actor", Required = false, Default = "",
            HelpText = "Default actor name for this loop.")]
        public string ActorName { get; set; } = string.Empty;

        [Option('n', "max-iterations", Required = false, Default = 5,
            HelpText = "Maximum number of iterations.")]
        public int MaxIterations { get; set; } = 5;

        [Option('s', "start-prompt", Required = false, Default = "{userPrompt}",
            HelpText = "The start prompt template. Use {userPrompt} as a placeholder.")]
        public string StartPrompt { get; set; } = "{userPrompt}";
    }
}
