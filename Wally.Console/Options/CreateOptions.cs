using CommandLine;

namespace Wally.Console.Options
{
    [Verb("create", HelpText = "Create a new default Wally workspace at the specified path.")]
    public class CreateOptions
    {
        [Value(0, Required = true, HelpText = "The path for the new workspace.")]
        public string Path { get; set; }
    }
}