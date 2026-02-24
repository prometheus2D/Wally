using CommandLine;

namespace Wally.Console.Options
{
    [Verb("save", HelpText = "Save the current Wally environment to the specified path.")]
    public class SaveOptions
    {
        [Value(0, Required = true, HelpText = "The path to save the workspace.")]
        public string Path { get; set; }
    }
}