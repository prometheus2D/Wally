using CommandLine;

namespace Wally.Console.Options.Runbooks
{
    [Verb("edit-runbook", HelpText = "Edit an existing runbook's description.")]
    public class EditRunbookOptions
    {
        [Value(0, Required = true, HelpText = "Name of the runbook to edit.")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = null,
            HelpText = "New description (omit to keep current).")]
        public string? Description { get; set; }
    }
}
