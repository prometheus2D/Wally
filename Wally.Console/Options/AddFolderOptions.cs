using CommandLine;

namespace Wally.Console.Options
{
    [Verb("add-folder", HelpText = "Register a folder for actor/copilot access.")]
    public class AddFolderOptions
    {
        [Value(0, Required = true, HelpText = "The folder path to register.")]
        public string FolderPath { get; set; }
    }
}
