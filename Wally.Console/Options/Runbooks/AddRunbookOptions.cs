using CommandLine;

namespace Wally.Console.Options.Runbooks
{
    [Verb("add-runbook", HelpText = "Create a new runbook (.wrb) in the workspace.")]
    public class AddRunbookOptions
    {
        [Value(0, Required = true, HelpText = "Name for the new runbook (becomes the .wrb file name).")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = "",
            HelpText = "Description comment placed at the top of the .wrb file.")]
        public string Description { get; set; } = string.Empty;
    }
}
