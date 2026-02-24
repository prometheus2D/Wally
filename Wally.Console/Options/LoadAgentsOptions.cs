using CommandLine;

namespace Wally.Console.Options
{
    [Verb("load-Actors", HelpText = "Load default Actors from a JSON file.")]
    public class LoadActorsOptions
    {
        [Value(0, Required = true, HelpText = "The path to the JSON Actors file.")]
        public string JsonPath { get; set; }
    }
}