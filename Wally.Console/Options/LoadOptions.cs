using CommandLine;

namespace Wally.Console.Options
{
    [Verb("load", HelpText = "Load a Wally workspace from the specified path.")]
    public class LoadOptions
    {
        [Value(0, Required = true, HelpText = "The path to the workspace folder.")]
        public string Path { get; set; }
    }
}