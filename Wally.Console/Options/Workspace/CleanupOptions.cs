using CommandLine;

namespace Wally.Console.Options.Workspace
{
    [Verb("cleanup", HelpText = "Delete the local .wally/ workspace folder so setup can scaffold a fresh one.")]
    public class CleanupOptions
    {
        [Value(0, Required = false, Default = null,
            HelpText = "The WorkSource directory whose .wally/ folder should be deleted. Defaults to the exe directory.")]
        public string? Path { get; set; }
    }
}
