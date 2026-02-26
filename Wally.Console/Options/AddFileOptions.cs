using CommandLine;

namespace Wally.Console.Options
{
    [Verb("add-file", HelpText = "Register a file for actor/copilot access.")]
    public class AddFileOptions
    {
        [Value(0, Required = true, HelpText = "The file path to register.")]
        public string FilePath { get; set; }
    }
}