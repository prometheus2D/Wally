using CommandLine;

namespace Wally.Console.Options
{
    [Verb("remove-folder", HelpText = "Remove a registered folder reference.")]
    public class RemoveFolderOptions
    {
        [Value(0, Required = true, HelpText = "The folder path to remove.")]
        public string FolderPath { get; set; }
    }
}
