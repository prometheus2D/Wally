using CommandLine;

namespace Wally.Console.Options
{
    [Verb("remove-file", HelpText = "Remove a registered file reference.")]
    public class RemoveFileOptions
    {
        [Value(0, Required = true, HelpText = "The file path to remove.")]
        public string FilePath { get; set; }
    }
}
