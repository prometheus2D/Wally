using CommandLine;

namespace Wally.Console.Options.Wrappers
{
    [Verb("edit-wrapper", HelpText = "Edit an existing LLM wrapper definition.")]
    public class EditWrapperOptions
    {
        [Value(0, Required = true, HelpText = "Name of the wrapper to edit.")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = null,
            HelpText = "New description (omit to keep current).")]
        public string? Description { get; set; }

        [Option('e', "executable", Required = false, Default = null,
            HelpText = "New executable (omit to keep current).")]
        public string? Executable { get; set; }

        [Option('t', "template", Required = false, Default = null,
            HelpText = "New argument template (omit to keep current).")]
        public string? ArgumentTemplate { get; set; }

        [Option("can-make-changes", Required = false, Default = null,
            HelpText = "Whether this wrapper can make file changes (omit to keep current).")]
        public bool? CanMakeChanges { get; set; }
    }
}
