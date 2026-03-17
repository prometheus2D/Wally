using CommandLine;

namespace Wally.Console.Options.Loops
{
    [Verb("edit-loop", HelpText = "Edit an existing loop definition.")]
    public class EditLoopOptions
    {
        [Value(0, Required = true, HelpText = "Name of the loop to edit.")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = null,
            HelpText = "New description (omit to keep current).")]
        public string? Description { get; set; }

        [Option('a', "actor", Required = false, Default = null,
            HelpText = "New default actor name (omit to keep current).")]
        public string? ActorName { get; set; }

        [Option('s', "start-prompt", Required = false, Default = null,
            HelpText = "New start prompt template (omit to keep current).")]
        public string? StartPrompt { get; set; }
    }
}
