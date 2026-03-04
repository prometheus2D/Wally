using CommandLine;

namespace Wally.Console.Options
{
    [Verb("edit-actor", HelpText = "Edit an existing actor's RBA prompts.")]
    public class EditActorOptions
    {
        [Value(0, Required = true, HelpText = "Name of the actor to edit.")]
        public string Name { get; set; } = string.Empty;

        [Option('r', "role", Required = false, Default = null,
            HelpText = "New role prompt (omit to keep current).")]
        public string? RolePrompt { get; set; }

        [Option('c', "criteria", Required = false, Default = null,
            HelpText = "New acceptance criteria prompt (omit to keep current).")]
        public string? CriteriaPrompt { get; set; }

        [Option('i', "intent", Required = false, Default = null,
            HelpText = "New intent prompt (omit to keep current).")]
        public string? IntentPrompt { get; set; }
    }
}
