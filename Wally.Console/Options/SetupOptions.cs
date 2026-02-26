using CommandLine;

namespace Wally.Console.Options
{
    [Verb("setup", HelpText = "Set up a Wally workspace. Defaults to the exe directory; supply --path to target a specific folder.")]
    public class SetupOptions
    {
        [Option('p', "path", Required = false, Default = null,
            HelpText = "The folder in which to scaffold (or load) the Wally workspace. Defaults to the exe directory.")]
        public string Path { get; set; }
    }
}