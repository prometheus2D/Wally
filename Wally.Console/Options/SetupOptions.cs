using CommandLine;

namespace Wally.Console.Options
{
    [Verb("setup", HelpText = "Set up a Wally workspace. Supply a path to your codebase root (WorkSource); .wally/ is created inside it. Defaults to the exe directory.")]
    public class SetupOptions
    {
        [Value(0, Required = false, Default = null,
            HelpText = "The WorkSource directory (your codebase root). The .wally/ workspace folder is created inside it. Defaults to the exe directory.")]
        public string Path { get; set; }
    }
}