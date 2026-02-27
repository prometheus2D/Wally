using CommandLine;

namespace Wally.Console.Options
{
    [Verb("create", HelpText = "Create a new default Wally workspace. Supply your codebase root (WorkSource); .wally/ is created inside it.")]
    public class CreateOptions
    {
        [Value(0, Required = true, HelpText = "The WorkSource directory (your codebase root). A .wally/ workspace folder will be created inside it.")]
        public string Path { get; set; }
    }
}