using CommandLine;

namespace Wally.Console.Options
{
    [Verb("delete-runbook", HelpText = "Delete a runbook (.wrb) from the workspace.")]
    public class DeleteRunbookOptions
    {
        [Value(0, Required = true, HelpText = "Name of the runbook to delete.")]
        public string Name { get; set; } = string.Empty;
    }
}
