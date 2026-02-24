using CommandLine;

namespace Wally.Console.Options
{
    [Verb("load-config", HelpText = "Load configuration from a JSON file.")]
    public class LoadConfigOptions
    {
        [Value(0, Required = true, HelpText = "The path to the JSON configuration file.")]
        public string JsonPath { get; set; }
    }
}