using CommandLine;

namespace Wally.Console.Options
{
    [Verb("delete-loop", HelpText = "Delete a loop definition from the workspace.")]
    public class DeleteLoopOptions
    {
        [Value(0, Required = true, HelpText = "Name of the loop to delete.")]
        public string Name { get; set; } = string.Empty;
    }
}
