using CommandLine;

namespace Wally.Console.Options
{
    [Verb("setup", HelpText = "Set up a Wally workspace. Supply a path to your codebase root (WorkSource); .wally/ is created inside it. Defaults to the exe directory.")]
    public class SetupOptions
    {
        [Value(0, Required = false, Default = null,
            HelpText = "The WorkSource directory (your codebase root). The .wally/ workspace folder is created inside it. Defaults to the exe directory.")]
        public string Path { get; set; }

        [Option('w', "worksource", Required = false, Default = null,
            HelpText = "Explicit WorkSource directory. Same as the positional <path> argument — use whichever you prefer. This takes priority when both are supplied.")]
        public string WorkSource { get; set; }

        /// <summary>
        /// Returns the resolved WorkSource path. <c>--worksource</c> takes
        /// priority over the positional argument.
        /// </summary>
        public string? ResolvedPath => WorkSource ?? Path;
    }
}