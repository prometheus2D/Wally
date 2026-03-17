using CommandLine;

namespace Wally.Console.Options.Loops
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
            HelpText = "Default actor name for single-actor runs.")]
        public string ActorName { get; set; } = string.Empty;

        [Option('s', "start-prompt", Required = false, Default = "{userPrompt}",
            HelpText = "The start prompt template. Use {userPrompt} as a placeholder.")]
        public string StartPrompt { get; set; } = "{userPrompt}";
    }
}
