using CommandLine;

namespace Wally.Console.Options
{
    [Verb("add-file", HelpText = "Add a file path to the Wally environment.")]
    public class AddFileOptions
    {
        [Value(0, Required = true, HelpText = "The file path to add.")]
        public string FilePath { get; set; }
    }
}