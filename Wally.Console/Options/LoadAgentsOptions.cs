using CommandLine;

namespace Wally.Console.Options
{
    [Verb("load-agents", HelpText = "Load default agents from a JSON file.")]
    public class LoadAgentsOptions
    {
        [Value(0, Required = true, HelpText = "The path to the JSON agents file.")]
        public string JsonPath { get; set; }
    }
}