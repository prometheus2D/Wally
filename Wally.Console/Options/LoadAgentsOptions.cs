using CommandLine;

namespace Wally.Console.Options
{
    [Verb("load-actors", HelpText = "Load actor definitions from a JSON file. Omit path to auto-resolve default-agents.json.")]
    public class LoadActorsOptions
    {
        [Value(0, Required = false, Default = null,
            HelpText = "Path to the agents JSON file. Defaults to default-agents.json in the workspace or exe directory.")]
        public string JsonPath { get; set; }
    }
}