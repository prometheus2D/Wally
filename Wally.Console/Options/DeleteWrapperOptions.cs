using CommandLine;

namespace Wally.Console.Options
{
    [Verb("delete-wrapper", HelpText = "Delete an LLM wrapper definition from the workspace.")]
    public class DeleteWrapperOptions
    {
        [Value(0, Required = true, HelpText = "Name of the wrapper to delete.")]
        public string Name { get; set; } = string.Empty;
    }
}
